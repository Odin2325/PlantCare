namespace PlantCare.Application.PlantCatalog;

public interface IPlantSpeciesService
{
    Task<IReadOnlyList<PlantSpeciesDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PlantSpeciesDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PlantSpeciesDto> CreateAsync(CreatePlantSpeciesCommand command, CancellationToken cancellationToken = default);
}