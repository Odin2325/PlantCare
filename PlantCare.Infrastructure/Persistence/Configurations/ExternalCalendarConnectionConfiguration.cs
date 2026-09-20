using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class ExternalCalendarConnectionConfiguration
    : IEntityTypeConfiguration<ExternalCalendarConnection>
{
    public void Configure(
        EntityTypeBuilder<ExternalCalendarConnection> builder)
    {
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Provider)
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(connection => connection.AccountEmail)
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(connection => connection.ProtectedAccessToken)
            .HasMaxLength(4000)
            .IsRequired();
        builder.Property(connection => connection.ProtectedRefreshToken)
            .HasMaxLength(4000)
            .IsRequired();
        builder.HasIndex(connection => new
            {
                connection.UserId,
                connection.Provider
            })
            .IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(connection => connection.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
