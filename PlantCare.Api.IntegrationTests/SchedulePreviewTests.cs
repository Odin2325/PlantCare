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

public sealed class SchedulePreviewTests : IAsyncLifetime
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

    private static object Request(int interval = 14, string mode = "Interval", int days = 0,
        string? time = null, string? zone = null, bool enabled = true) => new {
        intervalDays = interval, isEnabled = enabled, scheduleMode = mode,
        weekDays = days, preferredTimeLocal = time, timeZoneId = zone };

    [Fact]
    public async Task PreviewDoesNotWriteAndConfirmedUpdateMatchesBothRecurrenceModes()
    {
        var (owner,id) = await Seed(); using var client = await Client(owner);
        var url = $"/api/my-plants/{id}/care/Watering/schedule";
        foreach(var request in new[] { Request(), Request(mode:"Weekdays", days:1, time:"09:00", zone:"Europe/Berlin"), Request(enabled:false) })
        {
            var before = await client.GetStringAsync($"/api/my-plants/{id}");
            var response = await client.PostAsJsonAsync(url+"/preview",request); response.EnsureSuccessStatusCode();
            var preview = await response.Content.ReadAsStringAsync();
            Assert.Equal(before,await client.GetStringAsync($"/api/my-plants/{id}"));
            Assert.Empty((await client.GetFromJsonAsync<List<object>>($"/api/my-plants/{id}/care/history"))!);
            var applied = await client.PutAsJsonAsync(url,request); applied.EnsureSuccessStatusCode();
            Assert.Equal(preview,await applied.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task InvalidPreviewLeavesPersistedScheduleUnchanged()
    {
        var (owner,id) = await Seed();using var client = await Client(owner);
        var before = await client.GetStringAsync($"/api/my-plants/{id}");
        foreach(var request in new[] { Request(0), Request(mode:"Weekdays",time:"09:00",zone:"Europe/Berlin"), Request(mode:"Weekdays",days:1,time:"09:00",zone:"Invalid/Zone") })
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync($"/api/my-plants/{id}/care/Watering/schedule/preview",request)).StatusCode);
        Assert.Equal(before,await client.GetStringAsync($"/api/my-plants/{id}"));
    }

    [Fact]
    public async Task PreviewRequiresActiveOwnedScheduleAndRejectsErasedAccounts()
    {
        var (owner,id) = await Seed();using var client = await Client(owner);
        var url=$"/api/my-plants/{id}/care/Watering/schedule/preview";
        using var other=await Client(await factory.CreateUserAsync());
        Assert.Equal(HttpStatusCode.NotFound,(await other.PostAsJsonAsync(url,Request())).StatusCode);
        using var anon=factory.CreateClient(new WebApplicationFactoryClientOptions {BaseAddress=new Uri("https://localhost")});await anon.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PostAsJsonAsync(url,Request())).StatusCode);
        (await client.DeleteAsync($"/api/my-plants/{id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(url,Request())).StatusCode);
        (await client.PostAsync($"/api/my-plants/{id}/restore",null)).EnsureSuccessStatusCode();
        await factory.SeedAsync(db=>{db.AccountErasures.Add(AccountErasure.Create(owner,PlantCareApiFactory.UtcNow));return Task.CompletedTask;});
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(url,Request())).StatusCode);
    }
}
