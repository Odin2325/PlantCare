using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface IPlantSpeciesRepository
{
    Task<IReadOnlyList<PlantSpecies>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PlantSpecies?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(PlantSpecies plantSpecies);

    void Remove(PlantSpecies plantSpecies);
}