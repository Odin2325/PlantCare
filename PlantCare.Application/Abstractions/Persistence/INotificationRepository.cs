using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task<IReadOnlyList<CareSchedule>> GetDueSchedulesAsync(
        DateTimeOffset nowUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetForUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetTrackedForUserAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(Notification notification);
}
