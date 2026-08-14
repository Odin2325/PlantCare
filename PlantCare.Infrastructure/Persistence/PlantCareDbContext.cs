using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence;

public sealed class PlantCareDbContext(DbContextOptions<PlantCareDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<PlantSpecies> PlantSpecies => Set<PlantSpecies>();
    public DbSet<UserPlant> UserPlants => Set<UserPlant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlantCareDbContext).Assembly);
    }
}