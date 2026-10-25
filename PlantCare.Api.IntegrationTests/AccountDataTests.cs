using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.IntegrationTests;

public sealed class AccountDataTests : IAsyncLifetime
{
    private const string Password = "SyntheticPassword9!";
    private readonly PlantCareApiFactory factory = new(useRealAuthentication: true,
        pushTestSqlPassword: Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"));
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync()
    {
        try { await factory.DeletePushTestDatabaseAsync(); }
        finally { await factory.DisposeAsync(); }
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    private async Task<ApplicationUser> UserAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"account-{Guid.NewGuid():N}@example.test";
        var user = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
        Assert.True((await manager.CreateAsync(user, Password)).Succeeded);
        return user;
    }
    private async Task Login(HttpClient client, ApplicationUser user)
    {
        await client.AddAntiforgeryTokenAsync();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = user.Email, password = Password })).EnsureSuccessStatusCode();
        await client.AddAntiforgeryTokenAsync();
    }
    private async Task Seed(ApplicationUser a, ApplicationUser b)
    {
        var now = DateTimeOffset.UtcNow.AddDays(-2);
        var species = PlantSpecies.Create("Synthetic account species", null, "Test", SunlightRequirement.BrightIndirectLight,
            "Test", 1, "Test", null, null, "Test", null, null, null, false);
        var plant = UserPlant.Create(a.Id, species.Id, "Private A plant", "Test room", null, "Private A notes", now);
        var schedule = plant.AddCareSchedule(CareActionType.Watering, 1, now);
        plant.ReplaceTags(["Synthetic tag"]);
        plant.UpdateGrowingPlan(new DateOnly(2027, 3, 1), new DateOnly(2027, 5, 15), new DateOnly(2027, 8, 1));
        plant.Archive();
        var notification = Notification.Create(a.Id, schedule.Id, now, now);
        var device = PushSubscription.Create(a.Id, "https://push.example.test/private-endpoint", "private-key", "private-auth", now);
        await factory.SeedAsync(db =>
        {
            db.PlantSpecies.Add(species); db.UserPlants.Add(plant);
            db.UserPlants.Add(UserPlant.Create(b.Id, species.Id, "Private B plant", null, null, "Private B notes", now));
            db.CareEvents.Add(CareEvent.Create(schedule.Id, now, now));
            db.Notifications.AddRange(notification, Notification.CreateFollowUp(notification, now, 24)); db.PushSubscriptions.Add(device);
            db.PushDeliveries.Add(PushDelivery.Create(notification.Id, device.Id, now));
            db.NotificationPreferences.Add(NotificationPreference.CreateDefault(a.Id));
            db.CalendarSubscriptions.Add(CalendarSubscription.Create(a.Id, new string('a', 64), now));
            db.CalendarShares.AddRange(CalendarShare.Create(a.Id, b.Id, now), CalendarShare.Create(b.Id, a.Id, now));
            db.CalendarInvitations.AddRange(CalendarInvitation.Create(a.Id, "pending@gmail.com", new string('b',64), now), CalendarInvitation.Create(b.Id, a.Email!, new string('c',64), now));
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task AnonymousRequestsCannotExportOrDelete()
    {
        using var client = Client();
        await client.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/account/export", new { password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE" })).StatusCode);
    }

    [Fact]
    public async Task CookieSnoozeRequiresAntiforgeryAndRejectsArchivedCare()
    {
        var a = await UserAsync(); var b = await UserAsync(); await Seed(a, b);
        Guid notification;
        await using (var scope = factory.Services.CreateAsyncScope())
            notification = await scope.ServiceProvider.GetRequiredService<PlantCareDbContext>().Notifications
                .Where(n => n.UserId == a.Id && n.ReminderSequence == 0).Select(n => n.Id).SingleAsync();
        using var client = Client(); await Login(client, a);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/notifications/{notification}/snooze", new { hours = 1 })).StatusCode);
        await client.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/notifications/{notification}/snooze", new { hours = 1 })).StatusCode);
    }

    [Fact]
    public async Task GrowingPlanUsesRealCookieAntiforgeryAndOwnerBoundary()
    {
        var a = await UserAsync(); var b = await UserAsync(); await Seed(a, b);
        Guid id;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
            var plant = await db.UserPlants.SingleAsync(p => p.UserId == a.Id); plant.Restore(); id = plant.Id;
            await db.SaveChangesAsync();
        }
        using var client = Client(); await Login(client, a);
        var plan = new { plannedSowingOn = "2027-03-01", plannedPlantingOutOn = "2027-05-15", expectedHarvestOn = "2027-08-02" };
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).StatusCode);
        await client.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).StatusCode);
        using var outsider = Client(); await Login(outsider, b);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).StatusCode);
        using var anonymous = Client(); await anonymous.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).StatusCode);
    }

    [Fact]
    public async Task ExportIsPrivatePasswordProtectedAndExcludesCredentials()
    {
        var a = await UserAsync(); var b = await UserAsync(); await Seed(a, b);
        using var client = Client(); await Login(client, a);
        using var wrong = await client.PostAsJsonAsync("/api/account/export", new { password = "wrong" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        using var response = await client.PostAsJsonAsync("/api/account/export", new { password = Password, userId = b.Id });
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition!.ToString());
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        Assert.Equal(a.Email, json.RootElement.GetProperty("account").GetProperty("email").GetString());
        Assert.Single(json.RootElement.GetProperty("plants").EnumerateArray());
        Assert.Single(json.RootElement.GetProperty("history").EnumerateArray());
        Assert.Equal(2, json.RootElement.GetProperty("notifications").GetArrayLength());
        Assert.Contains(json.RootElement.GetProperty("notifications").EnumerateArray(), n => n.GetProperty("reminderSequence").GetInt32() == 1 && n.GetProperty("notBeforeUtc").ValueKind == JsonValueKind.String);
        Assert.Single(json.RootElement.GetProperty("tags").EnumerateArray());
        Assert.False(json.RootElement.GetProperty("plants")[0].GetProperty("isActive").GetBoolean());
        Assert.Equal("2027-03-01", json.RootElement.GetProperty("plants")[0].GetProperty("plannedSowingOn").GetString());
        Assert.Equal("2027-08-01", json.RootElement.GetProperty("plants")[0].GetProperty("expectedHarvestOn").GetString());
        Assert.Contains("Private A notes", text);
        foreach (var secret in new[] { "Private B notes", "Private B plant", "private-endpoint", "private-key", "private-auth", new string('a',64), "passwordHash", "securityStamp", Password })
            Assert.DoesNotContain(secret, text);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/export", new { password = Password })).StatusCode);
    }

    [Fact]
    public async Task DeleteRequiresConfirmationAndPasswordThenRemovesOnlyOwnerAndRevokesSessions()
    {
        var a = await UserAsync(); var b = await UserAsync(); await Seed(a, b);
        using var client = Client(); using var oldBrowser = Client(); using var bearer = Client();
        await Login(client, a); await Login(oldBrowser, a);
        await bearer.AddAntiforgeryTokenAsync();
        using var login = await bearer.PostAsJsonAsync("/api/auth/login", new { email = a.Email, password = Password });
        login.EnsureSuccessStatusCode();
        using var tokens = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.RootElement.GetProperty("accessToken").GetString());
        bearer.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "no" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/delete", new { password = "wrong", confirmation = "DELETE" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE" })).StatusCode);
        await client.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE", userId = b.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/account/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldBrowser.GetAsync("/api/account/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bearer.GetAsync("/api/my-plants")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bearer.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RootElement.GetProperty("refreshToken").GetString() })).StatusCode);
        using var fresh = Client(); await fresh.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = a.Email, password = Password })).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Id == a.Id)); Assert.True(await db.Users.AnyAsync(u => u.Id == b.Id));
        Assert.Single(await db.UserPlants.ToListAsync()); Assert.Single(await db.PlantSpecies.ToListAsync());
        Assert.Empty(await db.CareSchedules.ToListAsync()); Assert.Empty(await db.CareEvents.ToListAsync());
        Assert.Empty(await db.Notifications.ToListAsync()); Assert.Empty(await db.PushDeliveries.ToListAsync());
        Assert.Empty(await db.CalendarShares.ToListAsync()); Assert.Empty(await db.CalendarSubscriptions.ToListAsync());
        Assert.Empty(await db.CalendarInvitations.ToListAsync());
        Assert.Empty(await db.PushSubscriptions.ToListAsync()); Assert.Empty(await db.NotificationPreferences.ToListAsync());
        Assert.Single(await db.AccountErasures.Where(e => e.UserId == a.Id).ToListAsync());
    }

    [Fact]
    public async Task FailureDuringDeletionRollsBackSharesAndAllAccountData()
    {
        var a = await UserAsync(); var b = await UserAsync(); await Seed(a, b);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
            var sql = db.Database.IsSqlServer()
                ? "CREATE TRIGGER FailSyntheticAccountDelete ON dbo.AspNetUsers AFTER DELETE AS BEGIN THROW 51042, 'Synthetic failure', 1; END;"
                : "CREATE TRIGGER FailSyntheticAccountDelete BEFORE DELETE ON AspNetUsers BEGIN SELECT RAISE(ABORT, 'Synthetic failure'); END;";
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        using var client = Client(); await Login(client, a);
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE" })).StatusCode);
        await using var checkScope = factory.Services.CreateAsyncScope();
        var check = checkScope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.Equal(2, await check.Users.CountAsync()); Assert.Equal(2, await check.CalendarShares.CountAsync());
        Assert.Equal(2, await check.Notifications.CountAsync()); Assert.Single(await check.PushDeliveries.ToListAsync());
        Assert.Single(await check.CareEvents.ToListAsync()); Assert.Equal(2, await check.UserPlants.CountAsync());
        Assert.Empty(await check.AccountErasures.ToListAsync());
    }

    [Fact]
    public async Task ConnectedCalendarBlocksDeletionWithoutPartialChanges()
    {
        var a = await UserAsync(); var b = await UserAsync(); await Seed(a, b);
        await factory.SeedAsync(db => { db.ExternalCalendarConnections.Add(ExternalCalendarConnection.CreateGoogle(a.Id, a.Email!, "secret-access", "secret-refresh", DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow)); return Task.CompletedTask; });
        using var client = Client(); await Login(client, a);
        using var response = await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.Equal(2, await db.CalendarShares.CountAsync()); Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(2, await db.UserPlants.CountAsync());
        using var export = await client.PostAsJsonAsync("/api/account/export", new { password = Password });
        var text = await export.Content.ReadAsStringAsync();
        Assert.Contains("Google", text); Assert.DoesNotContain("secret-access", text); Assert.DoesNotContain("secret-refresh", text);
    }
}
