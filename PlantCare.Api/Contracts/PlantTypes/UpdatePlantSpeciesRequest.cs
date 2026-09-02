using PlantCare.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Contracts.PlantTypes;

public sealed class UpdatePlantSpeciesRequest : IValidatableObject
{
    [Required]
    [MaxLength(100)]
    public string CommonName { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? ScientificName { get; init; }

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public SunlightRequirement SunlightRequirement { get; init; }

    [Required]
    [MaxLength(1000)]
    public string SunlightInstructions { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DefaultWateringIntervalDays { get; init; }

    [Required]
    [MaxLength(1000)]
    public string WateringInstructions { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? DefaultFertilizingIntervalDays { get; init; }

    [MaxLength(1000)]
    public string? FertilizingInstructions { get; init; }

    [Required]
    [MaxLength(1000)]
    public string SoilInstructions { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? HumidityInstructions { get; init; }

    public decimal? MinimumTemperatureCelsius { get; init; }

    public decimal? MaximumTemperatureCelsius { get; init; }

    public bool IsToxicToPets { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (SunlightRequirement == SunlightRequirement.Unknown)
        {
            yield return new ValidationResult(
                "Sunlight requirement must be specified.",
                [nameof(SunlightRequirement)]);
        }

        if (DefaultFertilizingIntervalDays.HasValue &&
            string.IsNullOrWhiteSpace(FertilizingInstructions))
        {
            yield return new ValidationResult(
                "Fertilizing instructions are required when a fertilizing interval is specified.",
                [nameof(FertilizingInstructions)]);
        }

        if (MinimumTemperatureCelsius.HasValue &&
            MaximumTemperatureCelsius.HasValue &&
            MinimumTemperatureCelsius > MaximumTemperatureCelsius)
        {
            yield return new ValidationResult(
                "Minimum temperature cannot be greater than maximum temperature.",
                [
                    nameof(MinimumTemperatureCelsius),
                    nameof(MaximumTemperatureCelsius)
                ]);
        }
    }
}