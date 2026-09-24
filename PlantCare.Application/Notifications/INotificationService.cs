namespace PlantCare.Application.Notifications;

public interface INotificationService
{
    Task<int> GenerateDueAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        Guid userId,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDto> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid userId,
        UpdateNotificationPreferenceCommand command,
        CancellationToken cancellationToken = default);
}
