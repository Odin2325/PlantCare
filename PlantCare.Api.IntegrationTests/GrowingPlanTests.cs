using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Application.MyPlants;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Api.IntegrationTests;

public sealed class GrowingPlanTests : IAsyncLifetime
{
    private readonly PlantCareApiFactory factory = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync() => await factory.DisposeAsync();
    private async Task<(Guid Owner, Guid Plant)> Seed()
    {
        var owner = await factory.CreateUserAsync();
        var species = PlantSpecies.Create("Plan fixture", null, "Test", SunlightRequirement.FullSun, "Test", 7, "Test", null, null, "Test", null, null, null, false);
        var plant = UserPlant.Create(owner, species.Id, "My crop", "Balcony", new DateOnly(2025, 1, 1), "Keep notes", PlantCareApiFactory.UtcNow);
        plant.AddCareSchedule(CareActionType.Watering, 7, PlantCareApiFactory.UtcNow);
        await factory.SeedAsync(db => { db.PlantSpecies.Add(species); db.UserPlants.Add(plant); return Task.CompletedTask; });
        return (owner, plant.Id);
    }
    private async Task<HttpClient> Client(Guid owner)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.AuthenticateAs(owner); await client.AddAntiforgeryTokenAsync(); return client;
    }
    [Fact]
    public async Task PlanPersistsAndClearsWithoutChangingCareOrAcquisition()
    {
        var (owner, id) = await Seed(); using var client = await Client(owner);
        var before = await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions);
        var plan = new UpdateGrowingPlanCommand(new(2027, 3, 1), new(2027, 5, 15), new(2027, 8, 1));
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).EnsureSuccessStatusCode();
        var after = await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions);
        Assert.Equal(plan.PlannedSowingOn, after!.PlannedSowingOn);
        Assert.Equal(plan.PlannedPlantingOutOn, after.PlannedPlantingOutOn);
        Assert.Equal(plan.ExpectedHarvestOn, after.ExpectedHarvestOn);
        Assert.Equal(before!.CareSchedules, after.CareSchedules); Assert.Equal(before.AcquiredOn, after.AcquiredOn);
        Assert.Equal(before.Notes, after.Notes);
        Assert.Equal(plan.ExpectedHarvestOn, Assert.Single((await client.GetFromJsonAsync<List<UserPlantDto>>("/api/my-plants", JsonOptions))!).ExpectedHarvestOn);
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", new UpdateGrowingPlanCommand(null, null, null))).EnsureSuccessStatusCode();
        Assert.Null((await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!.ExpectedHarvestOn);
    }
    [Fact]
    public async Task InvalidOrderAndMalformedDateDoNotChangeSavedPlan()
    {
        var (owner, id) = await Seed(); using var client = await Client(owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", new UpdateGrowingPlanCommand(new(2027, 5, 1), null, new(2027, 4, 1)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", new { plannedSowingOn = "2027-02-30" })).StatusCode);
        Assert.Null((await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!.PlannedSowingOn);
    }
    [Fact]
    public async Task OtherOwnersAndArchivedPlantsCannotBeEdited()
    {
        var (owner, id) = await Seed(); var other = await factory.CreateUserAsync();
        using var outsider = await Client(other); var plan = new UpdateGrowingPlanCommand(null, null, new(2027, 8, 1));
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).StatusCode);
        using var client = await Client(owner); (await client.DeleteAsync($"/api/my-plants/{id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-plan", plan)).StatusCode);
        Assert.Null((await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!.ExpectedHarvestOn);
    }
}
