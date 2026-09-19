using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();
        builder.Property(notification => notification.UserId).IsRequired();
        builder.Property(notification => notification.CareScheduleId).IsRequired();
        builder.Property(notification => notification.DueAtUtc).IsRequired();
        builder.Property(notification => notification.CreatedAtUtc).IsRequired();

        builder.HasOne(notification => notification.CareSchedule)
            .WithMany()
            .HasForeignKey(notification => notification.CareScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(notification => new
        {
            notification.CareScheduleId,
            notification.DueAtUtc
        }).IsUnique();

        builder.HasIndex(notification => new
        {
            notification.UserId,
            notification.ReadAtUtc,
            notification.CreatedAtUtc
        });
    }
}
