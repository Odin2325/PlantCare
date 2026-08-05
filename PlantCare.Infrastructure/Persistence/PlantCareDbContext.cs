using Microsoft.EntityFrameworkCore;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence;

public sealed class PlantCareDbContext(DbContextOptions<PlantCareDbContext> options) : DbContext(options)
{
    public DbSet<PlantSpecies> PlantSpecies => Set<PlantSpecies>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlantCareDbContext).Assembly);
    }
}