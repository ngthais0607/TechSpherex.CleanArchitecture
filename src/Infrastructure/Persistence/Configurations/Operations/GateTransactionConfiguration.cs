using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class GateTransactionConfiguration : IEntityTypeConfiguration<GateTransaction>
{
    public void Configure(EntityTypeBuilder<GateTransaction> builder)
    {
        builder.ToTable("gate_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(t => t.VehicleNumber).HasMaxLength(20).IsRequired();
        builder.Property(t => t.DriverName).HasMaxLength(100);
        builder.Property(t => t.VirtualBlockCode).HasMaxLength(20);
        builder.Property(t => t.Type).HasConversion<int>().IsRequired();
        builder.Property(t => t.Status).HasConversion<int>().IsRequired();
        builder.Property(t => t.VehicleType).HasConversion<int>().IsRequired();

        builder.HasIndex(t => new { t.TenantId, t.ContainerId, t.Status });
        builder.HasIndex(t => new { t.TenantId, t.CreatedAt });

        builder.HasMany(t => t.Changes)
            .WithOne()
            .HasForeignKey(c => c.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
