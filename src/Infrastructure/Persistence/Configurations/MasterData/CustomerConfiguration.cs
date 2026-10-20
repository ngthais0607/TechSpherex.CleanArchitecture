using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.MasterData;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .HasMaxLength(64)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(c => c.TaxCode)
            .HasMaxLength(13)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.TaxCode })
            .IsUnique();

        builder.Property(c => c.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.LastModifiedBy)
            .HasMaxLength(100);
    }
}
