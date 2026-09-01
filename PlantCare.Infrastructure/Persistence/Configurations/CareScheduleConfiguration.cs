using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class CareScheduleConfiguration : IEntityTypeConfiguration<CareSchedule>
{
    public void Configure(EntityTypeBuilder<CareSchedule> builder)
    {
        builder.ToTable("CareSchedules");

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.Id)
            .ValueGeneratedNever();

        builder.Property(schedule => schedule.UserPlantId)
            .IsRequired();

        builder.Property(schedule => schedule.ActionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(schedule => schedule.IntervalDays)
            .IsRequired();

        builder.Property(schedule => schedule.IsEnabled)
            .IsRequired();

        builder
            .HasOne(schedule => schedule.UserPlant)
            .WithMany(userPlant => userPlant.CareSchedules)
            .HasForeignKey(schedule => schedule.UserPlantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(schedule => new
            {
                schedule.UserPlantId,
                schedule.ActionType
            })
            .IsUnique();

        builder
            .HasIndex(schedule => new
            {
                schedule.IsEnabled,
                schedule.NextDueAtUtc
            });
    }
}