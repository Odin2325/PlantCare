using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(PlantCareDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<CareSchedule>> GetDueSchedulesAsync(DateTimeOffset nowUtc, int take, CancellationToken cancellationToken = default) =>
        await dbContext.CareSchedules.AsNoTracking()
            .Include(schedule => schedule.UserPlant)
            .Where(schedule => schedule.IsEnabled && !schedule.IsArchived && schedule.UserPlant.IsActive &&
                schedule.NextDueAtUtc != null && schedule.NextDueAtUtc <= nowUtc &&
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
