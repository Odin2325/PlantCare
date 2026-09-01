using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface ICareEventRepository
{
    void Add(CareEvent careEvent);

    Task<IReadOnlyList<CareEvent>> GetForUserPlantAsync(
        Guid userId,
        Guid userPlantId,
        int take,
        CancellationToken cancellationToken = default);
}