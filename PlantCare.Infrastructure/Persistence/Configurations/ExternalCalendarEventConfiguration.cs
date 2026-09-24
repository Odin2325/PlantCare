using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class ExternalCalendarEventConfiguration
    : IEntityTypeConfiguration<ExternalCalendarEvent>
{
    public void Configure(EntityTypeBuilder<ExternalCalendarEvent> builder)
    {
        builder.HasKey(calendarEvent => calendarEvent.Id);
        builder.Property(calendarEvent => calendarEvent.PlantCareEntryId)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(calendarEvent => calendarEvent.ExternalEventId)
            .HasMaxLength(512)
            .IsRequired();
        builder.HasIndex(calendarEvent => new
            {
                calendarEvent.ConnectionId,
                calendarEvent.PlantCareEntryId
            })
            .IsUnique();
        builder.HasOne<ExternalCalendarConnection>()
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
