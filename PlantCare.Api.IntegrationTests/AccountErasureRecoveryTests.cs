using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Api.Operations;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.IntegrationTests;

public sealed class AccountErasureRecoveryTests : IAsyncLifetime
{
    private readonly PlantCareApiFactory factory = new(useRealAuthentication: true,
        pushTestSqlPassword: Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"));
    private readonly string folder = Path.Combine(Path.GetTempPath(), "plantcare-erasure-test-" + Guid.NewGuid().ToString("N"));
    private const string Password = "SyntheticErasurePassword9!";
    public async Task InitializeAsync() { Directory.CreateDirectory(folder); await factory.ResetDatabaseAsync(); }
    public async Task DisposeAsync()
    {
        try { await factory.DeletePushTestDatabaseAsync(); }
        finally
        {
            await factory.DisposeAsync();
            foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }
    private async Task<ApplicationUser> UserAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"erasure-{Guid.NewGuid():N}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await manager.CreateAsync(user, Password)).Succeeded);
        return user;
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    private async Task Login(HttpClient client, ApplicationUser user, HttpStatusCode expected = HttpStatusCode.OK)
    {
        await client.AddAntiforgeryTokenAsync();
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = user.Email, password = Password })).StatusCode);
        if (expected == HttpStatusCode.OK) await client.AddAntiforgeryTokenAsync();
    }
    private static IConfiguration RecoveryConfig(bool enabled = true, string source = "plantcare-local") =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Recovery:AllowAccountErasureApply"] = enabled.ToString(), ["Recovery:AccountErasureSource"] = source }).Build();
    private async Task Apply(string file, IConfiguration? config = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await AccountErasureCommands.ExecuteAsync(scope.ServiceProvider, config ?? RecoveryConfig(), ["--apply-account-erasures", file]);
    }

    [Fact]
    public async Task CurrentLedgerSuppressesReplayedOlderRowsAndPreservesAnotherAccount()
    {
        var a = await UserAsync(); var b = await UserAsync();
        var species = PlantSpecies.Create("Synthetic recovery species", null, "Test", SunlightRequirement.BrightIndirectLight, "Test", 1, "Test", null, null, "Test", null, null, null, false);
        var plant = UserPlant.Create(a.Id, species.Id, "Private erased plant", null, null, "Private erased notes", DateTimeOffset.UtcNow);
        var schedule = plant.AddCareSchedule(CareActionType.Watering, 1, DateTimeOffset.UtcNow.AddDays(-2));
        var history = CareEvent.Create(schedule.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1));
        await factory.SeedAsync(db => { db.PlantSpecies.Add(species); db.UserPlants.Add(plant); db.CareEvents.Add(history); return Task.CompletedTask; });
        using var client = Client(); await Login(client, a);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE" })).StatusCode);
        var file = Path.Combine(folder, "current.json");
        await using (var scope = factory.Services.CreateAsyncScope())
            await AccountErasureCommands.ExecuteAsync(scope.ServiceProvider, RecoveryConfig(), ["--export-account-erasures", "plantcare-local", file]);
        // Replay the pre-deletion database rows in this generated test database;
        // the current ledger is kept separately and must not be replaced by them.
        await factory.SeedAsync(async db =>
        {
            await db.AccountErasures.ExecuteDeleteAsync();
            db.Users.Add(a); db.UserPlants.Add(plant); db.CareEvents.Add(history);
            db.Entry(species).State = EntityState.Unchanged;
        });
        await Apply(file); await Apply(file); // Idempotent reconciliation.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
            Assert.False(await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == a.Id));
            Assert.False(await db.UserPlants.IgnoreQueryFilters().AnyAsync(p => p.UserId == a.Id));
            Assert.True(await db.Users.AnyAsync(u => u.Id == b.Id));
            Assert.Single(await db.AccountErasures.ToListAsync());
        }
        using var restoredLogin = Client(); await Login(restoredLogin, a, HttpStatusCode.Unauthorized);
        using var other = Client(); await Login(other, b);
    }

    [Theory]
    [InlineData("stale")]
    [InlineData("wrong-source")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("disabled")]
    public async Task UntrustedOrUnavailableLedgerCannotChangeTheTarget(string mode)
    {
        var a = await UserAsync(); var file = Path.Combine(folder, "invalid.json");
        var marker = new AccountErasureCommands.Marker(a.Id, DateTimeOffset.UtcNow);
        var document = new AccountErasureCommands.Ledger(1, mode == "wrong-source" ? "plantcare-vps-staging" : "plantcare-local",
            mode == "stale" ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow,
            mode == "duplicate" ? [marker, marker] : [marker], []);
        if (mode != "missing") await File.WriteAllTextAsync(file, JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Apply(file, RecoveryConfig(mode != "disabled")));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.True(await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == a.Id));
        Assert.Empty(await db.AccountErasures.ToListAsync());
    }

    [Fact]
    public async Task CompleteLiveMembershipAlsoProtectsDeletionBeforeMarkersExisted()
    {
        var a = await UserAsync(); var b = await UserAsync();
        await factory.SeedAsync(async db => { await db.Users.Where(u => u.Id == a.Id).ExecuteDeleteAsync(); });
        var file = Path.Combine(folder, "legacy-deletion.json");
        await using (var scope = factory.Services.CreateAsyncScope())
            await AccountErasureCommands.ExecuteAsync(scope.ServiceProvider, RecoveryConfig(), ["--export-account-erasures", "plantcare-local", file]);
        await factory.SeedAsync(db => { db.Users.Add(a); return Task.CompletedTask; });
        await Apply(file);
        await using var checkScope = factory.Services.CreateAsyncScope();
        var check = checkScope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.False(await check.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == a.Id));
        Assert.True(await check.Users.AnyAsync(u => u.Id == b.Id));
        Assert.Single(await check.AccountErasures.ToListAsync());
    }

    [Fact]
    public async Task MarkerHidesAnUnreconciledIdentityAndPrivatePlant()
    {
        var a = await UserAsync();
        var b = await UserAsync();
        var species = PlantSpecies.Create("Synthetic filtered species", null, "Test", SunlightRequirement.BrightIndirectLight, "Test", 1, "Test", null, null, "Test", null, null, null, false);
        var plant = UserPlant.Create(a.Id, species.Id, "Filtered plant", null, null, null, DateTimeOffset.UtcNow);
        var schedule = plant.AddCareSchedule(CareActionType.Watering, 1, DateTimeOffset.UtcNow.AddDays(-2));
        await factory.SeedAsync(db => {
            db.PlantSpecies.Add(species); db.UserPlants.Add(plant);
            db.Notifications.Add(Notification.Create(a.Id, schedule.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
            db.CalendarSubscriptions.Add(CalendarSubscription.Create(a.Id, new string('f',64), DateTimeOffset.UtcNow));
            db.CalendarShares.Add(CalendarShare.Create(a.Id, b.Id, DateTimeOffset.UtcNow));
            db.ExternalCalendarConnections.Add(ExternalCalendarConnection.CreateGoogle(a.Id,a.Email!,"synthetic-access","synthetic-refresh",DateTimeOffset.UtcNow.AddHours(1),DateTimeOffset.UtcNow));
            db.AccountErasures.Add(AccountErasure.Create(a.Id, DateTimeOffset.UtcNow)); return Task.CompletedTask;
        });
        using var client = Client(); await Login(client, a, HttpStatusCode.Unauthorized);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Id == a.Id));
        Assert.True(await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == a.Id));
        Assert.Empty(await db.UserPlants.ToListAsync()); Assert.Single(await db.UserPlants.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await db.CareSchedules.ToListAsync()); Assert.Empty(await db.Notifications.ToListAsync());
        Assert.Empty(await db.CalendarSubscriptions.ToListAsync()); Assert.Empty(await db.CalendarShares.ToListAsync());
        Assert.Empty(await db.ExternalCalendarConnections.ToListAsync());
    }
}
