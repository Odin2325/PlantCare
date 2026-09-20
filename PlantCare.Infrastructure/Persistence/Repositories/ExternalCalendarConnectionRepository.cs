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

    public void Add(ExternalCalendarConnection connection) =>
        dbContext.ExternalCalendarConnections.Add(connection);

    public void Remove(ExternalCalendarConnection connection) =>
        dbContext.ExternalCalendarConnections.Remove(connection);
}
