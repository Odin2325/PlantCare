namespace PlantCare.Application.Notifications;

public interface IPushSubscriptionService
{
    Task<PushSubscriptionDto> SaveAsync(Guid userId, SavePushSubscriptionCommand command, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default);
}

public interface IPushDeliveryService
{
    Task<int> DeliverPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);
}
