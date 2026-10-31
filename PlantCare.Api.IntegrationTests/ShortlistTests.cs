using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Controllers;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Api.IntegrationTests;

public sealed class ShortlistTests : IAsyncLifetime
{
    private readonly PlantCareApiFactory factory = new(pushTestSqlPassword: Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"));
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync()
    {
        try { await factory.DeletePushTestDatabaseAsync(); }
        finally { await factory.DisposeAsync(); }
    }
    private async Task<Guid> Species()
    {
        var plant = PlantSpecies.Create("Shortlist fixture", null, "Test", SunlightRequirement.FullSun,
            "Test", 7, "Test", null, null, "Test", null, null, null, false);
        await factory.SeedAsync(db => { db.PlantSpecies.Add(plant); return Task.CompletedTask; });
        return plant.Id;
    }
    private async Task<HttpClient> Client(Guid user)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.AuthenticateAs(user); await client.AddAntiforgeryTokenAsync(); return client;
    }

    [Fact]
    public async Task SavesAreIdempotentPrivateAndDoNotCreateOwnedPlantsOrReminders()
    {
        var a = await factory.CreateUserAsync(); var b = await factory.CreateUserAsync(); var species = await Species();
        using var owner = await Client(a); using var other = await Client(b);
        (await owner.PutAsJsonAsync($"/api/shortlist/{species}", new { userId = b })).EnsureSuccessStatusCode();
        var first = Assert.Single((await owner.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!);
        (await owner.PutAsJsonAsync($"/api/shortlist/{species}", new { })).EnsureSuccessStatusCode();
        Assert.Equal(first, Assert.Single((await owner.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!));
        using var reloaded = await Client(a);
        Assert.Equal(species, Assert.Single((await reloaded.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!).PlantSpeciesId);
        Assert.Empty((await other.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!);
        (await other.DeleteAsync($"/api/shortlist/{species}")).EnsureSuccessStatusCode();
        Assert.Single((await owner.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!);
        (await other.PutAsJsonAsync($"/api/shortlist/{species}", new { })).EnsureSuccessStatusCode();
        (await owner.DeleteAsync($"/api/shortlist/{species}")).EnsureSuccessStatusCode();
        (await owner.DeleteAsync($"/api/shortlist/{species}")).EnsureSuccessStatusCode();
        Assert.Empty((await owner.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!);
        Assert.Single((await other.GetFromJsonAsync<List<ShortlistController.Item>>("/api/shortlist"))!);
        await factory.SeedAsync(async db => {
            Assert.Empty(await db.UserPlants.ToListAsync()); Assert.Empty(await db.CareSchedules.ToListAsync());
            Assert.Empty(await db.Notifications.ToListAsync()); Assert.Empty(await db.HarvestRecords.ToListAsync());
        });
    }

    [Fact]
    public async Task AnonymousMissingSpeciesAndTombstonedOwnersCannotSave()
    {
        var owner = await factory.CreateUserAsync(); var species = await Species(); using var client = await Client(owner);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/shortlist/{Guid.NewGuid()}", new { })).StatusCode);
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/shortlist")).StatusCode);
        await anonymous.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync($"/api/shortlist/{species}", new { })).StatusCode);
        (await client.PutAsJsonAsync($"/api/shortlist/{species}", new { })).EnsureSuccessStatusCode();
        await factory.SeedAsync(db => { db.AccountErasures.Add(AccountErasure.Create(owner, DateTimeOffset.UtcNow)); return Task.CompletedTask; });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/shortlist")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"/api/shortlist/{species}", new { })).StatusCode);
        await factory.SeedAsync(async db => Assert.Empty(await db.ShortlistedPlants.ToListAsync()));
    }
}
