using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class CalendarShareConfiguration : IEntityTypeConfiguration<CalendarShare>
{
    public void Configure(EntityTypeBuilder<CalendarShare> builder)
    {
        builder.ToTable("CalendarShares");
        builder.HasKey(share => share.Id);
        builder.Property(share => share.Id).ValueGeneratedNever();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(share => share.OwnerUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(share => share.RecipientUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(share => new { share.OwnerUserId, share.RecipientUserId }).IsUnique().HasFilter("[RevokedAtUtc] IS NULL");
        builder.HasIndex(share => new { share.RecipientUserId, share.RevokedAtUtc });
    }
}
