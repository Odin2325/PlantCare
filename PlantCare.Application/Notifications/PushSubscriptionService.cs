using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Application.Notifications;

internal sealed class PushSubscriptionService(IPushSubscriptionRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IPushSubscriptionService
{
    public async Task<PushSubscriptionDto> SaveAsync(Guid userId, SavePushSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var subscription = await repository.GetByEndpointAsync(command.Endpoint, cancellationToken);
        if (subscription is not null && subscription.UserId != userId) throw new InvalidOperationException("The push endpoint belongs to another user.");
        if (subscription is null) { subscription = PushSubscription.Create(userId, command.Endpoint, command.P256dh, command.Auth, now); repository.Add(subscription); }
        else subscription.Update(command.P256dh, command.Auth, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(subscription.Id, subscription.Endpoint, subscription.CreatedAtUtc);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetForUserAsync(userId, subscriptionId, cancellationToken);
        if (subscription is null) return false;
        repository.Remove(subscription); await unitOfWork.SaveChangesAsync(cancellationToken); return true;
    }
}
