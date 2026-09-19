using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class CalendarRepository(PlantCareDbContext dbContext) : ICalendarRepository
{
    public async Task<IReadOnlyList<CareSchedule>> GetSchedulesAsync(Guid userId, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        await dbContext.CareSchedules.AsNoTracking().Include(schedule => schedule.UserPlant)
            .Where(schedule => schedule.UserPlant.UserId == userId && schedule.UserPlant.IsActive &&
                schedule.IsEnabled && !schedule.IsArchived && schedule.NextDueAtUtc != null && schedule.NextDueAtUtc < toUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CareEvent>> GetCompletedEventsAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        await dbContext.CareEvents.AsNoTracking()
            .Include(careEvent => careEvent.CareSchedule).ThenInclude(schedule => schedule.UserPlant)
            .Where(careEvent => careEvent.CareSchedule.UserPlant.UserId == userId &&
                careEvent.CompletedAtUtc >= fromUtc && careEvent.CompletedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
}
