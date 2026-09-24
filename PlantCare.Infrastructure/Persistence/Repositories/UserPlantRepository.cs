using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class UserPlantRepository(PlantCareDbContext dbContext) : IUserPlantRepository
{
    public async Task<IReadOnlyList<UserPlant>>
        GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPlants
            .AsNoTracking()
            .Include(userPlant => userPlant.PlantSpecies)
            .Include(userPlant => userPlant.CareSchedules)
            .Where(userPlant =>
                    userPlant.UserId == userId &&
                    userPlant.IsActive)
            .OrderBy(userPlant => userPlant.Nickname)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserPlant>>
        GetArchivedForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPlants
            .AsNoTracking()
            .Include(userPlant => userPlant.PlantSpecies)
            .Include(userPlant => userPlant.CareSchedules)
            .Where(userPlant =>
                userPlant.UserId == userId &&
                !userPlant.IsActive)
            .OrderBy(userPlant => userPlant.Nickname)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserPlant?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPlants
            .AsNoTracking()
            .Include(userPlant => userPlant.PlantSpecies)
            .Include(userPlant => userPlant.CareSchedules)
            .FirstOrDefaultAsync(userPlant =>
                    userPlant.Id == id &&
                    userPlant.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserPlant>> GetForDashboardAsync(Guid userId, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        // 
        return await dbContext.UserPlants
            .AsNoTracking()
            .Include(
                userPlant => userPlant.PlantSpecies)
            .Include(
                userPlant => userPlant.CareSchedules)
            .Where(
                userPlant =>
                    userPlant.UserId == userId &&
                    userPlant.IsActive)
            .Where(
                userPlant =>
                    userPlant.CareSchedules.Any(
                        schedule =>
                            !schedule.IsArchived &&
                            schedule.IsEnabled &&
                            schedule.NextDueAtUtc != null &&
                            schedule.NextDueAtUtc < end))
            .ToListAsync(cancellationToken);
    }

    // UserPlantRepository.cs
    public async Task<UserPlant?> GetTrackedByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPlants
            .Include(userPlant => userPlant.PlantSpecies)
            .Include(userPlant => userPlant.CareSchedules)
            .FirstOrDefaultAsync(
                userPlant => userPlant.Id == id && userPlant.UserId == userId,
                cancellationToken);
    }

    public void Add(UserPlant userPlant)
    {
        ArgumentNullException.ThrowIfNull(userPlant);

        dbContext.UserPlants.Add(userPlant);
    }
}
