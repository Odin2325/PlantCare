using System.Data;
using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Notifications;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Infrastructure.Notifications;

internal sealed class ReminderSnoozeService(PlantCareDbContext db, TimeProvider clock) : IReminderSnoozeService
{
    public async Task<SnoozeReminderResult> SnoozeAsync(Guid userId, Guid notificationId, int hours, CancellationToken ct = default)
    {
        if (hours is not (1 or 24 or 72)) throw new ArgumentOutOfRangeException(nameof(hours));
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var now = clock.GetUtcNow();
            // Claim before reading: concurrent commands for this ID serialize on
            // one write lock. No accepted delivery outcome is reset or deleted.
            var claimed = await db.Notifications.Where(n => n.Id == notificationId && n.UserId == userId && n.SupersededAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.SupersededAtUtc, now), ct);
            if (claimed == 0)
                return new SnoozeReminderResult(await db.Notifications.AnyAsync(n => n.Id == notificationId && n.UserId == userId, ct)
                    ? SnoozeStatus.NotCurrent : SnoozeStatus.NotFound);
            var previous = await db.Notifications.Include(n => n.CareSchedule).ThenInclude(s => s.UserPlant)
                .SingleAsync(n => n.Id == notificationId && n.UserId == userId, ct);
            var schedule = previous.CareSchedule;
            var preference = await db.NotificationPreferences.SingleOrDefaultAsync(p => p.UserId == userId, ct);
            if (!schedule.IsEnabled || schedule.IsArchived || !schedule.UserPlant.IsActive ||
                schedule.NextDueAtUtc != previous.DueAtUtc || previous.NotBeforeUtc > now ||
                (preference is not null && ((!preference.InAppEnabled && !preference.PushEnabled) || !preference.IsActionEnabled(schedule.ActionType))))
                return new SnoozeReminderResult(SnoozeStatus.NotCurrent); // Disposing the transaction rolls back the claim.
            var next = Notification.CreateFollowUp(previous, now, hours);
            // The API has no delivery-ledger write permission. The worker will
            // close remaining pending attempts without changing accepted ones.
            if (previous.PushSentAtUtc is null) previous.DeferPush(now);
            db.Notifications.Add(next);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new SnoozeReminderResult(SnoozeStatus.Success, new NotificationDto(next.Id, schedule.UserPlantId,
                schedule.UserPlant.Nickname, schedule.ActionType, next.DueAtUtc, next.CreatedAtUtc,
                null, next.NotBeforeUtc, false));
        });
    }
}
