using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class CareScheduleTests
{
    [Fact]
    public void Create_CreatesEnabledScheduleWithoutDueDate()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7);

        Assert.NotEqual(Guid.Empty, schedule.Id);
        Assert.Equal(CareActionType.Watering, schedule.ActionType);
        Assert.Equal(7, schedule.IntervalDays);
        Assert.True(schedule.IsEnabled);
        Assert.Null(schedule.LastCompletedAtUtc);
        Assert.Null(schedule.NextDueAtUtc);
    }

    [Fact]
    public void MarkCompleted_CalculatesNextDueDate()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7);

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
    public void MarkCompleted_UsesActualCompletionForNextDueDate()
    {
        var schedule = CareSchedule.Create(
            Guid.NewGuid(),
            CareActionType.Watering,
            7);

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
}