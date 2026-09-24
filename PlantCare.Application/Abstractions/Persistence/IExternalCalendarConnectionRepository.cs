using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface IExternalCalendarConnectionRepository
{
    Task<ExternalCalendarConnection?> GetGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ExternalCalendarConnection?> GetMicrosoftAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalCalendarEvent>> GetEventsAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalCalendarConnection>> GetDueForSyncAsync(
        IReadOnlyCollection<string> providers,
        DateTimeOffset syncedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default);

    void Add(ExternalCalendarConnection connection);
    void Remove(ExternalCalendarConnection connection);
    void AddEvent(ExternalCalendarEvent calendarEvent);
    void RemoveEvent(ExternalCalendarEvent calendarEvent);
}
