using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Entities;

public sealed class NotificationPreference
{
    private static readonly int[] AllowedLeadTimeHours = [0, 24, 48, 72, 168];

    private NotificationPreference() { }

    public Guid UserId { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool WateringEnabled { get; private set; }
    public bool FertilizingEnabled { get; private set; }
    public bool MistingEnabled { get; private set; }
    public bool PruningEnabled { get; private set; }
    public bool RepottingEnabled { get; private set; }
    public int ReminderLeadTimeHours { get; private set; }

    public static NotificationPreference CreateDefault(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        return new NotificationPreference
        {
            UserId = userId,
            InAppEnabled = true,
            PushEnabled = true,
            WateringEnabled = true,
            FertilizingEnabled = true,
            MistingEnabled = true,
            PruningEnabled = true,
            RepottingEnabled = true,
            ReminderLeadTimeHours = 0
        };
    }

    public void Update(
        bool inAppEnabled,
        bool pushEnabled,
        bool wateringEnabled,
        bool fertilizingEnabled,
        bool mistingEnabled,
        bool pruningEnabled,
        bool repottingEnabled,
        int reminderLeadTimeHours)
    {
        if (!AllowedLeadTimeHours.Contains(reminderLeadTimeHours))
            throw new ArgumentOutOfRangeException(
                nameof(reminderLeadTimeHours),
                "Reminder lead time must be 0, 24, 48, 72, or 168 hours.");

        InAppEnabled = inAppEnabled;
        PushEnabled = pushEnabled;
        WateringEnabled = wateringEnabled;
        FertilizingEnabled = fertilizingEnabled;
        MistingEnabled = mistingEnabled;
        PruningEnabled = pruningEnabled;
        RepottingEnabled = repottingEnabled;
        ReminderLeadTimeHours = reminderLeadTimeHours;
    }

    public bool IsActionEnabled(CareActionType actionType) => actionType switch
    {
        CareActionType.Watering => WateringEnabled,
        CareActionType.Fertilizing => FertilizingEnabled,
        CareActionType.Misting => MistingEnabled,
        CareActionType.Pruning => PruningEnabled,
        CareActionType.Repotting => RepottingEnabled,
        _ => false
    };
}
