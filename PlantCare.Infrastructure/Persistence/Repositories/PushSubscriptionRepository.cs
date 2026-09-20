using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class PushSubscriptionRepository(PlantCareDbContext dbContext) : IPushSubscriptionRepository
{
    public Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default) => dbContext.PushSubscriptions.SingleOrDefaultAsync(item => item.Endpoint == endpoint, cancellationToken);
    public Task<PushSubscription?> GetForUserAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) => dbContext.PushSubscriptions.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
    public void Add(PushSubscription subscription) => dbContext.PushSubscriptions.Add(subscription);
    public void Remove(PushSubscription subscription) => dbContext.PushSubscriptions.Remove(subscription);
}
