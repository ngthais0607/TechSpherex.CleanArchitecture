using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Containers;

public sealed class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("containers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContainerNumber)
            .HasConversion(
                cn => cn.Value,
                val => ContainerNumber.Create(val).Value!)
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.ContainerNumber })
            .IsUnique();

        builder.Property(c => c.IsoCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.OwnerCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.TareWeight)
            .HasPrecision(10, 2);

        builder.Property(c => c.MaxGrossWeight)
            .HasPrecision(10, 2);

        builder.Property(c => c.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Size)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.Classification)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.Condition)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.LastModifiedBy)
            .HasMaxLength(100);
    }
}
