namespace PlantCare.Application.MyPlants;

public sealed record UserPlantDto(
    Guid Id,
    Guid PlantSpeciesId,
    string SpeciesCommonName,
    string? SpeciesScientificName,
    string Nickname,
    string? Location,
    DateOnly? AcquiredOn,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    int DefaultWateringIntervalDays,
    int? DefaultFertilizingIntervalDays);