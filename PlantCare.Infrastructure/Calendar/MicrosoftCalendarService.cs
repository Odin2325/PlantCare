using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Application.Calendar;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Calendar;

internal sealed class MicrosoftCalendarService : IMicrosoftCalendarService
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private const string Scope =
        "openid email offline_access User.Read Calendars.ReadWrite";

    private readonly HttpClient httpClient;
    private readonly MicrosoftCalendarOptions options;
    private readonly IExternalCalendarConnectionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICalendarService calendarService;
    private readonly TimeProvider timeProvider;
    private readonly IDataProtector tokenProtector;
    private readonly ITimeLimitedDataProtector stateProtector;

    public MicrosoftCalendarService(
        HttpClient httpClient,
        IOptions<MicrosoftCalendarOptions> options,
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
            "PlantCare.MicrosoftCalendar.Tokens.v1");
        stateProtector = dataProtectionProvider
            .CreateProtector("PlantCare.MicrosoftCalendar.State.v1")
            .ToTimeLimitedDataProtector();
    }

    public async Task<ExternalCalendarStatusDto> GetStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connection = options.Enabled && options.IsComplete
            ? await repository.GetMicrosoftAsync(userId, cancellationToken)
            : null;
        return new ExternalCalendarStatusDto(
            options.Enabled && options.IsComplete,
            connection is not null,
            connection?.AccountEmail,
            connection?.LastSyncedAtUtc);
    }

    public string CreateAuthorizationUrl(Guid userId)
    {
        EnsureConfigured();
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64UrlEncode(
            SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = stateProtector.Protect(
            $"{userId:N}|{verifier}",
            TimeSpan.FromMinutes(10));
        var endpoint =
            $"https://login.microsoftonline.com/{options.Tenant}/oauth2/v2.0/authorize";

        return endpoint + "?" + string.Join('&', new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = options.CallbackUrl,
            ["response_mode"] = "query",
            ["scope"] = Scope,
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        }.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public async Task CompleteAuthorizationAsync(
        Guid userId,
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var stateValue = stateProtector.Unprotect(state, out _);
        var stateParts = stateValue.Split('|', 2);
        if (stateParts.Length != 2 ||
            !Guid.TryParseExact(stateParts[0], "N", out var stateUserId) ||
            stateUserId != userId)
            throw new InvalidOperationException(
                "The Microsoft authorization state is invalid.");

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["code"] = code,
                ["code_verifier"] = stateParts[1],
                ["redirect_uri"] = options.CallbackUrl,
                ["grant_type"] = "authorization_code",
                ["scope"] = Scope
            },
            cancellationToken);
        var accountEmail = await GetAccountEmailAsync(
            token.AccessToken,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var connection = await repository.GetMicrosoftAsync(
            userId,
            cancellationToken);
        var protectedAccessToken = tokenProtector.Protect(token.AccessToken);
        var protectedRefreshToken = string.IsNullOrWhiteSpace(token.RefreshToken)
            ? null
            : tokenProtector.Protect(token.RefreshToken);

        if (connection is null)
        {
            if (protectedRefreshToken is null)
                throw new InvalidOperationException(
                    "Microsoft did not issue an offline refresh token.");
            repository.Add(ExternalCalendarConnection.CreateMicrosoft(
                userId,
                accountEmail,
                protectedAccessToken,
                protectedRefreshToken,
                now.AddSeconds(token.ExpiresIn),
                now));
        }
        else
        {
            connection.Reconnect(
                accountEmail,
                protectedAccessToken,
                protectedRefreshToken,
                now.AddSeconds(token.ExpiresIn));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExternalCalendarSyncResultDto> SyncAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var connection = await repository.GetMicrosoftAsync(
            userId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Microsoft Calendar is not connected.");
        var accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var entries = await calendarService.GetEntriesAsync(
            userId,
            now,
            now.AddDays(366),
            cancellationToken);
        var mappings = await repository.GetEventsAsync(
            connection.Id,
            cancellationToken);
        var mappingByEntry = mappings.ToDictionary(
            mapping => mapping.PlantCareEntryId);
        var expectedIds = entries.Select(entry => entry.Id).ToHashSet();
        var created = 0;
        var updated = 0;
        var deleted = 0;

        foreach (var entry in entries)
        {
            var body = CreateEventBody(
                entry,
                includeTransactionId: !mappingByEntry.ContainsKey(entry.Id));
            if (mappingByEntry.TryGetValue(entry.Id, out var mapping))
            {
                var updateStatus = await SendGraphAsync(
                    HttpMethod.Patch,
                    $"{GraphBaseUrl}/me/events/{Uri.EscapeDataString(mapping.ExternalEventId)}",
                    accessToken,
                    body,
                    cancellationToken);
                if (updateStatus == HttpStatusCode.NotFound)
                {
                    mapping.ReplaceExternalEventId(await CreateEventAsync(
                        accessToken,
                        CreateEventBody(entry, includeTransactionId: true),
                        cancellationToken));
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            else
            {
                var externalEventId = await CreateEventAsync(
                    accessToken,
                    body,
                    cancellationToken);
                repository.AddEvent(ExternalCalendarEvent.Create(
                    connection.Id,
                    entry.Id,
                    externalEventId,
                    now));
                created++;
            }
        }

        foreach (var staleMapping in mappings.Where(mapping =>
                     !expectedIds.Contains(mapping.PlantCareEntryId)))
        {
            await SendGraphAsync(
                HttpMethod.Delete,
                $"{GraphBaseUrl}/me/events/{Uri.EscapeDataString(staleMapping.ExternalEventId)}",
                accessToken,
                null,
                cancellationToken);
            repository.RemoveEvent(staleMapping);
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

    public async Task DisconnectAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetMicrosoftAsync(
            userId,
            cancellationToken);
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

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["refresh_token"] = tokenProtector.Unprotect(
                    connection.ProtectedRefreshToken),
                ["grant_type"] = "refresh_token",
                ["scope"] = Scope
            },
            cancellationToken);
        connection.Reconnect(
            connection.AccountEmail,
            tokenProtector.Protect(token.AccessToken),
            string.IsNullOrWhiteSpace(token.RefreshToken)
                ? null
                : tokenProtector.Protect(token.RefreshToken),
            now.AddSeconds(token.ExpiresIn));
        return token.AccessToken;
    }

    private async Task<MicrosoftTokenResponse> RequestTokenAsync(
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var endpoint =
            $"https://login.microsoftonline.com/{options.Tenant}/oauth2/v2.0/token";
        using var response = await httpClient.PostAsync(
            endpoint,
            new FormUrlEncodedContent(values),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Microsoft did not return an OAuth token.");
    }

    private async Task<string> GetAccountEmailAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"{GraphBaseUrl}/me?$select=mail,userPrincipalName",
            accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<MicrosoftAccount>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Microsoft did not return account information.");
        return account.Mail ?? account.UserPrincipalName
            ?? throw new InvalidOperationException(
                "The Microsoft account has no usable email address.");
    }

    private async Task<string> CreateEventAsync(
        string accessToken,
        JsonObject body,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{GraphBaseUrl}/me/events",
            accessToken);
        request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var eventJson = await response.Content.ReadFromJsonAsync<JsonObject>(
            cancellationToken);
        return eventJson?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException(
                "Microsoft did not return the created event ID.");
    }

    private async Task<HttpStatusCode> SendGraphAsync(
        HttpMethod method,
        string url,
        string accessToken,
        JsonObject? body,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(method, url, accessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
        return response.StatusCode;
    }

    private static JsonObject CreateEventBody(
        CalendarEntryDto entry,
        bool includeTransactionId)
    {
        var body = new JsonObject
        {
            ["subject"] = $"{entry.ActionType}: {entry.PlantName}",
            ["body"] = new JsonObject
            {
                ["contentType"] = "text",
                ["content"] = "Plant care scheduled by PlantCare."
            },
            ["start"] = new JsonObject
            {
                ["dateTime"] = entry.StartsAtUtc.UtcDateTime.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffffff"),
                ["timeZone"] = "UTC"
            },
            ["end"] = new JsonObject
            {
                ["dateTime"] = entry.StartsAtUtc.AddMinutes(30).UtcDateTime.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffffff"),
                ["timeZone"] = "UTC"
            },
            ["showAs"] = "free"
        };

        if (includeTransactionId)
        {
            body["transactionId"] = $"plantcare-{entry.Id}";
        }

        return body;
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
            throw new InvalidOperationException(
                "Microsoft Calendar is not configured.");
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed record MicrosoftTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record MicrosoftAccount(
        [property: JsonPropertyName("mail")] string? Mail,
        [property: JsonPropertyName("userPrincipalName")] string? UserPrincipalName);
}
