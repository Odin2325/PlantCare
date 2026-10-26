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

public sealed class HarvestTests : IAsyncLifetime
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
        client.AuthenticateAs(owner);
        await client.AddAntiforgeryTokenAsync();
        return client;
    }
    private static RecordHarvestCommand Harvest(decimal? qty = 2, string? unit = "items") => new(DateOnly.FromDateTime(PlantCareApiFactory.UtcNow.UtcDateTime).AddDays(-1), qty, unit, " First crop ");
    [Fact]
    public async Task SavesListsAndRemovesHarvestWithoutChangingPlansOrCare()
    {
        var (owner, id) = await Seed();
        using var client = await Client(owner);
        var before = await client.GetStringAsync($"/api/my-plants/{id}");
        var response = await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest());
        response.EnsureSuccessStatusCode();
        var record = (await response.Content.ReadFromJsonAsync<HarvestRecordDto>())!;
        Assert.Equal("First crop", record.Notes);
        Assert.Equal(2, record.Quantity);
        Assert.Equal(Harvest().HarvestedOn, record.HarvestedOn);
        Assert.Single((await client.GetFromJsonAsync<List<HarvestRecordDto>>($"/api/my-plants/{id}/harvests"))!);
        Assert.Equal(before, await client.GetStringAsync($"/api/my-plants/{id}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest(0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", new { harvestedOn = "2026-02-30" })).StatusCode);
        (await client.DeleteAsync($"/api/my-plants/{id}/harvests/{record.Id}")).EnsureSuccessStatusCode();
        Assert.Empty((await client.GetFromJsonAsync<List<HarvestRecordDto>>($"/api/my-plants/{id}/harvests"))!);
    }
    [Fact]
    public async Task OtherOwnersAndAnonymousRequestsCannotReadWriteOrRemoveHarvests()
    {
        var (owner, id) = await Seed();
        using var client = await Client(owner);
        var record = (await (await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest())).Content.ReadFromJsonAsync<HarvestRecordDto>())!;
        using var other = await Client(await factory.CreateUserAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/my-plants/{id}/harvests")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/my-plants/{id}/harvests/{record.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/my-plants/{Guid.NewGuid()}/harvests/{record.Id}")).StatusCode);
        using var anon = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await anon.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest())).StatusCode);
    }
    [Fact]
    public async Task ArchiveRetainsReadOnlyHarvestsUntilRestored()
    {
        var (owner, id) = await Seed();
        using var client = await Client(owner);
        var record = (await (await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest())).Content.ReadFromJsonAsync<HarvestRecordDto>())!;
        (await client.DeleteAsync($"/api/my-plants/{id}")).EnsureSuccessStatusCode();
        Assert.Single((await client.GetFromJsonAsync<List<HarvestRecordDto>>($"/api/my-plants/{id}/harvests"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/my-plants/{id}/harvests/{record.Id}")).StatusCode);
        (await client.PostAsync($"/api/my-plants/{id}/restore", null)).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/my-plants/{id}/harvests/{record.Id}")).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task ListIsBoundedAndErasureMarkersHideRestoredHarvestRows()
    {
        var (owner, id) = await Seed();
        await factory.SeedAsync(db => { for (var n = 0; n < 103; n++) db.HarvestRecords.Add(HarvestRecord.Create(id, DateOnly.FromDateTime(PlantCareApiFactory.UtcNow.UtcDateTime).AddDays(-n - 1), null, null, null, PlantCareApiFactory.UtcNow)); return Task.CompletedTask; });
        using var client = await Client(owner);
        var list = (await client.GetFromJsonAsync<List<HarvestRecordDto>>($"/api/my-plants/{id}/harvests"))!;
        Assert.Equal(100, list.Count);
        Assert.Equal(DateOnly.FromDateTime(PlantCareApiFactory.UtcNow.UtcDateTime).AddDays(-1), list[0].HarvestedOn);
        await factory.SeedAsync(db => { db.AccountErasures.Add(AccountErasure.Create(owner, PlantCareApiFactory.UtcNow)); return Task.CompletedTask; });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/my-plants/{id}/harvests")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/my-plants/{id}/harvests", Harvest())).StatusCode);
        await factory.SeedAsync(async db => { Assert.Empty(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.HarvestRecords)); Assert.Equal(103, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(db.HarvestRecords))); });
    }

    [Fact]
    public async Task OverviewKeepsExactCompleteTotalsAndScopesDatesOwnerCategoryArchiveAndErasure()
    {
        var (owner, id) = await Seed();
        var day = Harvest().HarvestedOn;
        await factory.SeedAsync(async db => {
            var plant = await db.UserPlants.FindAsync(id);
            var species = await db.PlantSpecies.FindAsync(plant!.PlantSpeciesId);
            species!.SetCatalogProfile(new PlantCare.Domain.ValueObjects.PlantCatalogProfile("https://example.org/crop", day, null, Categories: ["vegetable"]));
            for (var n = 0; n < 101; n++) db.HarvestRecords.Add(HarvestRecord.Create(id, day, 1.001m, "kg", null, PlantCareApiFactory.UtcNow));
            db.HarvestRecords.Add(HarvestRecord.Create(id, day, 3m, "g", null, PlantCareApiFactory.UtcNow));
            db.HarvestRecords.Add(HarvestRecord.Create(id, day, null, null, null, PlantCareApiFactory.UtcNow));
            db.HarvestRecords.Add(HarvestRecord.Create(id, day.AddDays(-1), 99m, "items", null, PlantCareApiFactory.UtcNow));
        });
        using var client = await Client(owner);
        var url = $"/api/my-plants/harvest-overview?from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}";
        var result = (await client.GetFromJsonAsync<HarvestOverviewDto>(url))!;
        Assert.Equal(100, result.Rows.Count);
        var totals = Assert.Single(result.Plants);
        Assert.Equal(103, totals.RecordCount); Assert.Equal(1, totals.UnquantifiedCount);
        Assert.Equal("101.101", totals.Quantities["kg"]); Assert.Equal("3.000", totals.Quantities["g"]);
        Assert.False(totals.Quantities.ContainsKey("items"));
        var month = Assert.Single(result.Months); Assert.Equal(day.ToString("yyyy-MM"), month.Month);
        Assert.Equal(id, month.UserPlantId); Assert.Equal(103, month.RecordCount); Assert.Equal(1, month.UnquantifiedCount);
        Assert.Equal("101.101", month.Quantities["kg"]); Assert.Equal("3.000", month.Quantities["g"]);
        using var other = await Client(await factory.CreateUserAsync());
        Assert.Empty((await other.GetFromJsonAsync<HarvestOverviewDto>(url))!.Plants);
        Assert.Empty((await other.GetFromJsonAsync<HarvestOverviewDto>(url))!.Months);
        using var anon = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(url)).StatusCode);
        foreach (var query in new[] { "", "?from=2026-02-30&to=2026-03-01", "?from=2026-02-02&to=2026-02-01", "?from=2025-01-01&to=2026-01-02" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/my-plants/harvest-overview" + query)).StatusCode);
        (await client.DeleteAsync($"/api/my-plants/{id}")).EnsureSuccessStatusCode();
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Plants);
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Months);
        (await client.PostAsync($"/api/my-plants/{id}/restore", null)).EnsureSuccessStatusCode();
        await factory.SeedAsync(async db => {
            var plant = await db.UserPlants.FindAsync(id); var species = await db.PlantSpecies.FindAsync(plant!.PlantSpeciesId);
            species!.SetCatalogProfile(species.CatalogProfile! with { Categories = ["houseplant"] });
        });
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Plants);
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Months);
        await factory.SeedAsync(async db => {
            var plant = await db.UserPlants.FindAsync(id); var species = await db.PlantSpecies.FindAsync(plant!.PlantSpeciesId);
            species!.SetCatalogProfile(species.CatalogProfile! with { Categories = ["vegetable"] });
            db.AccountErasures.Add(AccountErasure.Create(owner, PlantCareApiFactory.UtcNow));
        });
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Rows);
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Plants);
        Assert.Empty((await client.GetFromJsonAsync<HarvestOverviewDto>(url))!.Months);
    }

    [Fact]
    public async Task TrendsGroupByRecordedCalendarMonthAndPlantAcrossYearBoundaries()
    {
        var (owner, id) = await Seed();
        Guid secondId = Guid.Empty;
        await factory.SeedAsync(async db => {
            var plant = await db.UserPlants.FindAsync(id); var species = await db.PlantSpecies.FindAsync(plant!.PlantSpeciesId);
            species!.SetCatalogProfile(new PlantCare.Domain.ValueObjects.PlantCatalogProfile("https://example.org/crop", new DateOnly(2025, 1, 1), null, Categories: ["vegetable"]));
            var second = UserPlant.Create(owner, species.Id, "Second crop", null, new DateOnly(2025,1,1), null, PlantCareApiFactory.UtcNow);
            secondId = second.Id; db.UserPlants.Add(second);
            foreach (var date in new[] { new DateOnly(2025,12,31), new DateOnly(2026,1,1) })
                db.HarvestRecords.Add(HarvestRecord.Create(id, date, 1.001m, "kg", null, PlantCareApiFactory.UtcNow));
            db.HarvestRecords.Add(HarvestRecord.Create(secondId, new DateOnly(2026,1,1), 3m, "g", null, PlantCareApiFactory.UtcNow));
            db.HarvestRecords.Add(HarvestRecord.Create(id, new DateOnly(2025,12,30), 99m, "items", null, PlantCareApiFactory.UtcNow));
        });
        using var client = await Client(owner);
        var result = (await client.GetFromJsonAsync<HarvestOverviewDto>("/api/my-plants/harvest-overview?from=2025-12-31&to=2026-01-01"))!;
        Assert.Equal(3, result.Months.Count);
        Assert.Equal("2025-12", result.Months[0].Month);
        Assert.Equal("1.001", Assert.Single(result.Months, m => m.Month == "2025-12").Quantities["kg"]);
        Assert.Equal("3.000", Assert.Single(result.Months, m => m.UserPlantId == secondId).Quantities["g"]);
        Assert.All(result.Months, month => Assert.Equal(1, month.RecordCount));
    }

}