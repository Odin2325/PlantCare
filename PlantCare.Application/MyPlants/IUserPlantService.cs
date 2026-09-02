namespace PlantCare.Application.MyPlants;

public interface IUserPlantService
{
    Task<IReadOnlyList<UserPlantDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserPlantDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<UserPlantDto?> AddAsync(Guid userId, AddUserPlantCommand command, CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}