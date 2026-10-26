using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class HarvestRecordRepository(PlantCareDbContext db) : IHarvestRecordRepository
{
    public async Task<IReadOnlyList<HarvestRecord>> GetLatestAsync(Guid plantId, CancellationToken ct) => await db.HarvestRecords.AsNoTracking().Where(r => r.UserPlantId == plantId).OrderByDescending(r => r.HarvestedOn).ThenByDescending(r => r.RecordedAtUtc).ThenBy(r => r.Id).Take(100).ToListAsync(ct);
    public Task<HarvestRecord?> GetTrackedAsync(Guid plantId, Guid id, CancellationToken ct) => db.HarvestRecords.FirstOrDefaultAsync(r => r.UserPlantId == plantId && r.Id == id, ct);
    public void Add(HarvestRecord record) => db.HarvestRecords.Add(record);
    public void Remove(HarvestRecord record) => db.HarvestRecords.Remove(record);
}