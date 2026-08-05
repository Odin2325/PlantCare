using PlantCare.Application.Abstractions.Persistence;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class UnitOfWork(
    PlantCareDbContext dbContext)
    : IUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}