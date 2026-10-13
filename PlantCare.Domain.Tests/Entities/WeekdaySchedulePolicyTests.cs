using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class WeekdaySchedulePolicyTests
{
    [Theory]
    [InlineData("Europe/Berlin", "2026-09-21T06:00:00Z", CareWeekDays.Monday, "09:00", "2026-09-21T07:00:00Z")]
    [InlineData("Europe/Berlin", "2026-09-21T07:00:00Z", CareWeekDays.Monday, "09:00", "2026-09-28T07:00:00Z")]
    [InlineData("Europe/Berlin", "2026-09-21T08:00:00Z", CareWeekDays.Monday, "09:00", "2026-09-28T07:00:00Z")]
    [InlineData("Europe/Berlin", "2026-09-21T06:00:00Z", CareWeekDays.Wednesday, "09:00", "2026-09-23T07:00:00Z")]
    // The weekday belongs to the schedule's zone, not the UTC date/server zone.
    [InlineData("Europe/Berlin", "2026-09-20T22:00:00Z", CareWeekDays.Monday, "00:30", "2026-09-20T22:30:00Z")]
    [InlineData("America/New_York", "2026-09-22T01:00:00Z", CareWeekDays.Monday, "22:00", "2026-09-22T02:00:00Z")]
    [InlineData("UTC", "2026-12-31T23:00:00Z", CareWeekDays.Friday, "00:00", "2027-01-01T00:00:00Z")]
    [InlineData("Europe/Berlin", "2026-01-31T12:00:00Z", CareWeekDays.Sunday, "09:00", "2026-02-01T08:00:00Z")]
    // Nonexistent clock times move to the first valid minute on that date.
    [InlineData("Europe/Berlin", "2026-03-28T12:00:00Z", CareWeekDays.Sunday, "02:15", "2026-03-29T01:00:00Z")]
    [InlineData("America/New_York", "2026-03-07T12:00:00Z", CareWeekDays.Sunday, "02:15", "2026-03-08T07:00:00Z")]
    [InlineData("Australia/Lord_Howe", "2026-10-03T12:00:00Z", CareWeekDays.Sunday, "02:15", "2026-10-03T15:30:00Z")]
    // A repeated clock time has exactly one occurrence, at the later UTC instant.
    [InlineData("Europe/Berlin", "2026-10-24T12:00:00Z", CareWeekDays.Sunday, "02:30", "2026-10-25T01:30:00Z")]
    [InlineData("Europe/Berlin", "2026-10-25T00:45:00Z", CareWeekDays.Sunday, "02:30", "2026-10-25T01:30:00Z")]
    [InlineData("Europe/Berlin", "2026-10-25T01:30:00Z", CareWeekDays.Sunday, "02:30", "2026-11-01T01:30:00Z")]
    [InlineData("America/New_York", "2026-10-31T12:00:00Z", CareWeekDays.Sunday, "01:30", "2026-11-01T06:30:00Z")]
    [InlineData("Australia/Lord_Howe", "2026-04-04T12:00:00Z", CareWeekDays.Sunday, "01:45", "2026-04-04T15:15:00Z")]
    public void ConfigurationUsesExplicitLocalTimePolicy(
        string zone, string anchor, CareWeekDays days, string time, string expected)
    {
        var schedule = NewSchedule();
        schedule.ConfigureWeekdays(days, TimeOnly.Parse(time), zone, DateTimeOffset.Parse(anchor));
        Assert.Equal(DateTimeOffset.Parse(expected), schedule.NextDueAtUtc);
        Assert.True(schedule.NextDueAtUtc > DateTimeOffset.Parse(anchor));
    }

    [Fact]
    public void EarlyCompletionAndHistoryRecalculationSkipTheCompletedLocalDate()
    {
        var schedule = NewSchedule();
        var earlyMonday = DateTimeOffset.Parse("2026-09-21T06:00:00Z");
        var nextWednesday = DateTimeOffset.Parse("2026-09-23T07:00:00Z");
        schedule.ConfigureWeekdays(CareWeekDays.Monday | CareWeekDays.Wednesday,
            new TimeOnly(9, 0), "Europe/Berlin", earlyMonday);
        schedule.MarkCompleted(earlyMonday);
        Assert.Equal(nextWednesday, schedule.NextDueAtUtc);
        // Editing a schedule after early completion must not reintroduce today.
        schedule.ConfigureWeekdays(CareWeekDays.Monday | CareWeekDays.Wednesday,
            new TimeOnly(9, 0), "Europe/Berlin", earlyMonday);
        Assert.Equal(nextWednesday, schedule.NextDueAtUtc);
        schedule.RecalculateAfterHistoryChange(earlyMonday, earlyMonday.AddHours(1));
        Assert.Equal(nextWednesday, schedule.NextDueAtUtc);
        schedule.RecalculateAfterHistoryChange(null, earlyMonday);
        Assert.Equal(earlyMonday.AddHours(1), schedule.NextDueAtUtc);
    }

    [Fact]
    public void CalendarRecurrenceIsStrictlyIncreasingAndDoesNotRepeatTheAutumnHour()
    {
        var schedule = NewSchedule();
        schedule.ConfigureWeekdays(CareWeekDays.Sunday, new TimeOnly(2, 30),
            "Europe/Berlin", DateTimeOffset.Parse("2026-10-24T12:00:00Z"));
        var first = schedule.NextDueAtUtc!.Value;
        var next = schedule.GetNextOccurrenceAfter(first);
        Assert.Equal(DateTimeOffset.Parse("2026-10-25T01:30:00Z"), first);
        Assert.Equal(DateTimeOffset.Parse("2026-11-01T01:30:00Z"), next);
        schedule.MarkCompleted(DateTimeOffset.Parse("2026-10-25T00:15:00Z"));
        Assert.Equal(next, schedule.NextDueAtUtc);
    }

    [Fact]
    public void ExplicitTimeZoneChangeRecalculatesAndIntervalModeClearsWeekdaySettings()
    {
        var schedule = NewSchedule();
        var anchor = DateTimeOffset.Parse("2026-09-21T06:00:00Z");
        schedule.ConfigureWeekdays(CareWeekDays.Monday, new TimeOnly(9, 0), "Europe/Berlin", anchor);
        schedule.ConfigureWeekdays(CareWeekDays.Monday, new TimeOnly(9, 0), "America/New_York", anchor);
        Assert.Equal(DateTimeOffset.Parse("2026-09-21T13:00:00Z"), schedule.NextDueAtUtc);
        Assert.Equal("America/New_York", schedule.TimeZoneId);
        schedule.ConfigureInterval(3, anchor);
        Assert.Equal(anchor.AddDays(3), schedule.NextDueAtUtc);
        Assert.Null(schedule.TimeZoneId);
        Assert.Null(schedule.PreferredTimeLocal);
        Assert.Equal(CareWeekDays.None, schedule.WeekDays);
    }

    private static CareSchedule NewSchedule() =>
        CareSchedule.Create(Guid.NewGuid(), CareActionType.Watering, 7, DateTimeOffset.UnixEpoch);
}
