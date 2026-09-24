namespace PlantCare.Application.Notifications;

public sealed record NotificationPreferenceDto(
    bool InAppEnabled,
    bool PushEnabled,
    bool WateringEnabled,
    bool FertilizingEnabled,
    bool MistingEnabled,
    bool PruningEnabled,
    bool RepottingEnabled,
    int ReminderLeadTimeHours);

public sealed record UpdateNotificationPreferenceCommand(
    bool InAppEnabled,
    bool PushEnabled,
    bool WateringEnabled,
    bool FertilizingEnabled,
    bool MistingEnabled,
    bool PruningEnabled,
    bool RepottingEnabled,
    int ReminderLeadTimeHours);
