using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class UserPlantConfiguration : IEntityTypeConfiguration<UserPlant>
{
    public void Configure(EntityTypeBuilder<UserPlant> builder)
    {
        builder.ToTable("UserPlants");

        builder.HasKey(userPlant => userPlant.Id);

        builder.Property(userPlant => userPlant.Id)
            .ValueGeneratedNever();

        builder.Property(userPlant => userPlant.UserId)
            .IsRequired();

        builder.Property(userPlant => userPlant.PlantSpeciesId)
            .IsRequired();

        builder.Property(userPlant => userPlant.Nickname)
            .HasMaxLength(UserPlant.NicknameMaxLength)
            .IsRequired();

        builder.Property(userPlant => userPlant.Location)
            .HasMaxLength(UserPlant.LocationMaxLength);

        builder.Property(userPlant => userPlant.AcquiredOn)
            .HasColumnType("date");

        builder.Property(userPlant => userPlant.Notes)
            .HasMaxLength(UserPlant.NotesMaxLength);

        builder.Property(userPlant => userPlant.IsActive)
            .IsRequired();

        builder.Property(userPlant => userPlant.CreatedAtUtc)
            .IsRequired();

        builder
            .HasOne(userPlant => userPlant.PlantSpecies)
            .WithMany()
            .HasForeignKey(
                userPlant => userPlant.PlantSpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(userPlant => userPlant.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(userPlant => userPlant.UserId);

        builder.HasIndex(
            userPlant => new
            {
                userPlant.UserId,
                userPlant.IsActive
            });
    }
}