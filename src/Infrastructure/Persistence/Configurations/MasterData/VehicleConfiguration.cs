using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.MasterData;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.PlateNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(v => v.PlateNumber)
            .IsUnique();

        builder.Property(v => v.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(100);

        builder.Property(v => v.LastModifiedBy)
            .HasMaxLength(100);
    }
}
