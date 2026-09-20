using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface IPushSubscriptionRepository
{
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<PushSubscription?> GetForUserAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    void Add(PushSubscription subscription);
    void Remove(PushSubscription subscription);
}
