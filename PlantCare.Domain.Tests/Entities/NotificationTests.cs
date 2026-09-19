using PlantCare.Domain.Entities;

namespace PlantCare.Domain.Tests.Entities;

public sealed class NotificationTests
{
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
