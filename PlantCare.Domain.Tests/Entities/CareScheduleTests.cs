using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class CareScheduleTests
{
    [Fact]
    public void Create_CreatesEnabledScheduleWithInitialDueDate()
    {
        var startsAt = new DateTimeOffset(
            2026,
            8,
            1,
            10,
            0,
            0,
            TimeSpan.Zero);

        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            startsAt);

        Assert.NotEqual(Guid.Empty, schedule.Id);
        Assert.Equal(CareActionType.Watering, schedule.ActionType);
        Assert.Equal(7, schedule.IntervalDays);
        Assert.True(schedule.IsEnabled);
        Assert.Null(schedule.LastCompletedAtUtc);
        Assert.Equal(startsAt.AddDays(7), schedule.NextDueAtUtc);
    }

    [Fact]
    public void MarkCompleted_CalculatesNextDueDate()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            DateTimeOffset.UnixEpoch);

        var completedAt =
            new DateTimeOffset(
                2026,
                8,
                10,
                10,
                0,
                0,
                TimeSpan.Zero);

        schedule.MarkCompleted(completedAt);

        Assert.Equal(
            completedAt,
            schedule.LastCompletedAtUtc);

        Assert.Equal(
            completedAt.AddDays(7),
            schedule.NextDueAtUtc);
    }

    [Fact]
    public void Create_WithLastCompletion_UsesItForSchedule()
    {
        var startsAt = new DateTimeOffset(
            2026, 8, 10, 10, 0, 0, TimeSpan.Zero);
        var lastCompletedAt = startsAt.AddDays(-3);

        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            startsAt,
            lastCompletedAt);

        Assert.Equal(
            lastCompletedAt,
            schedule.LastCompletedAtUtc);
        Assert.Equal(
            lastCompletedAt.AddDays(7),
            schedule.NextDueAtUtc);
    }

    [Fact]
    public void UpdateInterval_RecalculatesDueDateFromScheduleStart()
    {
        var startsAt = new DateTimeOffset(
            2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            startsAt);

        schedule.UpdateInterval(10);

        Assert.Equal(10, schedule.IntervalDays);
        Assert.Equal(startsAt.AddDays(10), schedule.NextDueAtUtc);
    }

    [Fact]
    public void UpdateInterval_AfterCompletion_RecalculatesFromLastCompletion()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            DateTimeOffset.UnixEpoch);
        var completedAt = new DateTimeOffset(
            2026, 8, 10, 10, 0, 0, TimeSpan.Zero);
        schedule.MarkCompleted(completedAt);

        schedule.UpdateInterval(14);

        Assert.Equal(completedAt.AddDays(14), schedule.NextDueAtUtc);
    }

    [Fact]
    public void MarkCompleted_UsesActualCompletionForNextDueDate()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            DateTimeOffset.UnixEpoch);

        var firstCompletion =
            new DateTimeOffset(
                2026,
                8,
                1,
                10,
                0,
                0,
                TimeSpan.Zero);

        schedule.MarkCompleted(firstCompletion);

        var lateCompletion =
            new DateTimeOffset(
                2026,
                8,
                10,
                10,
                0,
                0,
                TimeSpan.Zero);

        schedule.MarkCompleted(lateCompletion);

        Assert.Equal(
            new DateTimeOffset(
                2026,
                8,
                17,
                10,
                0,
                0,
                TimeSpan.Zero),
            schedule.NextDueAtUtc);
    }

    [Fact]
    public void WeekdaySchedule_UsesSelectedLocalDaysAndTime()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            DateTimeOffset.UnixEpoch);
        var mondayMorningUtc = new DateTimeOffset(
            2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

        schedule.ConfigureWeekdays(
            CareWeekDays.Monday | CareWeekDays.Wednesday,
            new TimeOnly(9, 0),
            "Europe/Berlin",
            mondayMorningUtc);

        Assert.Equal(CareScheduleMode.Weekdays, schedule.ScheduleMode);
        Assert.Equal(
            new DateTimeOffset(2026, 9, 23, 7, 0, 0, TimeSpan.Zero),
            schedule.NextDueAtUtc);
    }

    [Fact]
    public void WeekdaySchedule_KeepsLocalTimeAcrossDaylightSavingChange()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7,
            DateTimeOffset.UnixEpoch);
        schedule.ConfigureWeekdays(
            CareWeekDays.Monday,
            new TimeOnly(9, 0),
            "Europe/Berlin",
            new DateTimeOffset(2026, 10, 19, 7, 0, 0, TimeSpan.Zero));

        schedule.MarkCompleted(
            new DateTimeOffset(2026, 10, 19, 7, 0, 0, TimeSpan.Zero));

        Assert.Equal(
            new DateTimeOffset(2026, 10, 26, 8, 0, 0, TimeSpan.Zero),
            schedule.NextDueAtUtc);
    }
}
