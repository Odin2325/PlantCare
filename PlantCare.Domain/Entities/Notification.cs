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
}
