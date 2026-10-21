using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Configurations;

internal sealed class AccountErasureConfiguration : IEntityTypeConfiguration<AccountErasure>
{
    public void Configure(EntityTypeBuilder<AccountErasure> builder)
    {
        builder.ToTable("AccountErasures");
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).ValueGeneratedNever();
        builder.Property(e => e.ErasedAtUtc).IsRequired();
    }
}
