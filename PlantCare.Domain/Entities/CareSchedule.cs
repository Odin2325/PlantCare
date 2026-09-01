using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Entities;

public sealed class CareSchedule
{
    private CareSchedule()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserPlantId { get; private set; }

    public CareActionType ActionType { get; private set; }

    public int IntervalDays { get; private set; }

    public DateTimeOffset? LastCompletedAtUtc { get; private set; }

    public DateTimeOffset? NextDueAtUtc { get; private set; }

    public bool IsEnabled { get; private set; }

    public UserPlant UserPlant { get; private set; } = null!;

    public static CareSchedule Create(Guid userPlantId, CareActionType actionType, int intervalDays)
    {
        if (userPlantId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user plant ID must be provided.",
                nameof(userPlantId));
        }

        if (actionType == CareActionType.Unknown ||
            !Enum.IsDefined(typeof(CareActionType), actionType))
        {
            throw new ArgumentException(
                "A valid care action type must be provided.",
                nameof(actionType));
        }

        if (intervalDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalDays),
                "The interval must be greater than zero.");
        }

        return new CareSchedule
        {
            Id = Guid.NewGuid(),
            UserPlantId = userPlantId,
            ActionType = actionType,
            IntervalDays = intervalDays,
            IsEnabled = true
        };
    }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException(
                "A disabled care schedule cannot be completed.");
        }

        if (LastCompletedAtUtc.HasValue &&
            completedAtUtc < LastCompletedAtUtc.Value)
        {
            throw new ArgumentException(
                "The completion time cannot be earlier than the previous completion.",
                nameof(completedAtUtc));
        }

        LastCompletedAtUtc = completedAtUtc;

        NextDueAtUtc =
            completedAtUtc.AddDays(IntervalDays);
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void Enable()
    {
        IsEnabled = true;
    }
}