using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class NotificationPreferenceTests
{
    [Fact]
    public void CreateDefault_PreservesExistingNotificationBehavior()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());

        Assert.True(preference.InAppEnabled);
        Assert.True(preference.PushEnabled);
        Assert.True(preference.IsActionEnabled(CareActionType.Watering));
        Assert.True(preference.IsActionEnabled(CareActionType.Repotting));
        Assert.Equal(0, preference.ReminderLeadTimeHours);
    }

    [Fact]
    public void Update_RejectsUnsupportedLeadTime()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => preference.Update(
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            12));
    }
}
