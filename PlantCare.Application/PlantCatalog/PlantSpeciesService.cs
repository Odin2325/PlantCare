using PlantCare.Application.Abstractions.Persistence;
using PlantSpeciesEntity = PlantCare.Domain.Entities.PlantSpecies;

namespace PlantCare.Application.PlantCatalog;

internal sealed class PlantSpeciesService(
    IPlantSpeciesRepository plantSpeciesRepository,
    IUnitOfWork unitOfWork)
    : IPlantSpeciesService
{
    public async Task<IReadOnlyList<PlantSpeciesDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var plantSpecies =
            await plantSpeciesRepository.GetAllAsync(
                cancellationToken);

        return plantSpecies
            .Select(MapToDto)
            .ToList();
    }

    public async Task<PlantSpeciesDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var plantSpecies =
            await plantSpeciesRepository.GetByIdAsync(
                id,
                cancellationToken);

        return plantSpecies is null
            ? null
            : MapToDto(plantSpecies);
    }

    public async Task<PlantSpeciesDto> CreateAsync(
        CreatePlantSpeciesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plantSpecies =
            PlantSpeciesEntity.Create(
                commonName: command.CommonName,
                scientificName: command.ScientificName,
                description: command.Description,
                sunlightRequirement:
                    command.SunlightRequirement,
                sunlightInstructions:
                    command.SunlightInstructions,
                defaultWateringIntervalDays:
                    command.DefaultWateringIntervalDays,
                wateringInstructions:
                    command.WateringInstructions,
                defaultFertilizingIntervalDays:
                    command.DefaultFertilizingIntervalDays,
                fertilizingInstructions:
                    command.FertilizingInstructions,
                soilInstructions:
                    command.SoilInstructions,
                humidityInstructions:
                    command.HumidityInstructions,
                minimumTemperatureCelsius:
                    command.MinimumTemperatureCelsius,
                maximumTemperatureCelsius:
                    command.MaximumTemperatureCelsius,
                isToxicToPets:
                    command.IsToxicToPets);

        plantSpeciesRepository.Add(plantSpecies);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return MapToDto(plantSpecies);
    }

    public async Task<PlantSpeciesDto?> UpdateAsync(Guid id, UpdatePlantSpeciesCommand command, CancellationToken cancellationToken = default)
    {
        var plantSpecies = await plantSpeciesRepository.GetByIdAsync(id, cancellationToken);

        if (plantSpecies is null)
            return null;

        plantSpecies.Update(
            command.CommonName,
            command.ScientificName,
            command.Description,
            command.SunlightRequirement,
            command.SunlightInstructions,
            command.DefaultWateringIntervalDays,
            command.WateringInstructions,
            command.DefaultFertilizingIntervalDays,
            command.FertilizingInstructions,
            command.SoilInstructions,
            command.HumidityInstructions,
            command.MinimumTemperatureCelsius,
            command.MaximumTemperatureCelsius,
            command.IsToxicToPets);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(plantSpecies);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plantSpecies = await plantSpeciesRepository.GetByIdAsync(id, cancellationToken);

        if (plantSpecies is null)
            return false;

        plantSpeciesRepository.Remove(plantSpecies);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static PlantSpeciesDto MapToDto(
        PlantSpeciesEntity plantSpecies)
    {
        return new PlantSpeciesDto(
            Id: plantSpecies.Id,
            CommonName: plantSpecies.CommonName,
            ScientificName: plantSpecies.ScientificName,
            Description: plantSpecies.Description,
            SunlightRequirement:
                plantSpecies.SunlightRequirement,
            SunlightInstructions:
                plantSpecies.SunlightInstructions,
            DefaultWateringIntervalDays:
                plantSpecies.DefaultWateringIntervalDays,
            WateringInstructions:
                plantSpecies.WateringInstructions,
            DefaultFertilizingIntervalDays:
                plantSpecies.DefaultFertilizingIntervalDays,
            FertilizingInstructions:
                plantSpecies.FertilizingInstructions,
            SoilInstructions:
                plantSpecies.SoilInstructions,
            HumidityInstructions:
                plantSpecies.HumidityInstructions,
            MinimumTemperatureCelsius:
                plantSpecies.MinimumTemperatureCelsius,
            MaximumTemperatureCelsius:
                plantSpecies.MaximumTemperatureCelsius,
            IsToxicToPets:
                plantSpecies.IsToxicToPets);
    }
}