using PlantCare.Domain.Enums;

namespace PlantCare.Application.Care;

public sealed record CareScheduleDto(
    Guid Id,
    CareActionType ActionType,
    int IntervalDays,
    DateTimeOffset? LastCompletedAtUtc,
    DateTimeOffset? NextDueAtUtc,
    bool IsEnabled);