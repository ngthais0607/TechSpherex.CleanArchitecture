using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class ContainerMovementConfiguration : IEntityTypeConfiguration<ContainerMovement>
{
    public void Configure(EntityTypeBuilder<ContainerMovement> builder)
    {
        builder.ToTable("container_movements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(m => m.EventType).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.VehicleNumber).HasMaxLength(30);
        builder.Property(m => m.VehicleType).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Container>().WithMany().HasForeignKey(m => m.ContainerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContainerLifecycle>().WithMany().HasForeignKey(m => m.ContainerLifecycleId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(m => m.FromPosition, position => position.Property(p => p.BlockCode).HasMaxLength(50));
        builder.OwnsOne(m => m.ToPosition, position => position.Property(p => p.BlockCode).HasMaxLength(50));
        builder.HasIndex(m => new { m.TenantId, m.ContainerId, m.OccurredAtUtc, m.Id });
        builder.HasIndex(m => new { m.TenantId, m.ContainerLifecycleId });
    }
}
