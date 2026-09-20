using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Application.Calendar;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Calendar;

internal sealed class GoogleCalendarService : IExternalCalendarService
{
    private const string AuthorizationEndpoint =
        "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint =
        "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint =
        "https://openidconnect.googleapis.com/v1/userinfo";
    private const string EventsEndpoint =
        "https://www.googleapis.com/calendar/v3/calendars/primary/events";

    private readonly HttpClient httpClient;
    private readonly GoogleCalendarOptions options;
    private readonly IExternalCalendarConnectionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICalendarService calendarService;
    private readonly TimeProvider timeProvider;
    private readonly IDataProtector tokenProtector;
    private readonly ITimeLimitedDataProtector stateProtector;

    public GoogleCalendarService(
        HttpClient httpClient,
        IOptions<GoogleCalendarOptions> options,
        IExternalCalendarConnectionRepository repository,
        IUnitOfWork unitOfWork,
        ICalendarService calendarService,
        TimeProvider timeProvider,
        IDataProtectionProvider dataProtectionProvider)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.calendarService = calendarService;
        this.timeProvider = timeProvider;
        tokenProtector = dataProtectionProvider.CreateProtector(
            "PlantCare.GoogleCalendar.Tokens.v1");
        stateProtector = dataProtectionProvider
            .CreateProtector("PlantCare.GoogleCalendar.State.v1")
            .ToTimeLimitedDataProtector();
    }

    public async Task<ExternalCalendarStatusDto> GetGoogleStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connection = options.Enabled && options.IsComplete
            ? await repository.GetGoogleAsync(userId, cancellationToken)
            : null;

        return new ExternalCalendarStatusDto(
            options.Enabled && options.IsComplete,
            connection is not null,
            connection?.AccountEmail,
            connection?.LastSyncedAtUtc);
    }

    public string CreateGoogleAuthorizationUrl(Guid userId)
    {
        EnsureConfigured();
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        var state = stateProtector.Protect(
            userId.ToString("N"),
            TimeSpan.FromMinutes(10));
        var scope = string.Join(' ',
            "openid",
            "email",
            "https://www.googleapis.com/auth/calendar.events");

        return AuthorizationEndpoint + "?" + string.Join('&', new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = options.CallbackUrl,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = state
        }.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public async Task CompleteGoogleAuthorizationAsync(
        Guid userId,
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var stateUserId = stateProtector.Unprotect(state, out _);
        if (!Guid.TryParseExact(stateUserId, "N", out var parsedUserId) ||
            parsedUserId != userId)
            throw new InvalidOperationException("The Google authorization state is invalid.");

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = options.CallbackUrl
            },
            cancellationToken);

        using var userInfoRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            UserInfoEndpoint,
            token.AccessToken);
        using var userInfoResponse = await httpClient.SendAsync(
            userInfoRequest,
            cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();
        var userInfo = await userInfoResponse.Content
            .ReadFromJsonAsync<GoogleUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Google did not return account information.");

        var now = timeProvider.GetUtcNow();
        var connection = await repository.GetGoogleAsync(
            userId,
            cancellationToken);
        var protectedAccessToken = tokenProtector.Protect(token.AccessToken);
        var protectedRefreshToken = string.IsNullOrWhiteSpace(token.RefreshToken)
            ? null
            : tokenProtector.Protect(token.RefreshToken);

        if (connection is null)
        {
            if (protectedRefreshToken is null)
                throw new InvalidOperationException("Google did not issue an offline refresh token.");
            repository.Add(ExternalCalendarConnection.CreateGoogle(
                userId,
                userInfo.Email,
                protectedAccessToken,
                protectedRefreshToken,
                now.AddSeconds(token.ExpiresIn),
                now));
        }
        else
        {
            connection.Reconnect(
                userInfo.Email,
                protectedAccessToken,
                protectedRefreshToken,
                now.AddSeconds(token.ExpiresIn));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExternalCalendarSyncResultDto> SyncGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var connection = await repository.GetGoogleAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Google Calendar is not connected.");
        var accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var toUtc = now.AddDays(366);
        var entries = await calendarService.GetEntriesAsync(
            userId,
            now,
            toUtc,
            cancellationToken);
        var existingEvents = await GetPlantCareEventsAsync(
            accessToken,
            now,
            toUtc,
            cancellationToken);
        var expectedIds = entries.Select(entry => entry.Id).ToHashSet();
        var created = 0;
        var updated = 0;
        var deleted = 0;

        foreach (var entry in entries)
        {
            var body = CreateEventBody(entry);
            if (existingEvents.TryGetValue(entry.Id, out var googleEventId))
            {
                await SendGoogleAsync(
                    HttpMethod.Put,
                    $"{EventsEndpoint}/{Uri.EscapeDataString(googleEventId)}",
                    accessToken,
                    body,
                    cancellationToken);
                updated++;
            }
            else
            {
                await SendGoogleAsync(
                    HttpMethod.Post,
                    EventsEndpoint,
                    accessToken,
                    body,
                    cancellationToken);
                created++;
            }
        }

        foreach (var staleEvent in existingEvents.Where(pair =>
                     !expectedIds.Contains(pair.Key)))
        {
            await SendGoogleAsync(
                HttpMethod.Delete,
                $"{EventsEndpoint}/{Uri.EscapeDataString(staleEvent.Value)}",
                accessToken,
                null,
                cancellationToken);
            deleted++;
        }

        connection.MarkSynced(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ExternalCalendarSyncResultDto(
            created,
            updated,
            deleted,
            now);
    }

    public async Task DisconnectGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetGoogleAsync(userId, cancellationToken);
        if (connection is null) return;
        repository.Remove(connection);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(
        ExternalCalendarConnection connection,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (connection.AccessTokenExpiresAtUtc > now.AddMinutes(1))
            return tokenProtector.Unprotect(connection.ProtectedAccessToken);

        var refreshToken = tokenProtector.Unprotect(
            connection.ProtectedRefreshToken);
        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            },
            cancellationToken);
        connection.RefreshAccessToken(
            tokenProtector.Protect(token.AccessToken),
            now.AddSeconds(token.ExpiresIn));
        return token.AccessToken;
    }

    private async Task<GoogleTokenResponse> RequestTokenAsync(
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            TokenEndpoint,
            new FormUrlEncodedContent(values),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(
            cancellationToken)
            ?? throw new InvalidOperationException("Google did not return an OAuth token.");
    }

    private async Task<Dictionary<string, string>> GetPlantCareEventsAsync(
        string accessToken,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var url = EventsEndpoint + "?" + string.Join('&',
            $"timeMin={Uri.EscapeDataString(fromUtc.ToString("O"))}",
            $"timeMax={Uri.EscapeDataString(toUtc.ToString("O"))}",
            "singleEvents=true",
            "maxResults=2500",
            $"privateExtendedProperty={Uri.EscapeDataString("plantCare=true")}");
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            url,
            accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var document = await JsonNode.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        return document?["items"]?.AsArray()
            .Select(item => new
            {
                GoogleId = item?["id"]?.GetValue<string>(),
                PlantCareId = item?["extendedProperties"]?["private"]?["plantCareId"]?.GetValue<string>()
            })
            .Where(item => item.GoogleId is not null && item.PlantCareId is not null)
            .ToDictionary(item => item.PlantCareId!, item => item.GoogleId!)
            ?? new Dictionary<string, string>();
    }

    private static JsonObject CreateEventBody(CalendarEntryDto entry) =>
        new()
        {
            ["summary"] = $"{entry.ActionType}: {entry.PlantName}",
            ["description"] = "Plant care scheduled by PlantCare.",
            ["start"] = new JsonObject
            {
                ["dateTime"] = entry.StartsAtUtc.ToString("O"),
                ["timeZone"] = "UTC"
            },
            ["end"] = new JsonObject
            {
                ["dateTime"] = entry.StartsAtUtc.AddMinutes(30).ToString("O"),
                ["timeZone"] = "UTC"
            },
            ["extendedProperties"] = new JsonObject
            {
                ["private"] = new JsonObject
                {
                    ["plantCare"] = "true",
                    ["plantCareId"] = entry.Id
                }
            }
        };

    private async Task SendGoogleAsync(
        HttpMethod method,
        string url,
        string accessToken,
        JsonObject? body,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(method, url, accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        return request;
    }

    private void EnsureConfigured()
    {
        if (!options.Enabled || !options.IsComplete)
            throw new InvalidOperationException("Google Calendar is not configured.");
    }

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record GoogleUserInfo(
        [property: JsonPropertyName("email")] string Email);
}
