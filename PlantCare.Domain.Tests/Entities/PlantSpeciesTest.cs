using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class PlantSpeciesTests
{
    [Fact]
    public void Create_WithValidValues_CreatesPlantSpecies()
    {
        // Act
        var plantSpecies = CreateValidPlantSpecies();

        // Assert
        Assert.NotEqual(Guid.Empty, plantSpecies.Id);
        Assert.Equal("Monstera", plantSpecies.CommonName);
        Assert.Equal(
            "Monstera deliciosa",
            plantSpecies.ScientificName);

        Assert.Equal(
            SunlightRequirement.BrightIndirectLight,
            plantSpecies.SunlightRequirement);

        Assert.Equal(
            7,
            plantSpecies.DefaultWateringIntervalDays);

        Assert.Equal(
            30,
            plantSpecies.DefaultFertilizingIntervalDays);

        Assert.True(plantSpecies.IsToxicToPets);
    }

    [Fact]
    public void Create_WithZeroWateringInterval_ThrowsException()
    {
        // Act
        var action = () => CreateValidPlantSpecies(
            wateringIntervalDays: 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Create_WithInvalidTemperatureRange_ThrowsException()
    {
        // Act
        var action = () => CreateValidPlantSpecies(
            minimumTemperatureCelsius: 30,
            maximumTemperatureCelsius: 18);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Create_WithWhitespaceAroundNames_TrimsValues()
    {
        // Act
        var plantSpecies = PlantSpecies.Create(
            commonName: "  Monstera  ",
            scientificName: "  Monstera deliciosa  ",
            description: "A tropical climbing plant.",
            sunlightRequirement:
                SunlightRequirement.BrightIndirectLight,
            sunlightInstructions:
                "Place near a bright window.",
            defaultWateringIntervalDays: 7,
            wateringInstructions:
                "Water when the upper soil is dry.",
            defaultFertilizingIntervalDays: 30,
            fertilizingInstructions:
                "Fertilize during the growing season.",
            soilInstructions:
                "Use well-draining soil.",
            humidityInstructions:
                "Prefers moderate to high humidity.",
            minimumTemperatureCelsius: 18,
            maximumTemperatureCelsius: 30,
            isToxicToPets: true);

        // Assert
        Assert.Equal("Monstera", plantSpecies.CommonName);
        Assert.Equal(
            "Monstera deliciosa",
            plantSpecies.ScientificName);
    }

    private static PlantSpecies CreateValidPlantSpecies(
        int wateringIntervalDays = 7,
        decimal? minimumTemperatureCelsius = 18,
        decimal? maximumTemperatureCelsius = 30)
    {
        return PlantSpecies.Create(
            commonName: "Monstera",
            scientificName: "Monstera deliciosa",
            description: "A tropical climbing plant.",
            sunlightRequirement:
                SunlightRequirement.BrightIndirectLight,
            sunlightInstructions:
                "Place it near a bright window without strong direct sunlight.",
            defaultWateringIntervalDays:
                wateringIntervalDays,
            wateringInstructions:
                "Water when the upper layer of soil feels dry.",
            defaultFertilizingIntervalDays: 30,
            fertilizingInstructions:
                "Fertilize monthly during the growing season.",
            soilInstructions:
                "Use a loose, well-draining potting mixture.",
            humidityInstructions:
                "Prefers moderate to high humidity.",
            minimumTemperatureCelsius:
                minimumTemperatureCelsius,
            maximumTemperatureCelsius:
                maximumTemperatureCelsius,
            isToxicToPets: true);
    }
}