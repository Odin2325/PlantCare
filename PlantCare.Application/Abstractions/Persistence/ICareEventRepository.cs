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

    Task<CareEvent?> GetTrackedForUserAsync(
        Guid userId,
        Guid userPlantId,
        Guid careEventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CareEvent>> GetTrackedForScheduleAsync(
        Guid careScheduleId,
        CancellationToken cancellationToken = default);

    void Remove(CareEvent careEvent);
}
