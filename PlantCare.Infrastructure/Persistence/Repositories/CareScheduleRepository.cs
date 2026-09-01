using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class CareScheduleRepository(
    PlantCareDbContext dbContext)
    : ICareScheduleRepository
{
    public Task<CareSchedule?> GetForUserAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CareSchedules
            .FirstOrDefaultAsync(
                schedule =>
                    schedule.UserPlantId == userPlantId &&
                    schedule.UserPlant.UserId == userId &&
                    schedule.ActionType == actionType,
                cancellationToken);
    }
}