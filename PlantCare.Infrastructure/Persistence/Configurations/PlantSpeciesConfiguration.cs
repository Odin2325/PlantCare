using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class PlantSpeciesConfiguration : IEntityTypeConfiguration<PlantSpecies>
{
    public void Configure(EntityTypeBuilder<PlantSpecies> builder)
    {
        builder.ToTable("PlantSpecies");

        builder.HasKey(plantSpecies => plantSpecies.Id);

        builder.Property(plantSpecies => plantSpecies.Id)
            .ValueGeneratedNever();

        builder.Property(plantSpecies => plantSpecies.CommonName)
            .HasMaxLength(PlantSpecies.CommonNameMaxLength)
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.ScientificName)
            .HasMaxLength(PlantSpecies.ScientificNameMaxLength);

        builder.Property(plantSpecies => plantSpecies.Description)
            .HasMaxLength(PlantSpecies.DescriptionMaxLength)
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.SunlightRequirement)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.SunlightInstructions)
            .HasMaxLength(PlantSpecies.InstructionsMaxLength)
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.DefaultWateringIntervalDays)
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.WateringInstructions)
            .HasMaxLength(PlantSpecies.InstructionsMaxLength)
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.DefaultFertilizingIntervalDays);

        builder.Property(plantSpecies => plantSpecies.FertilizingInstructions)
            .HasMaxLength(PlantSpecies.InstructionsMaxLength);

        builder.Property(plantSpecies => plantSpecies.SoilInstructions)
            .HasMaxLength(PlantSpecies.InstructionsMaxLength)
            .IsRequired();

        builder.Property(plantSpecies => plantSpecies.HumidityInstructions)
            .HasMaxLength(PlantSpecies.InstructionsMaxLength);

        builder.Property(plantSpecies => plantSpecies.MinimumTemperatureCelsius)
            .HasPrecision(5, 2);

        builder.Property(plantSpecies => plantSpecies.MaximumTemperatureCelsius)
            .HasPrecision(5, 2);

        builder.Property(plantSpecies => plantSpecies.IsToxicToPets)
            .IsRequired();

        builder.HasIndex(plantSpecies => plantSpecies.CommonName);

        builder.HasIndex(plantSpecies => plantSpecies.ScientificName);
    }
}