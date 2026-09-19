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
    public DbSet<CareSchedule> CareSchedules => Set<CareSchedule>();

    public DbSet<CareEvent> CareEvents => Set<CareEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CalendarSubscription> CalendarSubscriptions => Set<CalendarSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlantCareDbContext).Assembly);
    }
}
