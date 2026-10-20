using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.ToTable("delivery_orders");

        builder.HasKey(o => o.Id);
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.Property(o => o.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.OrderNumber })
            .IsUnique();

        builder.HasIndex(o => o.ExpiryDate);

        builder.Property(o => o.CustomerTaxCode)
            .HasMaxLength(13)
            .IsRequired();

        builder.Property(o => o.CustomerName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.LineOperator)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.OutboundVessel)
            .HasMaxLength(100);

        builder.Property(o => o.IsCompleted)
            .IsRequired();

        builder.Property(o => o.CreatedBy)
            .HasMaxLength(100);

        builder.Property(o => o.LastModifiedBy)
            .HasMaxLength(100);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
