using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface ICalendarRepository
{
    Task<IReadOnlyList<CareSchedule>> GetSchedulesAsync(Guid userId, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareEvent>> GetCompletedEventsAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
