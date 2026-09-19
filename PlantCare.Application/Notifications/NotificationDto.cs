using PlantCare.Domain.Enums;

namespace PlantCare.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    Guid UserPlantId,
    string PlantName,
    CareActionType ActionType,
    DateTimeOffset DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);
