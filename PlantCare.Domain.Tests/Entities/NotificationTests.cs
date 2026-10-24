using PlantCare.Domain.Entities;

namespace PlantCare.Domain.Tests.Entities;

public sealed class NotificationTests
{
    [Fact]
    public void FollowUpKeepsTheCareOccurrenceAndUsesElapsedDelayAcrossClockChanges()
    {
        var now = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
        var original = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), now.AddDays(-1), now);
        original.MarkPushSent(now);
        var next = Notification.CreateFollowUp(original, now, 72);
        Assert.NotEqual(original.Id, next.Id);
        Assert.Equal(original.DueAtUtc, next.DueAtUtc);
        Assert.Equal(original.CareScheduleId, next.CareScheduleId);
        Assert.Equal(original.UserId, next.UserId);
        Assert.Equal(now.AddHours(72), next.NotBeforeUtc);
        Assert.Equal(1, next.ReminderSequence);
        Assert.Null(next.PushSentAtUtc);
        Assert.Equal(now, original.PushSentAtUtc);
        foreach (var hours in new[] { 0, 2, 25, -1, 73 })
            Assert.Throws<ArgumentOutOfRangeException>(() => Notification.CreateFollowUp(original, now, hours));
    }
    [Fact]
    public void Create_StoresOccurrenceAndStartsUnread()
    {
        var userId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var dueAt = DateTimeOffset.UtcNow.AddHours(-1);
        var createdAt = DateTimeOffset.UtcNow;

        var notification = Notification.Create(userId, scheduleId, dueAt, createdAt);

        Assert.Equal(userId, notification.UserId);
        Assert.Equal(scheduleId, notification.CareScheduleId);
        Assert.Equal(dueAt, notification.DueAtUtc);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public void MarkRead_IsIdempotent()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var firstRead = DateTimeOffset.UtcNow;

        notification.MarkRead(firstRead);
        notification.MarkRead(firstRead.AddMinutes(5));

        Assert.Equal(firstRead, notification.ReadAtUtc);
    }
}
