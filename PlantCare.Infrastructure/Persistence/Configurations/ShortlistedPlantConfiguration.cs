using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class ShortlistedPlantConfiguration : IEntityTypeConfiguration<ShortlistedPlant>
{
    public void Configure(EntityTypeBuilder<ShortlistedPlant> builder)
    {
        builder.ToTable("ShortlistedPlants");
        builder.HasKey(p => new { p.UserId, p.PlantSpeciesId });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PlantSpecies>().WithMany().HasForeignKey(p => p.PlantSpeciesId).OnDelete(DeleteBehavior.Cascade);
    }
}
