using System.ComponentModel.DataAnnotations;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Api.Contracts.PlantTypes;

public sealed class CreatePlantSpeciesRequest
    : IValidatableObject
{
    [Required]
    [MaxLength(PlantSpecies.CommonNameMaxLength)]
    public string CommonName { get; init; } = string.Empty;

    [MaxLength(PlantSpecies.ScientificNameMaxLength)]
    public string? ScientificName { get; init; }

    [Required]
    [MaxLength(PlantSpecies.DescriptionMaxLength)]
    public string Description { get; init; } = string.Empty;

    public SunlightRequirement SunlightRequirement { get; init; }

    [Required]
    [MaxLength(PlantSpecies.InstructionsMaxLength)]
    public string SunlightInstructions { get; init; } = string.Empty;

    [Range(1, 3_650)]
    public int DefaultWateringIntervalDays { get; init; }

    [Required]
    [MaxLength(PlantSpecies.InstructionsMaxLength)]
    public string WateringInstructions { get; init; } = string.Empty;

    [Range(1, 3_650)]
    public int? DefaultFertilizingIntervalDays { get; init; }

    [MaxLength(PlantSpecies.InstructionsMaxLength)]
    public string? FertilizingInstructions { get; init; }

    [Required]
    [MaxLength(PlantSpecies.InstructionsMaxLength)]
    public string SoilInstructions { get; init; } = string.Empty;

    [MaxLength(PlantSpecies.InstructionsMaxLength)]
    public string? HumidityInstructions { get; init; }

    [Range(-50, 100)]
    public decimal? MinimumTemperatureCelsius { get; init; }

    [Range(-50, 100)]
    public decimal? MaximumTemperatureCelsius { get; init; }

    public bool IsToxicToPets { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(SunlightRequirement) ||
            SunlightRequirement ==
            SunlightRequirement.Unknown)
        {
            yield return new ValidationResult(
                "A valid sunlight requirement must be provided.",
                [nameof(SunlightRequirement)]);
        }

        if (MinimumTemperatureCelsius.HasValue &&
            MaximumTemperatureCelsius.HasValue &&
            MinimumTemperatureCelsius >
            MaximumTemperatureCelsius)
        {
            yield return new ValidationResult(
                "The minimum temperature cannot be greater " +
                "than the maximum temperature.",
                [
                    nameof(MinimumTemperatureCelsius),
                    nameof(MaximumTemperatureCelsius)
                ]);
        }

        if (DefaultFertilizingIntervalDays.HasValue &&
            string.IsNullOrWhiteSpace(FertilizingInstructions))
        {
            yield return new ValidationResult(
                "Fertilizing instructions are required when " +
                "a fertilizing interval is provided.",
                [nameof(FertilizingInstructions)]);
        }
    }
}