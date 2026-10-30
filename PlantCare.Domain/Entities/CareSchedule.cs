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

        // Completing care early still satisfies this local care date.
        NextDueAtUtc = ScheduleMode == CareScheduleMode.Weekdays
            ? CalculateNextWeekdayOccurrence(completedAtUtc, includeAnchorDate: false)
            : GetNextOccurrenceAfter(completedAtUtc);
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
        var anchor = latestCompletedAtUtc ?? resetAtUtc;
        NextDueAtUtc = ScheduleMode == CareScheduleMode.Weekdays
            ? CalculateNextWeekdayOccurrence(anchor, includeAnchorDate: latestCompletedAtUtc is null)
            : GetNextOccurrenceAfter(anchor);
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
        NextDueAtUtc = CalculateNextWeekdayOccurrence(
            anchorUtc, includeAnchorDate: LastCompletedAtUtc != anchorUtc);
    }

    public DateTimeOffset GetNextOccurrenceAfter(DateTimeOffset occurrenceUtc)
    {
        return ScheduleMode == CareScheduleMode.Weekdays
            ? CalculateNextWeekdayOccurrence(occurrenceUtc)
            : occurrenceUtc.AddDays(IntervalDays);
    }

    private DateTimeOffset CalculateNextWeekdayOccurrence(
        DateTimeOffset afterUtc,
        bool includeAnchorDate = true)
    {
        if (PreferredTimeLocal is null || string.IsNullOrWhiteSpace(TimeZoneId))
            throw new InvalidOperationException(
                "The weekday schedule is incomplete.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var localAfter = TimeZoneInfo.ConvertTime(afterUtc, timeZone);
        var localDate = DateOnly.FromDateTime(localAfter.DateTime);

        // A second week also accommodates a completely skipped local date.
        for (var offset = includeAnchorDate ? 0 : 1; offset <= 14; offset++)
        {
            var candidateDate = localDate.AddDays(offset);
            if (!WeekDays.HasFlag(ToCareWeekDay(candidateDate.DayOfWeek)))
                continue;

            var localDateTime = candidateDate.ToDateTime(
                PreferredTimeLocal.Value,
                DateTimeKind.Unspecified);
            if (timeZone.IsInvalidTime(localDateTime))
            {
                localDateTime = localDateTime.AddTicks(
                    -(localDateTime.Ticks % TimeSpan.TicksPerMinute));
                do { localDateTime = localDateTime.AddMinutes(1); }
                while (DateOnly.FromDateTime(localDateTime) == candidateDate &&
                    timeZone.IsInvalidTime(localDateTime));
                if (DateOnly.FromDateTime(localDateTime) != candidateDate)
                    continue;
            }

            // Choose one instant in a repeated hour: the smaller offset gives
            // the later UTC occurrence, including zones with half-hour changes.
            var occurrence = timeZone.IsAmbiguousTime(localDateTime)
                ? new DateTimeOffset(localDateTime,
                    timeZone.GetAmbiguousTimeOffsets(localDateTime).Min()).ToUniversalTime()
                : new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone));
            // Keep the resolved occurrence on its selected local date.
            if (DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(occurrence, timeZone).DateTime) != candidateDate)
                continue;
            if (occurrence > afterUtc)
                return occurrence;
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

    // The detached copy uses the same recurrence rules without changing tracked state.
    public CareSchedule PreviewChanges(int intervalDays, bool isEnabled,
        CareScheduleMode mode, CareWeekDays days, TimeOnly? time, string? zone,
        DateTimeOffset now)
    {
        var copy = (CareSchedule)MemberwiseClone();
        copy.ApplyChanges(intervalDays, isEnabled, mode, days, time, zone, now);
        return copy;
    }

    public void ApplyChanges(int intervalDays, bool isEnabled,
        CareScheduleMode mode, CareWeekDays days, TimeOnly? time, string? zone,
        DateTimeOffset now)
    {
        if (mode == CareScheduleMode.Weekdays)
            ConfigureWeekdays(days, time ?? throw new ArgumentException("A preferred time is required."),
                zone ?? string.Empty, LastCompletedAtUtc ?? now);
        else if (mode != CareScheduleMode.Interval)
            throw new ArgumentOutOfRangeException(nameof(mode));
        else if (ScheduleMode == CareScheduleMode.Weekdays)
            ConfigureInterval(intervalDays, LastCompletedAtUtc ?? now);
        else
            UpdateInterval(intervalDays);
        if (isEnabled) Enable(); else Disable();
    }

    public void Enable()
    {
        IsEnabled = true;
    }
}
