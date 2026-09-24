using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(NotificationPreference preference);
}
