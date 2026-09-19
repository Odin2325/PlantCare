using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Api.IntegrationTests;

public sealed class PlantCareApiTests
    : IAsyncLifetime
{
    private readonly PlantCareApiFactory factory = new();

    public Task InitializeAsync() =>
        factory.ResetDatabaseAsync();

    public async Task DisposeAsync() =>
        await factory.DisposeAsync();

    [Fact]
    public async Task UserCannotReadAnotherUsersPlant()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(ownerId);
        using var client = CreateClient();
        client.AuthenticateAs(otherUserId);

        using var response = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotModifyAnotherUsersPlant()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(ownerId);
        using var client = CreateClient();
        client.AuthenticateAs(otherUserId);
        await client.AddAntiforgeryTokenAsync();

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}",
            new
            {
                nickname = "Stolen plant",
                location = "Other home",
                acquiredOn = (DateOnly?)null,
                notes = (string?)null
            });
        using var archiveResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            archiveResponse.StatusCode);

        client.AuthenticateAs(ownerId);
        using var ownerResponse = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}");
        ownerResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await ownerResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Kitchen Monstera",
            document.RootElement.GetProperty("nickname").GetString());
        Assert.True(
            document.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task UpdatingSpeciesPersistsTheChanges()
    {
        var (plantSpecies, _) = await SeedPlantAsync(Guid.NewGuid());
        using var client = CreateClient();
        client.AuthenticateAs(Guid.NewGuid(), "Admin");
        await client.AddAntiforgeryTokenAsync();

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/plant-species/{plantSpecies.Id}",
            CreateSpeciesRequest("Updated Monstera"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var getResponse = await client.GetAsync(
            $"/api/plant-species/{plantSpecies.Id}");
        getResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Updated Monstera",
            document.RootElement
                .GetProperty("commonName")
                .GetString());
    }

    [Fact]
    public async Task UnsafeCookieRequestWithoutAntiforgeryTokenIsRejected()
    {
        var (plantSpecies, _) = await SeedPlantAsync(Guid.NewGuid());
        using var client = CreateClient();
        client.AuthenticateAs(Guid.NewGuid(), "Admin");

        using var response = await client.PutAsJsonAsync(
            $"/api/plant-species/{plantSpecies.Id}",
            CreateSpeciesRequest("Updated Monstera"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ArchivedPlantIsHiddenAndCannotReceiveCare()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var archiveResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}");
        Assert.Equal(
            HttpStatusCode.NoContent,
            archiveResponse.StatusCode);

        using var dashboardResponse = await client.GetAsync(
            "/api/dashboard/care?daysAhead=30");
        dashboardResponse.EnsureSuccessStatusCode();
        Assert.Equal(
            0,
            JsonDocument.Parse(
                    await dashboardResponse.Content.ReadAsStringAsync())
                .RootElement
                .GetArrayLength());

        using var completionResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/complete",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow,
                notes = "Should not be recorded"
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            completionResponse.StatusCode);
    }

    [Fact]
    public async Task CompletingCareCreatesHistoryAndAdvancesSchedule()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        var completedAtUtc = PlantCareApiFactory.UtcNow.AddDays(-1);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var completionResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/complete",
            new
            {
                completedAtUtc,
                notes = "Watered thoroughly"
            });
        completionResponse.EnsureSuccessStatusCode();

        using var completionDocument = JsonDocument.Parse(
            await completionResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            completedAtUtc.AddDays(7),
            completionDocument.RootElement
                .GetProperty("schedule")
                .GetProperty("nextDueAtUtc")
                .GetDateTimeOffset());

        using var historyResponse = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}/care/history");
        historyResponse.EnsureSuccessStatusCode();

        using var historyDocument = JsonDocument.Parse(
            await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, historyDocument.RootElement.GetArrayLength());
        Assert.Equal(
            "Watered thoroughly",
            historyDocument.RootElement[0]
                .GetProperty("notes")
                .GetString());
    }

    [Fact]
    public async Task FutureCareCompletionIsRejectedWithoutCreatingHistory()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var completionResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/complete",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow.AddMinutes(1),
                notes = "From the future"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            completionResponse.StatusCode);

        using var historyResponse = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}/care/history");
        historyResponse.EnsureSuccessStatusCode();

        using var historyDocument = JsonDocument.Parse(
            await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, historyDocument.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task OutOfOrderCareCompletionIsRejected()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var firstResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/complete",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow.AddDays(-1),
                notes = "Latest care"
            });
        firstResponse.EnsureSuccessStatusCode();

        using var secondResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/complete",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow.AddDays(-2),
                notes = "Older care"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);

        using var historyResponse = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}/care/history");
        historyResponse.EnsureSuccessStatusCode();

        using var historyDocument = JsonDocument.Parse(
            await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, historyDocument.RootElement.GetArrayLength());
        Assert.Equal(
            "Latest care",
            historyDocument.RootElement[0]
                .GetProperty("notes")
                .GetString());
    }

    [Fact]
    public async Task DeletingSpeciesInUseReturnsConflict()
    {
        var (plantSpecies, _) = await SeedPlantAsync(Guid.NewGuid());
        using var client = CreateClient();
        client.AuthenticateAs(Guid.NewGuid(), "Admin");
        await client.AddAntiforgeryTokenAsync();

        using var deleteResponse = await client.DeleteAsync(
            $"/api/plant-species/{plantSpecies.Id}");

        Assert.Equal(
            HttpStatusCode.Conflict,
            deleteResponse.StatusCode);

        using var getResponse = await client.GetAsync(
            $"/api/plant-species/{plantSpecies.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });
    }

    private async Task<(PlantSpecies PlantSpecies, UserPlant UserPlant)>
        SeedPlantAsync(Guid userId)
    {
        var plantSpecies = CreateSpecies();
        var userPlant = UserPlant.Create(
            userId,
            plantSpecies.Id,
            "Kitchen Monstera",
            "Kitchen",
            null,
            null,
            PlantCareApiFactory.UtcNow.AddDays(-10));
        userPlant.AddCareSchedule(
            CareActionType.Watering,
            7,
            PlantCareApiFactory.UtcNow.AddDays(-10));

        await factory.SeedAsync(dbContext =>
        {
            dbContext.PlantSpecies.Add(plantSpecies);
            dbContext.UserPlants.Add(userPlant);
            return Task.CompletedTask;
        });

        return (plantSpecies, userPlant);
    }

    private static PlantSpecies CreateSpecies()
    {
        return PlantSpecies.Create(
            commonName: "Monstera",
            scientificName: "Monstera deliciosa",
            description: "A tropical houseplant.",
            sunlightRequirement:
                SunlightRequirement.BrightIndirectLight,
            sunlightInstructions: "Provide bright indirect light.",
            defaultWateringIntervalDays: 7,
            wateringInstructions: "Water when the top soil is dry.",
            defaultFertilizingIntervalDays: null,
            fertilizingInstructions: null,
            soilInstructions: "Use well-draining soil.",
            humidityInstructions: "Prefers moderate humidity.",
            minimumTemperatureCelsius: 18,
            maximumTemperatureCelsius: 30,
            isToxicToPets: true);
    }

    private static object CreateSpeciesRequest(string commonName)
    {
        return new
        {
            commonName,
            scientificName = "Monstera deliciosa",
            description = "An updated tropical houseplant.",
            sunlightRequirement = "BrightIndirectLight",
            sunlightInstructions = "Provide bright indirect light.",
            defaultWateringIntervalDays = 7,
            wateringInstructions = "Water when the top soil is dry.",
            defaultFertilizingIntervalDays = (int?)null,
            fertilizingInstructions = (string?)null,
            soilInstructions = "Use well-draining soil.",
            humidityInstructions = "Prefers moderate humidity.",
            minimumTemperatureCelsius = 18,
            maximumTemperatureCelsius = 30,
            isToxicToPets = true
        };
    }
}
