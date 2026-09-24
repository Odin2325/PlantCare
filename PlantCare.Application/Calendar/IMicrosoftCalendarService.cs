namespace PlantCare.Application.Calendar;

public interface IMicrosoftCalendarService
{
    Task<ExternalCalendarStatusDto> GetStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    string CreateAuthorizationUrl(Guid userId);

    Task CompleteAuthorizationAsync(
        Guid userId,
        string code,
        string state,
        CancellationToken cancellationToken = default);

    Task<ExternalCalendarSyncResultDto> SyncAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
