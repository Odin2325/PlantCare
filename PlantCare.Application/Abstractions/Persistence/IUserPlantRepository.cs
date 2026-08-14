using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface IUserPlantRepository
{
    Task<IReadOnlyList<UserPlant>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserPlant?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    void Add(UserPlant userPlant);
}