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
}