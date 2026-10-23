using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class CalendarInvitationConfiguration : IEntityTypeConfiguration<CalendarInvitation>
{
    public void Configure(EntityTypeBuilder<CalendarInvitation> b)
    {
        b.ToTable("CalendarInvitations"); b.HasKey(i => i.Id); b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.RecipientEmail).HasMaxLength(256).IsRequired();
        b.Property(i => i.NormalizedRecipientEmail).HasMaxLength(256).IsRequired();
        b.Property(i => i.TokenHash).HasMaxLength(64).IsRequired();
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(i => i.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(i => i.TokenHash).IsUnique();
        b.HasIndex(i => new { i.OwnerUserId, i.NormalizedRecipientEmail }).IsUnique()
            .HasFilter("[RevokedAtUtc] IS NULL AND [AcceptedAtUtc] IS NULL");
        b.HasIndex(i => i.NormalizedRecipientEmail);
    }
}
