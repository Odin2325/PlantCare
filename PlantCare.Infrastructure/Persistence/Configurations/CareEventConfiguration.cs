using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class CareEventConfiguration
    : IEntityTypeConfiguration<CareEvent>
{
    public void Configure(
        EntityTypeBuilder<CareEvent> builder)
    {
        builder.ToTable("CareEvents");

        builder.HasKey(careEvent => careEvent.Id);

        builder.Property(careEvent => careEvent.Id)
            .ValueGeneratedNever();

        builder.Property(careEvent => careEvent.CareScheduleId)
            .IsRequired();

        builder.Property(careEvent => careEvent.CompletedAtUtc)
            .IsRequired();

        builder.Property(careEvent => careEvent.RecordedAtUtc)
            .IsRequired();

        builder.Property(careEvent => careEvent.Notes)
            .HasMaxLength(CareEvent.NotesMaxLength);

        builder
            .HasOne(careEvent => careEvent.CareSchedule)
            .WithMany()
            .HasForeignKey(
                careEvent => careEvent.CareScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(
            careEvent => careEvent.CareScheduleId);
    }
}