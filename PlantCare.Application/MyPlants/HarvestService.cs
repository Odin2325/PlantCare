using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
namespace PlantCare.Application.MyPlants;

public sealed record RecordHarvestCommand(DateOnly HarvestedOn, decimal? Quantity, string? Unit, string? Notes);
public sealed record HarvestRecordDto(Guid Id, DateOnly HarvestedOn, decimal? Quantity, string? Unit, string? Notes, DateTimeOffset RecordedAtUtc);
public interface IHarvestService
{
    Task<HarvestOverviewDto> GetOverviewAsync(Guid user, DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<HarvestRecordDto>?> GetAsync(Guid user, Guid plant, CancellationToken ct);
    Task<HarvestRecordDto?> AddAsync(Guid user, Guid plant, RecordHarvestCommand command, CancellationToken ct);
    Task<bool> DeleteAsync(Guid user, Guid plant, Guid record, CancellationToken ct);
}
internal sealed class HarvestService(IUserPlantRepository plants, IHarvestRecordRepository records, IUnitOfWork unitOfWork, TimeProvider clock) : IHarvestService
{
    public Task<HarvestOverviewDto> GetOverviewAsync(Guid user, DateOnly from, DateOnly to, CancellationToken ct) => records.GetOverviewAsync(user, from, to, ct);
    private static HarvestRecordDto Map(HarvestRecord r) => new(r.Id, r.HarvestedOn, r.Quantity, r.Unit, r.Notes, r.RecordedAtUtc);
    public async Task<IReadOnlyList<HarvestRecordDto>?> GetAsync(Guid user, Guid plant, CancellationToken ct)
    {
        if (await plants.GetByIdForUserAsync(plant, user, ct) is null)
            return null;
        return (await records.GetLatestAsync(plant, ct)).Select(Map).ToList();
    }
    public async Task<HarvestRecordDto?> AddAsync(Guid user, Guid plant, RecordHarvestCommand command, CancellationToken ct)
    {
        var owned = await plants.GetByIdForUserAsync(plant, user, ct);
        if (owned is null || !owned.IsActive)
            return null;
        var record = HarvestRecord.Create(plant, command.HarvestedOn, command.Quantity, command.Unit, command.Notes, clock.GetUtcNow());
        records.Add(record);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(record);
    }
    public async Task<bool> DeleteAsync(Guid user, Guid plant, Guid record, CancellationToken ct)
    {
        var owned = await plants.GetByIdForUserAsync(plant, user, ct);
        if (owned is null || !owned.IsActive)
            return false;
        var item = await records.GetTrackedAsync(plant, record, ct);
        if (item is null)
            return false;
        records.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}