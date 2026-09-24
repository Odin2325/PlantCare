using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Application.Notifications;

internal sealed class NotificationService(
    INotificationRepository notificationRepository,
    INotificationPreferenceRepository preferenceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : INotificationService
{
    public async Task<int> GenerateDueAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        var now = timeProvider.GetUtcNow();
        var schedules = await notificationRepository
            .GetDueSchedulesAsync(now, batchSize, cancellationToken);

        foreach (var schedule in schedules)
        {
            notificationRepository.Add(Notification.Create(
                schedule.UserPlant.UserId,
                schedule.Id,
                schedule.NextDueAtUtc!.Value,
                now));
        }

        if (schedules.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return schedules.Count;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        Guid userId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take));

        var notifications = await notificationRepository
            .GetForUserAsync(userId, take, cancellationToken);
        var preference = await preferenceRepository.GetAsync(
            userId,
            cancellationToken);
        if (preference is not null && !preference.InAppEnabled)
            return [];

        return notifications
            .Where(notification => preference is null ||
                preference.IsActionEnabled(notification.CareSchedule.ActionType))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid notification and user IDs are required.");

        var notification = await notificationRepository
            .GetTrackedForUserAsync(notificationId, userId, cancellationToken);
        if (notification is null)
            return false;

        notification.MarkRead(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        var preference = await preferenceRepository.GetAsync(
            userId,
            cancellationToken);
        return MapPreference(preference ??
            NotificationPreference.CreateDefault(userId));
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid userId,
        UpdateNotificationPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        var preference = await preferenceRepository.GetAsync(
            userId,
            cancellationToken);
        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(userId);
            preferenceRepository.Add(preference);
        }

        preference.Update(
            command.InAppEnabled,
            command.PushEnabled,
            command.WateringEnabled,
            command.FertilizingEnabled,
            command.MistingEnabled,
            command.PruningEnabled,
            command.RepottingEnabled,
            command.ReminderLeadTimeHours);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPreference(preference);
    }

    private static NotificationDto MapToDto(Notification notification) =>
        new(
            notification.Id,
            notification.CareSchedule.UserPlantId,
            notification.CareSchedule.UserPlant.Nickname,
            notification.CareSchedule.ActionType,
            notification.DueAtUtc,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);

    private static NotificationPreferenceDto MapPreference(
        NotificationPreference preference) =>
        new(
            preference.InAppEnabled,
            preference.PushEnabled,
            preference.WateringEnabled,
            preference.FertilizingEnabled,
            preference.MistingEnabled,
            preference.PruningEnabled,
            preference.RepottingEnabled,
            preference.ReminderLeadTimeHours);
}
