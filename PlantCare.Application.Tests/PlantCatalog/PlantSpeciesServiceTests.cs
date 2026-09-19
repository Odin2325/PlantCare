using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Application.PlantCatalog;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Application.Tests.PlantCatalog;

public sealed class PlantSpeciesServiceTests
{
    [Fact]
    public async Task UpdateAsync_LoadsTrackedEntityAndSavesChanges()
    {
        var plantSpecies = CreatePlantSpecies();
        var repository = new PlantSpeciesRepositoryFake(plantSpecies);
        var unitOfWork = new UnitOfWorkFake();
        var service = new PlantSpeciesService(repository, unitOfWork);

        var result = await service.UpdateAsync(
            plantSpecies.Id,
            CreateUpdateCommand());

        Assert.NotNull(result);
        Assert.Equal("Updated Monstera", result.CommonName);
        Assert.Equal(1, repository.TrackedLoadCount);
        Assert.Equal(0, repository.ReadOnlyLoadCount);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    private static PlantSpecies CreatePlantSpecies()
    {
        return PlantSpecies.Create(
            commonName: "Monstera",
            scientificName: "Monstera deliciosa",
            description: "A tropical plant.",
            sunlightRequirement: SunlightRequirement.BrightIndirectLight,
            sunlightInstructions: "Keep near a bright window.",
            defaultWateringIntervalDays: 7,
            wateringInstructions: "Water when the top soil is dry.",
            defaultFertilizingIntervalDays: 30,
            fertilizingInstructions: "Fertilize during the growing season.",
            soilInstructions: "Use well-draining soil.",
            humidityInstructions: "Prefers moderate humidity.",
            minimumTemperatureCelsius: 18,
            maximumTemperatureCelsius: 30,
            isToxicToPets: true);
    }

    private static UpdatePlantSpeciesCommand CreateUpdateCommand()
    {
        return new UpdatePlantSpeciesCommand
        {
            CommonName = "Updated Monstera",
            ScientificName = "Monstera deliciosa",
            Description = "An updated tropical plant.",
            SunlightRequirement = SunlightRequirement.BrightIndirectLight,
            SunlightInstructions = "Keep near a bright window.",
            DefaultWateringIntervalDays = 8,
            WateringInstructions = "Water when the top soil is dry.",
            DefaultFertilizingIntervalDays = 30,
            FertilizingInstructions = "Fertilize during the growing season.",
            SoilInstructions = "Use well-draining soil.",
            HumidityInstructions = "Prefers moderate humidity.",
            MinimumTemperatureCelsius = 18,
            MaximumTemperatureCelsius = 30,
            IsToxicToPets = true
        };
    }

    private sealed class PlantSpeciesRepositoryFake(
        PlantSpecies plantSpecies)
        : IPlantSpeciesRepository
    {
        public int ReadOnlyLoadCount { get; private set; }

        public int TrackedLoadCount { get; private set; }

        public Task<IReadOnlyList<PlantSpecies>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PlantSpecies> result = [plantSpecies];
            return Task.FromResult(result);
        }

        public Task<PlantSpecies?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            ReadOnlyLoadCount++;
            return Task.FromResult<PlantSpecies?>(plantSpecies);
        }

        public Task<PlantSpecies?> GetTrackedByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            TrackedLoadCount++;
            return Task.FromResult<PlantSpecies?>(plantSpecies);
        }

        public void Add(PlantSpecies entity)
        {
        }

        public void Remove(PlantSpecies entity)
        {
        }
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }
}
