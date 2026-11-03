using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence;

public sealed class PlantCareDbContext(DbContextOptions<PlantCareDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<AccountErasure> AccountErasures => Set<AccountErasure>();
    public DbSet<PlantSpecies> PlantSpecies => Set<PlantSpecies>();
    public DbSet<ShortlistedPlant> ShortlistedPlants => Set<ShortlistedPlant>();
    public DbSet<UserPlant> UserPlants => Set<UserPlant>();
    public DbSet<UserPlantTag> UserPlantTags => Set<UserPlantTag>();
    public DbSet<CareSchedule> CareSchedules => Set<CareSchedule>();

    public DbSet<HarvestRecord> HarvestRecords => Set<HarvestRecord>();
    public DbSet<PlantPhotoEntry> PlantPhotoEntries => Set<PlantPhotoEntry>();
    public DbSet<PlantPhotoMedia> PlantPhotoMedia => Set<PlantPhotoMedia>();
    public DbSet<CareEvent> CareEvents => Set<CareEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<CalendarSubscription> CalendarSubscriptions => Set<CalendarSubscription>();
    public DbSet<CalendarShare> CalendarShares => Set<CalendarShare>();
    public DbSet<CalendarInvitation> CalendarInvitations => Set<CalendarInvitation>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<PushDelivery> PushDeliveries => Set<PushDelivery>();
    public DbSet<ExternalCalendarConnection> ExternalCalendarConnections => Set<ExternalCalendarConnection>();
    public DbSet<ExternalCalendarEvent> ExternalCalendarEvents => Set<ExternalCalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlantCareDbContext).Assembly);
        modelBuilder.SharedTypeEntity<Dictionary<string, object>>("PhotoJournalQuota", b => {
            b.ToTable("PhotoJournalQuota"); b.IndexerProperty<int>("Id").ValueGeneratedNever();
            b.IndexerProperty<long>("Revision"); b.HasKey("Id");
            b.HasData(new Dictionary<string, object> { ["Id"] = 1, ["Revision"] = 0L });
        });
        // Defense in depth while reconciling a restored database: tombstoned
        // identities and their private/feed/integration records stay invisible.
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => !AccountErasures.Any(e => e.UserId == u.Id));
        modelBuilder.Entity<CalendarInvitation>().HasQueryFilter(i => !AccountErasures.Any(e => e.UserId == i.OwnerUserId));
        modelBuilder.Entity<UserPlant>().HasQueryFilter(p => !AccountErasures.Any(e => e.UserId == p.UserId));
        modelBuilder.Entity<ShortlistedPlant>().HasQueryFilter(p => !AccountErasures.Any(e => e.UserId == p.UserId));
        modelBuilder.Entity<CareSchedule>().HasQueryFilter(s => !AccountErasures.Any(e => e.UserId == s.UserPlant.UserId));
        modelBuilder.Entity<HarvestRecord>().HasQueryFilter(h => !AccountErasures.Any(e => e.UserId == h.UserPlant.UserId));
        modelBuilder.Entity<PlantPhotoEntry>().HasQueryFilter(p => !AccountErasures.Any(e => e.UserId == p.UserPlant.UserId));
        modelBuilder.Entity<PlantPhotoMedia>().HasQueryFilter(p => !AccountErasures.Any(e => e.UserId == p.Entry.UserPlant.UserId));
        modelBuilder.Entity<CareEvent>().HasQueryFilter(c => !AccountErasures.Any(e => e.UserId == c.CareSchedule.UserPlant.UserId));
        modelBuilder.Entity<Notification>().HasQueryFilter(n => !AccountErasures.Any(e => e.UserId == n.UserId));
        modelBuilder.Entity<CalendarSubscription>().HasQueryFilter(s => !AccountErasures.Any(e => e.UserId == s.UserId));
        modelBuilder.Entity<CalendarShare>().HasQueryFilter(s => !AccountErasures.Any(e => e.UserId == s.OwnerUserId || e.UserId == s.RecipientUserId));
        modelBuilder.Entity<ExternalCalendarConnection>().HasQueryFilter(c => !AccountErasures.Any(e => e.UserId == c.UserId));
    }
}
