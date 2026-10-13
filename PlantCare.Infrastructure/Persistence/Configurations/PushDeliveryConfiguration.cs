using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class PushDeliveryConfiguration : IEntityTypeConfiguration<PushDelivery>
{
    public void Configure(EntityTypeBuilder<PushDelivery> builder)
    {
        builder.ToTable("PushDeliveries");
        builder.HasKey(item => new { item.NotificationId, item.SubscriptionId });
        builder.HasOne<Notification>().WithMany().HasForeignKey(item => item.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
        // No subscription FK: retain terminal outcomes after an expired device
        // is removed, without retaining endpoint/key material.
    }
}
