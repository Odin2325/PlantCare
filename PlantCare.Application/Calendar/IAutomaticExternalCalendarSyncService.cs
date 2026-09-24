namespace PlantCare.Application.Calendar;

public interface IAutomaticExternalCalendarSyncService
{
    Task<AutomaticExternalCalendarSyncResultDto> SyncDueAsync(
        TimeSpan minimumSyncInterval,
        int batchSize,
        CancellationToken cancellationToken = default);
}

public sealed record AutomaticExternalCalendarSyncResultDto(
    int Attempted,
    int Succeeded,
    int Failed);
