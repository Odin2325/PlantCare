using PlantCare.Domain.Enums;

namespace PlantCare.Application.Care;

public sealed record CareEventHistoryDto(
    Guid Id,
    Guid CareScheduleId,
    CareActionType ActionType,
    DateTimeOffset CompletedAtUtc,
    DateTimeOffset RecordedAtUtc,
    string? Notes);