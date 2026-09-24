using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class NotificationPreferenceConfiguration
    : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(preference => preference.UserId);
        builder.Property(preference => preference.UserId).ValueGeneratedNever();
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<NotificationPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
