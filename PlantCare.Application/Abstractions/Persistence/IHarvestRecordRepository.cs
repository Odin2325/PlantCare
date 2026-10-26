using PlantCare.Application.MyPlants;
using PlantCare.Domain.Entities;
namespace PlantCare.Application.Abstractions.Persistence;

public interface IHarvestRecordRepository
{
    Task<HarvestOverviewDto> GetOverviewAsync(Guid user, DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<HarvestRecord>> GetLatestAsync(Guid plantId, CancellationToken ct);
    Task<HarvestRecord?> GetTrackedAsync(Guid plantId, Guid id, CancellationToken ct);
    void Add(HarvestRecord record);
    void Remove(HarvestRecord record);
}