using PlantCare.Domain.Enums;

namespace PlantCare.Application.PlantCatalog;

public sealed class UpdatePlantSpeciesCommand
{
    public string CommonName { get; init; } = string.Empty;
    public string? ScientificName { get; init; }
    public string Description { get; init; } = string.Empty;

    public SunlightRequirement SunlightRequirement { get; init; }
    public string SunlightInstructions { get; init; } = string.Empty;

    public int DefaultWateringIntervalDays { get; init; }
    public string WateringInstructions { get; init; } = string.Empty;

    public int? DefaultFertilizingIntervalDays { get; init; }
    public string? FertilizingInstructions { get; init; }

    public string SoilInstructions { get; init; } = string.Empty;
    public string? HumidityInstructions { get; init; }

    public decimal? MinimumTemperatureCelsius { get; init; }
    public decimal? MaximumTemperatureCelsius { get; init; }

    public bool IsToxicToPets { get; init; }
}