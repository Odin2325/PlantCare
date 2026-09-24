using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(PlantCareDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<CareSchedule>> GetDueSchedulesAsync(DateTimeOffset nowUtc, int take, CancellationToken cancellationToken = default) =>
        await dbContext.CareSchedules.AsNoTracking()
            .Include(schedule => schedule.UserPlant)
            .Where(schedule => schedule.IsEnabled && !schedule.IsArchived && schedule.UserPlant.IsActive &&
                schedule.NextDueAtUtc != null &&
                (
                    (!dbContext.NotificationPreferences.Any(preference =>
                         preference.UserId == schedule.UserPlant.UserId) &&
                     schedule.NextDueAtUtc <= nowUtc) ||
                    dbContext.NotificationPreferences.Any(preference =>
                        preference.UserId == schedule.UserPlant.UserId &&
                        (preference.InAppEnabled || preference.PushEnabled) &&
                        (schedule.ActionType != CareActionType.Watering || preference.WateringEnabled) &&
                        (schedule.ActionType != CareActionType.Fertilizing || preference.FertilizingEnabled) &&
                        (schedule.ActionType != CareActionType.Misting || preference.MistingEnabled) &&
                        (schedule.ActionType != CareActionType.Pruning || preference.PruningEnabled) &&
                        (schedule.ActionType != CareActionType.Repotting || preference.RepottingEnabled) &&
                        ((preference.ReminderLeadTimeHours == 0 && schedule.NextDueAtUtc <= nowUtc) ||
                         (preference.ReminderLeadTimeHours == 24 && schedule.NextDueAtUtc <= nowUtc.AddHours(24)) ||
                         (preference.ReminderLeadTimeHours == 48 && schedule.NextDueAtUtc <= nowUtc.AddHours(48)) ||
                         (preference.ReminderLeadTimeHours == 72 && schedule.NextDueAtUtc <= nowUtc.AddHours(72)) ||
                         (preference.ReminderLeadTimeHours == 168 && schedule.NextDueAtUtc <= nowUtc.AddHours(168))))
                ) &&
                !dbContext.Notifications.Any(notification => notification.CareScheduleId == schedule.Id && notification.DueAtUtc == schedule.NextDueAtUtc))
            .OrderBy(schedule => schedule.NextDueAtUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications.AsNoTracking()
            .Include(notification => notification.CareSchedule).ThenInclude(schedule => schedule.UserPlant)
            .Where(notification => notification.UserId == userId)
            .OrderBy(notification => notification.ReadAtUtc != null)
            .ThenByDescending(notification => notification.CreatedAtUtc).Take(take).ToListAsync(cancellationToken);

    public Task<Notification?> GetTrackedForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.SingleOrDefaultAsync(notification => notification.Id == notificationId && notification.UserId == userId, cancellationToken);

    public void Add(Notification notification) => dbContext.Notifications.Add(notification);
}
