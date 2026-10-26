using PlantCare.Domain.Entities;
namespace PlantCare.Application.Abstractions.Persistence;

public interface IHarvestRecordRepository
{
    Task<IReadOnlyList<HarvestRecord>> GetLatestAsync(Guid plantId, CancellationToken ct);
    Task<HarvestRecord?> GetTrackedAsync(Guid plantId, Guid id, CancellationToken ct);
    void Add(HarvestRecord record);
    void Remove(HarvestRecord record);
}