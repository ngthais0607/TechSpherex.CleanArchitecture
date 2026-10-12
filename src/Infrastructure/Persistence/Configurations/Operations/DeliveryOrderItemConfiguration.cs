using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class DeliveryOrderItemConfiguration : IEntityTypeConfiguration<DeliveryOrderItem>
{
    public void Configure(EntityTypeBuilder<DeliveryOrderItem> builder)
    {
        builder.ToTable("delivery_order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.DeliveryOrderId)
            .IsRequired();

        builder.Property(i => i.Size)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(i => i.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.RequiredQuantity)
            .IsRequired();

        builder.Property(i => i.DeliveredQuantity)
            .IsRequired();

        // Optimistic concurrency using PostgreSQL xmin
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
