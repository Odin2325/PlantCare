using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class CalendarSubscriptionRepository(PlantCareDbContext dbContext) : ICalendarSubscriptionRepository
{
    public Task<CalendarSubscription?> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.CalendarSubscriptions.SingleOrDefaultAsync(subscription => subscription.UserId == userId && subscription.RevokedAtUtc == null, cancellationToken);

    public Task<CalendarSubscription?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.CalendarSubscriptions.AsNoTracking().SingleOrDefaultAsync(subscription => subscription.TokenHash == tokenHash && subscription.RevokedAtUtc == null, cancellationToken);

    public void Add(CalendarSubscription subscription) => dbContext.CalendarSubscriptions.Add(subscription);
}
