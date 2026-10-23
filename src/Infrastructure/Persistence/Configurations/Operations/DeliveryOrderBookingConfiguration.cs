using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class DeliveryOrderBookingConfiguration : IEntityTypeConfiguration<DeliveryOrderBooking>
{
    public void Configure(EntityTypeBuilder<DeliveryOrderBooking> builder)
    {
        builder.ToTable("delivery_order_bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Status).HasConversion<int>();

        builder.HasIndex(b => new { b.TenantId, b.ContainerId })
            .IsUnique()
            .HasFilter("\"status\" = 1")
            .HasDatabaseName("uq_booking_container_reserved");
    }
}
