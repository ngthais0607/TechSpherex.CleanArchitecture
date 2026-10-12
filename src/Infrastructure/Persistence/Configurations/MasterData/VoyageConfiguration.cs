using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.MasterData;

public sealed class VoyageConfiguration : IEntityTypeConfiguration<Voyage>
{
    public void Configure(EntityTypeBuilder<Voyage> builder)
    {
        builder.ToTable("voyages");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VesselName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.VoyageNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(v => new { v.VesselName, v.VoyageNumber });

        builder.Property(v => v.Etd)
            .IsRequired();

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(100);

        builder.Property(v => v.LastModifiedBy)
            .HasMaxLength(100);
    }
}
