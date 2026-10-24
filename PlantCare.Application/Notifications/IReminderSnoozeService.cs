namespace PlantCare.Application.Notifications;

public enum SnoozeStatus { Success, NotFound, NotCurrent }
public sealed record SnoozeReminderResult(SnoozeStatus Status, NotificationDto? Notification = null);
public interface IReminderSnoozeService
{
    Task<SnoozeReminderResult> SnoozeAsync(Guid userId, Guid notificationId, int hours, CancellationToken ct = default);
}
