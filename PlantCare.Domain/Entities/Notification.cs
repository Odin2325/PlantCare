namespace PlantCare.Domain.Entities;

public sealed class Notification
{
    private Notification()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CareScheduleId { get; private set; }
    public DateTimeOffset DueAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public DateTimeOffset? PushSentAtUtc { get; private set; }
    public DateTimeOffset? PushNextAttemptAtUtc { get; private set; }
    public int ReminderSequence { get; private set; }
    public DateTimeOffset? NotBeforeUtc { get; private set; }
    public DateTimeOffset? SupersededAtUtc { get; private set; }
    public CareSchedule CareSchedule { get; private set; } = null!;

    public static Notification Create(
        Guid userId,
        Guid careScheduleId,
        DateTimeOffset dueAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));
        if (careScheduleId == Guid.Empty)
            throw new ArgumentException("A valid care schedule ID is required.", nameof(careScheduleId));

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CareScheduleId = careScheduleId,
            DueAtUtc = dueAtUtc,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void MarkRead(DateTimeOffset readAtUtc)
    {
        ReadAtUtc ??= readAtUtc;
    }

    public static Notification CreateFollowUp(Notification previous, DateTimeOffset now, int hours)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (hours is not (1 or 24 or 72)) throw new ArgumentOutOfRangeException(nameof(hours));
        var next = Create(previous.UserId, previous.CareScheduleId, previous.DueAtUtc, now);
        next.ReminderSequence = checked(previous.ReminderSequence + 1);
        next.NotBeforeUtc = now.AddHours(hours);
        return next;
    }

    public void MarkPushSent(DateTimeOffset sentAtUtc)
    {
        PushSentAtUtc ??= sentAtUtc;
        PushNextAttemptAtUtc = null;
    }

    public void DeferPush(DateTimeOffset nextAttemptAtUtc) => PushNextAttemptAtUtc = nextAttemptAtUtc;
}
