namespace PlantCare.Application.Calendar;

public interface IExternalCalendarService
{
    Task<ExternalCalendarStatusDto> GetGoogleStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    string CreateGoogleAuthorizationUrl(Guid userId);

    Task CompleteGoogleAuthorizationAsync(
        Guid userId,
        string code,
        string state,
        CancellationToken cancellationToken = default);

    Task<ExternalCalendarSyncResultDto> SyncGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DisconnectGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record ExternalCalendarStatusDto(
    bool IsConfigured,
    bool IsConnected,
    string? AccountEmail,
    DateTimeOffset? LastSyncedAtUtc);

public sealed record ExternalCalendarSyncResultDto(
    int Created,
    int Updated,
    int Deleted,
    DateTimeOffset SyncedAtUtc);
