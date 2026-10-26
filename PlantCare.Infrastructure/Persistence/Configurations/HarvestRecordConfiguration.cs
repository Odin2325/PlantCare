using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;
namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class HarvestRecordConfiguration : IEntityTypeConfiguration<HarvestRecord>
{
    public void Configure(EntityTypeBuilder<HarvestRecord> b)
    {
        b.ToTable("HarvestRecords");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.HarvestedOn).HasColumnType("date");
        b.Property(r => r.Quantity).HasPrecision(10, 3);
        b.Property(r => r.Unit).HasMaxLength(5);
        b.Property(r => r.Notes).HasMaxLength(1000);
        b.HasOne(r => r.UserPlant).WithMany().HasForeignKey(r => r.UserPlantId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(r => new { r.UserPlantId, r.HarvestedOn });
    }
}