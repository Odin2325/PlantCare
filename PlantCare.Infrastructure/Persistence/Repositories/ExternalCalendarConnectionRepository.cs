using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class ExternalCalendarConnectionRepository(
    PlantCareDbContext dbContext)
    : IExternalCalendarConnectionRepository
{
    public Task<ExternalCalendarConnection?> GetGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.ExternalCalendarConnections.SingleOrDefaultAsync(
            connection =>
                connection.UserId == userId &&
                connection.Provider == "Google",
            cancellationToken);

    public Task<ExternalCalendarConnection?> GetMicrosoftAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.ExternalCalendarConnections.SingleOrDefaultAsync(
            connection =>
                connection.UserId == userId &&
                connection.Provider == "Microsoft",
            cancellationToken);

    public async Task<IReadOnlyList<ExternalCalendarEvent>> GetEventsAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ExternalCalendarEvents
            .Where(calendarEvent => calendarEvent.ConnectionId == connectionId)
            .ToListAsync(cancellationToken);

    public void Add(ExternalCalendarConnection connection) =>
        dbContext.ExternalCalendarConnections.Add(connection);

    public void Remove(ExternalCalendarConnection connection) =>
        dbContext.ExternalCalendarConnections.Remove(connection);

    public void AddEvent(ExternalCalendarEvent calendarEvent) =>
        dbContext.ExternalCalendarEvents.Add(calendarEvent);

    public void RemoveEvent(ExternalCalendarEvent calendarEvent) =>
        dbContext.ExternalCalendarEvents.Remove(calendarEvent);
}
