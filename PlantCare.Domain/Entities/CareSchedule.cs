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

    public CareScheduleMode ScheduleMode { get; private set; }

    public CareWeekDays WeekDays { get; private set; }

    public TimeOnly? PreferredTimeLocal { get; private set; }

    public string? TimeZoneId { get; private set; }

    public DateTimeOffset? LastCompletedAtUtc { get; private set; }

    public DateTimeOffset? NextDueAtUtc { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool IsArchived { get; private set; }

    public UserPlant UserPlant { get; private set; } = null!;

    public static CareSchedule Create(
        Guid userPlantId,
        CareActionType actionType,
        int intervalDays,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? lastCompletedAtUtc = null)
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

        if (lastCompletedAtUtc > startsAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastCompletedAtUtc),
                "The last completion cannot be later than the schedule start.");
        }

        var scheduleStart =
            lastCompletedAtUtc ?? startsAtUtc;

        return new CareSchedule
        {
            Id = Guid.NewGuid(),
            UserPlantId = userPlantId,
            ActionType = actionType,
            IntervalDays = intervalDays,
            ScheduleMode = CareScheduleMode.Interval,
            LastCompletedAtUtc = lastCompletedAtUtc,
            NextDueAtUtc = scheduleStart.AddDays(intervalDays),
            IsEnabled = true,
            IsArchived = false
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

        NextDueAtUtc = GetNextOccurrenceAfter(completedAtUtc);
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void Archive()
    {
        IsArchived = true;
        IsEnabled = false;
    }

    public void Restore(int intervalDays)
    {
        IsArchived = false;
        UpdateInterval(intervalDays);
        IsEnabled = true;
    }

    public void RecalculateAfterHistoryChange(
        DateTimeOffset? latestCompletedAtUtc,
        DateTimeOffset resetAtUtc)
    {
        LastCompletedAtUtc = latestCompletedAtUtc;
        NextDueAtUtc = GetNextOccurrenceAfter(
            latestCompletedAtUtc ?? resetAtUtc);
    }

    public void UpdateInterval(int intervalDays)
    {
        if (intervalDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalDays),
                "The interval must be greater than zero.");
        }

        var scheduleStart = LastCompletedAtUtc ??
            NextDueAtUtc?.AddDays(-IntervalDays);

        IntervalDays = intervalDays;
        ScheduleMode = CareScheduleMode.Interval;
        WeekDays = CareWeekDays.None;
        PreferredTimeLocal = null;
        TimeZoneId = null;

        if (scheduleStart.HasValue)
        {
            NextDueAtUtc =
                scheduleStart.Value.AddDays(intervalDays);
        }
    }

    public void ConfigureInterval(
        int intervalDays,
        DateTimeOffset anchorUtc)
    {
        if (intervalDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(intervalDays));

        IntervalDays = intervalDays;
        ScheduleMode = CareScheduleMode.Interval;
        WeekDays = CareWeekDays.None;
        PreferredTimeLocal = null;
        TimeZoneId = null;
        NextDueAtUtc = anchorUtc.AddDays(intervalDays);
    }

    public void ConfigureWeekdays(
        CareWeekDays weekDays,
        TimeOnly preferredTimeLocal,
        string timeZoneId,
        DateTimeOffset anchorUtc)
    {
        const CareWeekDays allDays =
            CareWeekDays.Monday |
            CareWeekDays.Tuesday |
            CareWeekDays.Wednesday |
            CareWeekDays.Thursday |
            CareWeekDays.Friday |
            CareWeekDays.Saturday |
            CareWeekDays.Sunday;

        if (weekDays == CareWeekDays.None || (weekDays & ~allDays) != 0)
            throw new ArgumentOutOfRangeException(
                nameof(weekDays),
                "At least one valid weekday must be selected.");
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException(
                "A time zone is required for weekday schedules.",
                nameof(timeZoneId));

        _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        ScheduleMode = CareScheduleMode.Weekdays;
        WeekDays = weekDays;
        PreferredTimeLocal = preferredTimeLocal;
        TimeZoneId = timeZoneId;
        NextDueAtUtc = CalculateNextWeekdayOccurrence(anchorUtc);
    }

    public DateTimeOffset GetNextOccurrenceAfter(DateTimeOffset occurrenceUtc)
    {
        return ScheduleMode == CareScheduleMode.Weekdays
            ? CalculateNextWeekdayOccurrence(occurrenceUtc)
            : occurrenceUtc.AddDays(IntervalDays);
    }

    private DateTimeOffset CalculateNextWeekdayOccurrence(
        DateTimeOffset afterUtc)
    {
        if (PreferredTimeLocal is null || string.IsNullOrWhiteSpace(TimeZoneId))
            throw new InvalidOperationException(
                "The weekday schedule is incomplete.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var localAfter = TimeZoneInfo.ConvertTime(afterUtc, timeZone);
        var localDate = DateOnly.FromDateTime(localAfter.DateTime);

        for (var offset = 1; offset <= 7; offset++)
        {
            var candidateDate = localDate.AddDays(offset);
            if (!WeekDays.HasFlag(ToCareWeekDay(candidateDate.DayOfWeek)))
                continue;

            var localDateTime = candidateDate.ToDateTime(
                PreferredTimeLocal.Value,
                DateTimeKind.Unspecified);
            while (timeZone.IsInvalidTime(localDateTime))
                localDateTime = localDateTime.AddMinutes(30);

            return TimeZoneInfo.ConvertTimeToUtc(
                localDateTime,
                timeZone);
        }

        throw new InvalidOperationException(
            "The weekday schedule has no next occurrence.");
    }

    private static CareWeekDays ToCareWeekDay(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => CareWeekDays.Monday,
            DayOfWeek.Tuesday => CareWeekDays.Tuesday,
            DayOfWeek.Wednesday => CareWeekDays.Wednesday,
            DayOfWeek.Thursday => CareWeekDays.Thursday,
            DayOfWeek.Friday => CareWeekDays.Friday,
            DayOfWeek.Saturday => CareWeekDays.Saturday,
            DayOfWeek.Sunday => CareWeekDays.Sunday,
            _ => CareWeekDays.None
        };

    public void Enable()
    {
        IsEnabled = true;
    }
}
