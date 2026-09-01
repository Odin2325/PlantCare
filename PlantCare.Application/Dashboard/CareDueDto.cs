using PlantCare.Domain.Enums;

namespace PlantCare.Application.Dashboard;

public sealed record CareDueDto(
    Guid UserPlantId,
    string PlantName,
    string SpeciesCommonName,
    CareActionType ActionType,
    DateTimeOffset DueAtUtc,
    CareDueStatus Status);