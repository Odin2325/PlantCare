using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class UserPlantTagConfiguration : IEntityTypeConfiguration<UserPlantTag>
{
    public void Configure(EntityTypeBuilder<UserPlantTag> builder)
    {
        builder.ToTable("UserPlantTags");
        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedNever();
        builder.Property(tag => tag.Name)
            .HasMaxLength(UserPlantTag.NameMaxLength)
            .IsRequired();

        builder.HasOne<UserPlant>()
            .WithMany(plant => plant.Tags)
            .HasForeignKey(tag => tag.UserPlantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tag => new { tag.UserPlantId, tag.Name })
            .IsUnique();
    }
}
