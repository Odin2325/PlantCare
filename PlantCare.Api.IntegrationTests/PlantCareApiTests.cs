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
    public async Task HealthEndpointsDistinguishLivenessAndReadiness()
    {
        using var client = CreateClient();
        using var liveResponse = await client.GetAsync("/health/live");
        using var readyResponse = await client.GetAsync("/health/ready");

        liveResponse.EnsureSuccessStatusCode();
        readyResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResponsesIncludeProtectiveSecurityHeaders()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task UnknownApiRouteReturnsNotFoundInsteadOfSpaShell()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/api/route-that-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

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
    public async Task PasswordResetRequestDoesNotRevealUnknownEmail()
    {
        using var client = CreateClient();
        await client.AddAntiforgeryTokenAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/forgotPassword",
            new
            {
                email = "unknown@example.com"
            });

        response.EnsureSuccessStatusCode();
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
    public async Task OwnerCanViewAndRestoreArchivedPlant()
    {
        var ownerId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(ownerId);
        using var client = CreateClient();
        client.AuthenticateAs(ownerId);
        await client.AddAntiforgeryTokenAsync();

        using var archiveResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}");
        Assert.Equal(
            HttpStatusCode.NoContent,
            archiveResponse.StatusCode);

        client.AuthenticateAs(Guid.NewGuid());
        using var otherUsersArchivedResponse = await client.GetAsync(
            "/api/my-plants/archived");
        otherUsersArchivedResponse.EnsureSuccessStatusCode();
        using var otherUsersDocument = JsonDocument.Parse(
            await otherUsersArchivedResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            0,
            otherUsersDocument.RootElement.GetArrayLength());

        using var unauthorizedRestoreResponse = await client.PostAsync(
            $"/api/my-plants/{userPlant.Id}/restore",
            null);
        Assert.Equal(
            HttpStatusCode.NotFound,
            unauthorizedRestoreResponse.StatusCode);

        client.AuthenticateAs(ownerId);
        using var archivedResponse = await client.GetAsync(
            "/api/my-plants/archived");
        archivedResponse.EnsureSuccessStatusCode();
        using var archivedDocument = JsonDocument.Parse(
            await archivedResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            archivedDocument.RootElement.EnumerateArray(),
            plant => plant.GetProperty("id").GetGuid() == userPlant.Id);

        using var restoreResponse = await client.PostAsync(
            $"/api/my-plants/{userPlant.Id}/restore",
            null);
        Assert.Equal(
            HttpStatusCode.NoContent,
            restoreResponse.StatusCode);

        using var activeResponse = await client.GetAsync(
            "/api/my-plants");
        activeResponse.EnsureSuccessStatusCode();
        using var activeDocument = JsonDocument.Parse(
            await activeResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            activeDocument.RootElement.EnumerateArray(),
            plant => plant.GetProperty("id").GetGuid() == userPlant.Id);

        using var dashboardResponse = await client.GetAsync(
            "/api/dashboard/care?daysAhead=30");
        dashboardResponse.EnsureSuccessStatusCode();
        using var dashboardDocument = JsonDocument.Parse(
            await dashboardResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            dashboardDocument.RootElement.EnumerateArray(),
            item => item.GetProperty("userPlantId").GetGuid() == userPlant.Id);
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

    [Fact]
    public async Task AddingPlantUsesCustomInitialCareSchedules()
    {
        var plantSpecies = await SeedSpeciesAsync();
        var userId = Guid.NewGuid();
        var lastWateredAt = PlantCareApiFactory.UtcNow.AddDays(-2);
        var lastFertilizedAt = PlantCareApiFactory.UtcNow.AddDays(-5);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/my-plants",
            new
            {
                plantSpeciesId = plantSpecies.Id,
                nickname = "Custom Monstera",
                location = "Office",
                acquiredOn = (DateOnly?)null,
                notes = (string?)null,
                wateringIntervalDays = 5,
                lastWateredAtUtc = lastWateredAt,
                fertilizingIntervalDays = 20,
                lastFertilizedAtUtc = lastFertilizedAt
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var schedules = document.RootElement
            .GetProperty("careSchedules")
            .EnumerateArray()
            .ToList();
        var watering = schedules.Single(schedule =>
            schedule.GetProperty("actionType").GetString() ==
            "Watering");
        var fertilizing = schedules.Single(schedule =>
            schedule.GetProperty("actionType").GetString() ==
            "Fertilizing");

        Assert.Equal(5, watering.GetProperty("intervalDays").GetInt32());
        Assert.Equal(
            lastWateredAt,
            watering.GetProperty("lastCompletedAtUtc").GetDateTimeOffset());
        Assert.Equal(
            lastWateredAt.AddDays(5),
            watering.GetProperty("nextDueAtUtc").GetDateTimeOffset());
        Assert.Equal(20, fertilizing.GetProperty("intervalDays").GetInt32());
        Assert.Equal(
            lastFertilizedAt.AddDays(20),
            fertilizing.GetProperty("nextDueAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task AddingPlantRejectsFutureInitialCareDate()
    {
        var plantSpecies = await SeedSpeciesAsync();
        using var client = CreateClient();
        client.AuthenticateAs(Guid.NewGuid());
        await client.AddAntiforgeryTokenAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/my-plants",
            new
            {
                plantSpeciesId = plantSpecies.Id,
                nickname = "Future Monstera",
                location = (string?)null,
                acquiredOn = (DateOnly?)null,
                notes = (string?)null,
                wateringIntervalDays = 7,
                lastWateredAtUtc = PlantCareApiFactory.UtcNow.AddMinutes(1),
                fertilizingIntervalDays = (int?)null,
                lastFertilizedAtUtc = (DateTimeOffset?)null
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OwnerCanEditPauseAndResumeCareSchedule()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var pauseResponse = await client.PutAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/schedule",
            new
            {
                intervalDays = 10,
                isEnabled = false
            });
        pauseResponse.EnsureSuccessStatusCode();

        using var pauseDocument = JsonDocument.Parse(
            await pauseResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            10,
            pauseDocument.RootElement
                .GetProperty("intervalDays")
                .GetInt32());
        Assert.False(
            pauseDocument.RootElement
                .GetProperty("isEnabled")
                .GetBoolean());
        Assert.Equal(
            PlantCareApiFactory.UtcNow,
            pauseDocument.RootElement
                .GetProperty("nextDueAtUtc")
                .GetDateTimeOffset());

        using var completionResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/complete",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow,
                notes = (string?)null
            });
        Assert.Equal(
            HttpStatusCode.BadRequest,
            completionResponse.StatusCode);

        using var resumeResponse = await client.PutAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/schedule",
            new
            {
                intervalDays = 10,
                isEnabled = true
            });
        resumeResponse.EnsureSuccessStatusCode();

        using var resumeDocument = JsonDocument.Parse(
            await resumeResponse.Content.ReadAsStringAsync());
        Assert.True(
            resumeDocument.RootElement
                .GetProperty("isEnabled")
                .GetBoolean());
    }

    [Fact]
    public async Task UserCannotUpdateAnotherUsersCareSchedule()
    {
        var ownerId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(ownerId);
        using var client = CreateClient();
        client.AuthenticateAs(Guid.NewGuid());
        await client.AddAntiforgeryTokenAsync();

        using var response = await client.PutAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Watering/schedule",
            new
            {
                intervalDays = 30,
                isEnabled = false
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemovingAndRestoringOptionalSchedulePreservesHistory()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        using var addResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Fertilizing/schedule",
            new { intervalDays = 30 });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        using var addDocument = JsonDocument.Parse(
            await addResponse.Content.ReadAsStringAsync());
        var scheduleId = addDocument.RootElement
            .GetProperty("id")
            .GetGuid();

        using var completionResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Fertilizing/complete",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow,
                notes = "Spring feed"
            });
        completionResponse.EnsureSuccessStatusCode();

        using var removeResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}/care/Fertilizing/schedule");
        Assert.Equal(
            HttpStatusCode.NoContent,
            removeResponse.StatusCode);

        using var plantResponse = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}");
        plantResponse.EnsureSuccessStatusCode();
        using var plantDocument = JsonDocument.Parse(
            await plantResponse.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            plantDocument.RootElement
                .GetProperty("careSchedules")
                .EnumerateArray(),
            schedule =>
                schedule.GetProperty("actionType").GetString() ==
                "Fertilizing");

        using var historyResponse = await client.GetAsync(
            $"/api/my-plants/{userPlant.Id}/care/history");
        historyResponse.EnsureSuccessStatusCode();
        using var historyDocument = JsonDocument.Parse(
            await historyResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            historyDocument.RootElement.EnumerateArray(),
            careEvent =>
                careEvent.GetProperty("notes").GetString() ==
                "Spring feed");

        using var restoreResponse = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/Fertilizing/schedule",
            new { intervalDays = 45 });
        Assert.Equal(HttpStatusCode.Created, restoreResponse.StatusCode);

        using var restoreDocument = JsonDocument.Parse(
            await restoreResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            scheduleId,
            restoreDocument.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(
            45,
            restoreDocument.RootElement
                .GetProperty("intervalDays")
                .GetInt32());
    }

    [Fact]
    public async Task UpdatingAndDeletingEventsRecalculatesSchedule()
    {
        var userId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(userId);
        using var client = CreateClient();
        client.AuthenticateAs(userId);
        await client.AddAntiforgeryTokenAsync();

        var firstEventId = await CompleteCareAsync(
            client,
            userPlant.Id,
            PlantCareApiFactory.UtcNow.AddDays(-2),
            "First");
        var secondEventId = await CompleteCareAsync(
            client,
            userPlant.Id,
            PlantCareApiFactory.UtcNow.AddDays(-1),
            "Second");

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/history/{firstEventId}",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow,
                notes = "Corrected"
            });
        updateResponse.EnsureSuccessStatusCode();

        var schedule = await GetWateringScheduleAsync(
            client,
            userPlant.Id);
        Assert.Equal(
            PlantCareApiFactory.UtcNow.AddDays(7),
            schedule.GetProperty("nextDueAtUtc").GetDateTimeOffset());

        using var deleteLatestResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}/care/history/{firstEventId}");
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteLatestResponse.StatusCode);

        schedule = await GetWateringScheduleAsync(client, userPlant.Id);
        Assert.Equal(
            PlantCareApiFactory.UtcNow.AddDays(6),
            schedule.GetProperty("nextDueAtUtc").GetDateTimeOffset());

        using var deleteLastResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}/care/history/{secondEventId}");
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteLastResponse.StatusCode);

        schedule = await GetWateringScheduleAsync(client, userPlant.Id);
        Assert.Equal(
            JsonValueKind.Null,
            schedule.GetProperty("lastCompletedAtUtc").ValueKind);
        Assert.Equal(
            PlantCareApiFactory.UtcNow.AddDays(7),
            schedule.GetProperty("nextDueAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task UserCannotModifyAnotherUsersCareEvent()
    {
        var ownerId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(ownerId);
        using var client = CreateClient();
        client.AuthenticateAs(ownerId);
        await client.AddAntiforgeryTokenAsync();
        var eventId = await CompleteCareAsync(
            client,
            userPlant.Id,
            PlantCareApiFactory.UtcNow,
            null);

        client.AuthenticateAs(Guid.NewGuid());
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/my-plants/{userPlant.Id}/care/history/{eventId}",
            new
            {
                completedAtUtc = PlantCareApiFactory.UtcNow,
                notes = "Unauthorized"
            });
        using var deleteResponse = await client.DeleteAsync(
            $"/api/my-plants/{userPlant.Id}/care/history/{eventId}");

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CalendarReturnsOnlyCurrentUsersScheduledAndCompletedCare()
    {
        var ownerId = Guid.NewGuid();
        var (_, userPlant) = await SeedPlantAsync(ownerId);
        using var client = CreateClient();
        client.AuthenticateAs(ownerId);
        await client.AddAntiforgeryTokenAsync();
        await CompleteCareAsync(client, userPlant.Id, PlantCareApiFactory.UtcNow, "Calendar test");

        var from = Uri.EscapeDataString(PlantCareApiFactory.UtcNow.AddDays(-1).ToString("O"));
        var to = Uri.EscapeDataString(PlantCareApiFactory.UtcNow.AddDays(15).ToString("O"));
        using var response = await client.GetAsync($"/api/calendar?fromUtc={from}&toUtc={to}");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Contains(document.RootElement.EnumerateArray(), entry =>
            entry.GetProperty("kind").GetString() == "Completed" &&
            entry.GetProperty("userPlantId").GetGuid() == userPlant.Id);
        Assert.Contains(document.RootElement.EnumerateArray(), entry =>
            entry.GetProperty("kind").GetString() == "Scheduled" &&
            entry.GetProperty("startsAtUtc").GetDateTimeOffset() == PlantCareApiFactory.UtcNow.AddDays(7));

        client.AuthenticateAs(Guid.NewGuid());
        using var otherResponse = await client.GetAsync($"/api/calendar?fromUtc={from}&toUtc={to}");
        otherResponse.EnsureSuccessStatusCode();
        using var otherDocument = JsonDocument.Parse(await otherResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, otherDocument.RootElement.GetArrayLength());
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

    private async Task<PlantSpecies> SeedSpeciesAsync()
    {
        var plantSpecies = CreateSpecies();

        await factory.SeedAsync(dbContext =>
        {
            dbContext.PlantSpecies.Add(plantSpecies);
            return Task.CompletedTask;
        });

        return plantSpecies;
    }

    private static async Task<Guid> CompleteCareAsync(
        HttpClient client,
        Guid userPlantId,
        DateTimeOffset completedAtUtc,
        string? notes)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/my-plants/{userPlantId}/care/Watering/complete",
            new { completedAtUtc, notes });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        return document.RootElement
            .GetProperty("event")
            .GetProperty("id")
            .GetGuid();
    }

    private static async Task<JsonElement> GetWateringScheduleAsync(
        HttpClient client,
        Guid userPlantId)
    {
        using var response = await client.GetAsync(
            $"/api/my-plants/{userPlantId}");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement
            .GetProperty("careSchedules")
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("actionType").GetString() ==
                "Watering")
            .Clone();
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
