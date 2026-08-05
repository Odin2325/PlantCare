using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class PlantSpeciesRepository(
    PlantCareDbContext dbContext)
    : IPlantSpeciesRepository
{
    public async Task<IReadOnlyList<PlantSpecies>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlantSpecies
            .AsNoTracking()
            .OrderBy(plantSpecies => plantSpecies.CommonName)
            .ThenBy(plantSpecies => plantSpecies.ScientificName)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlantSpecies?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlantSpecies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                plantSpecies => plantSpecies.Id == id,
                cancellationToken);
    }

    public void Add(PlantSpecies plantSpecies)
    {
        ArgumentNullException.ThrowIfNull(plantSpecies);

        dbContext.PlantSpecies.Add(plantSpecies);
    }
}