using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class CareEventRepository(
    PlantCareDbContext dbContext)
    : ICareEventRepository
{
    public void Add(CareEvent careEvent)
    {
        ArgumentNullException.ThrowIfNull(careEvent);

        dbContext.CareEvents.Add(careEvent);
    }

    public async Task<IReadOnlyList<CareEvent>>
        GetForUserPlantAsync(
            Guid userId,
            Guid userPlantId,
            int take,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.CareEvents
            .AsNoTracking()
            .Include(
                careEvent => careEvent.CareSchedule)
            .Where(
                careEvent =>
                    careEvent.CareSchedule.UserPlantId ==
                        userPlantId &&
                    careEvent.CareSchedule.UserPlant.UserId ==
                        userId)
            .OrderByDescending(
                careEvent => careEvent.CompletedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<CareEvent?> GetTrackedForUserAsync(
        Guid userId,
        Guid userPlantId,
        Guid careEventId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CareEvents
            .Include(careEvent => careEvent.CareSchedule)
            .FirstOrDefaultAsync(
                careEvent =>
                    careEvent.Id == careEventId &&
                    careEvent.CareSchedule.UserPlantId == userPlantId &&
                    careEvent.CareSchedule.UserPlant.UserId == userId &&
                    careEvent.CareSchedule.UserPlant.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CareEvent>>
        GetTrackedForScheduleAsync(
            Guid careScheduleId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.CareEvents
            .Where(careEvent =>
                careEvent.CareScheduleId == careScheduleId)
            .ToListAsync(cancellationToken);
    }

    public void Remove(CareEvent careEvent)
    {
        ArgumentNullException.ThrowIfNull(careEvent);
        dbContext.CareEvents.Remove(careEvent);
    }
}
