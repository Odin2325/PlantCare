using PlantCare.Domain.Enums;

namespace PlantCare.Application.Calendar;

public sealed record CalendarEntryDto(
    string Id,
    Guid UserPlantId,
    string PlantName,
    CareActionType ActionType,
    DateTimeOffset StartsAtUtc,
    CalendarEntryKind Kind);

public enum CalendarEntryKind
{
    Scheduled,
    Completed
}
