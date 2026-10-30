using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class SchedulePreviewTests
{
    [Fact]
    public void PreviewUsesTheSameDstRulesWithoutChangingTheOriginal()
    {
        var now=new DateTimeOffset(2026,3,28,12,0,0,TimeSpan.Zero);
        var schedule=CareSchedule.Create(Guid.NewGuid(),CareActionType.Watering,7,now);
        var due=schedule.NextDueAtUtc;
        var preview=schedule.PreviewChanges(7,true,CareScheduleMode.Weekdays,
            CareWeekDays.Sunday,new TimeOnly(2,30),"Europe/Berlin",now);
        Assert.Equal(due,schedule.NextDueAtUtc);
        Assert.Equal(CareScheduleMode.Interval,schedule.ScheduleMode);
        Assert.Equal(new DateTimeOffset(2026,3,29,1,0,0,TimeSpan.Zero),preview.NextDueAtUtc);
        schedule.ApplyChanges(7,true,CareScheduleMode.Weekdays,CareWeekDays.Sunday,
            new TimeOnly(2,30),"Europe/Berlin",now);
        Assert.Equal(preview.NextDueAtUtc,schedule.NextDueAtUtc);
    }
}
