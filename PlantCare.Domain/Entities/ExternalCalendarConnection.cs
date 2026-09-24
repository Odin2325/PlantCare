namespace PlantCare.Domain.Entities;

public sealed class ExternalCalendarConnection
{
    private ExternalCalendarConnection() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string AccountEmail { get; private set; } = string.Empty;
    public string ProtectedAccessToken { get; private set; } = string.Empty;
    public string ProtectedRefreshToken { get; private set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastSyncedAtUtc { get; private set; }

    public static ExternalCalendarConnection CreateGoogle(
        Guid userId,
        string accountEmail,
        string protectedAccessToken,
        string protectedRefreshToken,
        DateTimeOffset accessTokenExpiresAtUtc,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Google",
            AccountEmail = accountEmail,
            ProtectedAccessToken = protectedAccessToken,
            ProtectedRefreshToken = protectedRefreshToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            CreatedAtUtc = createdAtUtc
        };

    public static ExternalCalendarConnection CreateMicrosoft(
        Guid userId,
        string accountEmail,
        string protectedAccessToken,
        string protectedRefreshToken,
        DateTimeOffset accessTokenExpiresAtUtc,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Microsoft",
            AccountEmail = accountEmail,
            ProtectedAccessToken = protectedAccessToken,
            ProtectedRefreshToken = protectedRefreshToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            CreatedAtUtc = createdAtUtc
        };

    public void Reconnect(
        string accountEmail,
        string protectedAccessToken,
        string? protectedRefreshToken,
        DateTimeOffset accessTokenExpiresAtUtc)
    {
        AccountEmail = accountEmail;
        ProtectedAccessToken = protectedAccessToken;
        if (!string.IsNullOrWhiteSpace(protectedRefreshToken))
            ProtectedRefreshToken = protectedRefreshToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
    }

    public void RefreshAccessToken(
        string protectedAccessToken,
        DateTimeOffset accessTokenExpiresAtUtc)
    {
        ProtectedAccessToken = protectedAccessToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
    }

    public void MarkSynced(DateTimeOffset syncedAtUtc) =>
        LastSyncedAtUtc = syncedAtUtc;
}
