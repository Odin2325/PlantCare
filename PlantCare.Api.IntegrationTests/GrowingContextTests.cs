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

public sealed class GrowingContextTests : IAsyncLifetime
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
    public async Task ContextPersistsClearsAndDoesNotChangeCareHistoryPlansOrNotes()
    {
        var (owner, id) = await Seed(); using var client = await Client(owner);
        var before = (await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!;
        var context = new UpdateGrowingContextCommand("balcony", "winter");
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context", context)).EnsureSuccessStatusCode();
        var after = (await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!;
        Assert.Equal("balcony", after.GrowingEnvironment); Assert.Equal("winter", after.CareSeason);
        Assert.Equal(JsonSerializer.Serialize(before,JsonOptions), JsonSerializer.Serialize(after with { GrowingEnvironment = null, CareSeason = null },JsonOptions));
        Assert.Equal("winter", Assert.Single((await client.GetFromJsonAsync<List<UserPlantDto>>("/api/my-plants", JsonOptions))!).CareSeason);
        Assert.Empty((await client.GetFromJsonAsync<List<object>>($"/api/my-plants/{id}/care/history"))!);
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context", new UpdateGrowingContextCommand(null,null))).EnsureSuccessStatusCode();
        Assert.Null((await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!.CareSeason);
    }
    [Fact]
    public async Task InvalidPreferencesAreAtomicAndErasureMarkersHideRestoredContext()
    {
        var (owner, id) = await Seed(); using var client = await Client(owner);
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context", new UpdateGrowingContextCommand("indoor","summer"))).EnsureSuccessStatusCode();
        foreach (var command in new[] {new UpdateGrowingContextCommand("wrong","winter"),new UpdateGrowingContextCommand("balcony","wrong")})
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context", command)).StatusCode);
        var plant = (await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}", JsonOptions))!;
        Assert.Equal("indoor",plant.GrowingEnvironment); Assert.Equal("summer",plant.CareSeason);
        await factory.SeedAsync(db => { db.AccountErasures.Add(AccountErasure.Create(owner,PlantCareApiFactory.UtcNow));return Task.CompletedTask; });
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/my-plants/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context",new UpdateGrowingContextCommand(null,null))).StatusCode);
    }
    [Fact]
    public async Task OwnerAnonymousAndArchiveBoundariesApply()
    {
        var (owner, id) = await Seed(); using var client = await Client(owner);
        var command = new UpdateGrowingContextCommand("greenhouse","spring");
        using var other = await Client(await factory.CreateUserAsync());
        Assert.Equal(HttpStatusCode.NotFound,(await other.PutAsJsonAsync($"/api/my-plants/{id}/growing-context",command)).StatusCode);
        using var anon=factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });await anon.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PutAsJsonAsync($"/api/my-plants/{id}/growing-context",command)).StatusCode);
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context",command)).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/my-plants/{id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound,(await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context",new UpdateGrowingContextCommand(null,null))).StatusCode);
        Assert.Equal("spring",(await client.GetFromJsonAsync<UserPlantDto>($"/api/my-plants/{id}",JsonOptions))!.CareSeason);
        (await client.PostAsync($"/api/my-plants/{id}/restore",null)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/my-plants/{id}/growing-context",new UpdateGrowingContextCommand(null,null))).EnsureSuccessStatusCode();
    }
}
