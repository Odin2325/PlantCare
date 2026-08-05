using PlantCare.Domain.Enums;

namespace PlantCare.Application.PlantCatalog;

public sealed record PlantSpeciesDto(
    Guid Id,
    string CommonName,
    string? ScientificName,
    string Description,
    SunlightRequirement SunlightRequirement,
    string SunlightInstructions,
    int DefaultWateringIntervalDays,
    string WateringInstructions,
    int? DefaultFertilizingIntervalDays,
    string? FertilizingInstructions,
    string SoilInstructions,
    string? HumidityInstructions,
    decimal? MinimumTemperatureCelsius,
    decimal? MaximumTemperatureCelsius,
    bool IsToxicToPets);