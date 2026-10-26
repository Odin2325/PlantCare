using PlantCare.Application.MyPlants;
using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class HarvestRecordRepository(PlantCareDbContext db) : IHarvestRecordRepository
{
    public async Task<IReadOnlyList<HarvestRecord>> GetLatestAsync(Guid plantId, CancellationToken ct) => await db.HarvestRecords.AsNoTracking().Where(r => r.UserPlantId == plantId).OrderByDescending(r => r.HarvestedOn).ThenByDescending(r => r.RecordedAtUtc).ThenBy(r => r.Id).Take(100).ToListAsync(ct);
    public Task<HarvestRecord?> GetTrackedAsync(Guid plantId, Guid id, CancellationToken ct) => db.HarvestRecords.FirstOrDefaultAsync(r => r.UserPlantId == plantId && r.Id == id, ct);
    public async Task<HarvestOverviewDto> GetOverviewAsync(Guid user, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var plants = await db.UserPlants.AsNoTracking().Where(p => p.UserId == user && p.IsActive)
            .Select(p => new { p.Id, p.PlantSpecies.CatalogProfile }).ToListAsync(ct);
        // Catalog profiles are JSON value objects; classify after reading owned plant metadata.
        var gardenIds = plants.Where(p => p.CatalogProfile?.Categories?.Any(c => c != "houseplant") == true).Select(p => p.Id).ToArray();
        var rows = new List<HarvestOverviewRow>();
        var totals = new Dictionary<Guid, (int Count, int Unquantified, Dictionary<string, decimal> Quantities)>();
        if (gardenIds.Length > 0)
        {
            var query = db.HarvestRecords.AsNoTracking().Where(h => h.UserPlant.UserId == user && h.UserPlant.IsActive &&
                gardenIds.Contains(h.UserPlantId) && h.HarvestedOn >= from && h.HarvestedOn <= to)
                .OrderByDescending(h => h.HarvestedOn).ThenByDescending(h => h.RecordedAtUtc).ThenBy(h => h.Id);
            // Stream all matching records for complete totals; retain only the latest 100 display rows.
            await foreach (var h in query.AsAsyncEnumerable().WithCancellation(ct))
            {
                var sum = totals.GetValueOrDefault(h.UserPlantId, (Count: 0, Unquantified: 0, Quantities: new Dictionary<string, decimal>()));
                sum.Count++;
                if (h.Quantity is { } quantity && h.Unit is { } unit) sum.Quantities[unit] = sum.Quantities.GetValueOrDefault(unit) + quantity;
                else sum.Unquantified++;
                totals[h.UserPlantId] = sum;
                if (rows.Count < 100) rows.Add(new(h.Id, h.UserPlantId, h.HarvestedOn, h.Quantity, h.Unit, h.Notes));
            }
        }
        var summaries = totals.OrderBy(p => p.Key).Select(p => new PlantHarvestSummary(
            p.Key, p.Value.Count, p.Value.Unquantified, p.Value.Quantities.ToDictionary(q => q.Key,
                q => q.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)))).ToList();
        return new(from, to, rows, summaries);
    }
    public void Add(HarvestRecord record) => db.HarvestRecords.Add(record);
    public void Remove(HarvestRecord record) => db.HarvestRecords.Remove(record);
}
