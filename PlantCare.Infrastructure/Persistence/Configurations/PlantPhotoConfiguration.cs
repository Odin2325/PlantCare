using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class PlantPhotoEntryConfiguration : IEntityTypeConfiguration<PlantPhotoEntry>
{
    public void Configure(EntityTypeBuilder<PlantPhotoEntry> b)
    {
        b.ToTable("PlantPhotoEntries"); b.HasKey(p => p.Id); b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.PhotographedOn).HasColumnType("date");
        b.Property(p => p.Notes).HasMaxLength(500);
        b.Property(p => p.UploadFingerprint).HasMaxLength(64).IsUnicode(false);
        b.Property(p => p.Version).IsConcurrencyToken();
        b.HasOne(p => p.UserPlant).WithMany().HasForeignKey(p => p.UserPlantId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => new { p.UserPlantId, p.UploadRequestId }).IsUnique();
        b.HasIndex(p => new { p.UserPlantId, p.PhotographedOn, p.Id });
    }
}
internal sealed class PlantPhotoMediaConfiguration : IEntityTypeConfiguration<PlantPhotoMedia>
{
    public void Configure(EntityTypeBuilder<PlantPhotoMedia> b)
    {
        b.ToTable("PlantPhotoMedia"); b.HasKey(p => p.Id); b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Image).HasMaxLength(524288).IsRequired();
        b.Property(p => p.Thumbnail).HasMaxLength(65536).IsRequired();
        b.Property(p => p.ImageSha256).HasMaxLength(64).IsUnicode(false);
        b.Property(p => p.ThumbnailSha256).HasMaxLength(64).IsUnicode(false);
        b.HasOne(p => p.Entry).WithOne().HasForeignKey<PlantPhotoMedia>(p => p.Id).OnDelete(DeleteBehavior.Cascade);
    }
}
