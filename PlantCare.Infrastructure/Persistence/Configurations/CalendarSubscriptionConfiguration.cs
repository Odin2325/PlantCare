using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class CalendarSubscriptionConfiguration : IEntityTypeConfiguration<CalendarSubscription>
{
    public void Configure(EntityTypeBuilder<CalendarSubscription> builder)
    {
        builder.ToTable("CalendarSubscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedNever();
        builder.Property(subscription => subscription.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(subscription => subscription.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(subscription => subscription.TokenHash).IsUnique();
        builder.HasIndex(subscription => subscription.UserId).IsUnique().HasFilter("[RevokedAtUtc] IS NULL");
    }
}
