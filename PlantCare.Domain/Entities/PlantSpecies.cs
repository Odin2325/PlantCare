using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Entities;

public sealed class PlantSpecies
{
    private const int CommonNameMaxLength = 100;
    private const int ScientificNameMaxLength = 150;
    private const int DescriptionMaxLength = 2_000;
    private const int InstructionsMaxLength = 1_000;

    // Required later by Entity Framework Core when materializing entities.
    // It remains private so normal application code must use Create().
    private PlantSpecies()
    {
    }

    public Guid Id { get; private set; }

    public string CommonName { get; private set; } = string.Empty;

    public string? ScientificName { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public SunlightRequirement SunlightRequirement { get; private set; }

    public string SunlightInstructions { get; private set; } = string.Empty;

    public int DefaultWateringIntervalDays { get; private set; }

    public string WateringInstructions { get; private set; } = string.Empty;

    public int? DefaultFertilizingIntervalDays { get; private set; }

    public string? FertilizingInstructions { get; private set; }

    public string SoilInstructions { get; private set; } = string.Empty;

    public string? HumidityInstructions { get; private set; }

    public decimal? MinimumTemperatureCelsius { get; private set; }

    public decimal? MaximumTemperatureCelsius { get; private set; }

    public bool IsToxicToPets { get; private set; }

    public static PlantSpecies Create(
        string commonName,
        string? scientificName,
        string description,
        SunlightRequirement sunlightRequirement,
        string sunlightInstructions,
        int defaultWateringIntervalDays,
        string wateringInstructions,
        int? defaultFertilizingIntervalDays,
        string? fertilizingInstructions,
        string soilInstructions,
        string? humidityInstructions,
        decimal? minimumTemperatureCelsius,
        decimal? maximumTemperatureCelsius,
        bool isToxicToPets)
    {
        ValidateSunlightRequirement(sunlightRequirement);
        ValidateWateringInterval(defaultWateringIntervalDays);
        ValidateFertilizingInterval(defaultFertilizingIntervalDays);
        ValidateTemperatureRange(
            minimumTemperatureCelsius,
            maximumTemperatureCelsius);

        return new PlantSpecies
        {
            Id = Guid.NewGuid(),

            CommonName = NormalizeRequired(
                commonName,
                nameof(commonName),
                CommonNameMaxLength),

            ScientificName = NormalizeOptional(
                scientificName,
                nameof(scientificName),
                ScientificNameMaxLength),

            Description = NormalizeRequired(
                description,
                nameof(description),
                DescriptionMaxLength),

            SunlightRequirement = sunlightRequirement,

            SunlightInstructions = NormalizeRequired(
                sunlightInstructions,
                nameof(sunlightInstructions),
                InstructionsMaxLength),

            DefaultWateringIntervalDays = defaultWateringIntervalDays,

            WateringInstructions = NormalizeRequired(
                wateringInstructions,
                nameof(wateringInstructions),
                InstructionsMaxLength),

            DefaultFertilizingIntervalDays =
                defaultFertilizingIntervalDays,

            FertilizingInstructions = NormalizeOptional(
                fertilizingInstructions,
                nameof(fertilizingInstructions),
                InstructionsMaxLength),

            SoilInstructions = NormalizeRequired(
                soilInstructions,
                nameof(soilInstructions),
                InstructionsMaxLength),

            HumidityInstructions = NormalizeOptional(
                humidityInstructions,
                nameof(humidityInstructions),
                InstructionsMaxLength),

            MinimumTemperatureCelsius =
                minimumTemperatureCelsius,

            MaximumTemperatureCelsius =
                maximumTemperatureCelsius,

            IsToxicToPets = isToxicToPets
        };
    }

    private static void ValidateSunlightRequirement(
        SunlightRequirement sunlightRequirement)
    {
        var isDefined = Enum.IsDefined(
            typeof(SunlightRequirement),
            sunlightRequirement);

        if (!isDefined ||
            sunlightRequirement == SunlightRequirement.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sunlightRequirement),
                sunlightRequirement,
                "A valid sunlight requirement must be provided.");
        }
    }

    private static void ValidateWateringInterval(
        int defaultWateringIntervalDays)
    {
        if (defaultWateringIntervalDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultWateringIntervalDays),
                defaultWateringIntervalDays,
                "The watering interval must be greater than zero.");
        }
    }

    private static void ValidateFertilizingInterval(
        int? defaultFertilizingIntervalDays)
    {
        if (defaultFertilizingIntervalDays is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultFertilizingIntervalDays),
                defaultFertilizingIntervalDays,
                "The fertilizing interval must be greater than zero.");
        }
    }

    private static void ValidateTemperatureRange(
        decimal? minimumTemperatureCelsius,
        decimal? maximumTemperatureCelsius)
    {
        if (minimumTemperatureCelsius.HasValue &&
            maximumTemperatureCelsius.HasValue &&
            minimumTemperatureCelsius >
            maximumTemperatureCelsius)
        {
            throw new ArgumentException(
                "The minimum temperature cannot be greater " +
                "than the maximum temperature.");
        }
    }

    private static string NormalizeRequired(
        string value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "The value cannot be empty.",
                parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(
        string? value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}