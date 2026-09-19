using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface ICalendarSubscriptionRepository
{
    Task<CalendarSubscription?> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CalendarSubscription?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(CalendarSubscription subscription);
}
