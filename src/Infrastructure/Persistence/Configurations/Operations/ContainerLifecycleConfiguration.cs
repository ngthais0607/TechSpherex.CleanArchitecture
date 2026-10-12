using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class ContainerLifecycleConfiguration : IEntityTypeConfiguration<ContainerLifecycle>
{
    public void Configure(EntityTypeBuilder<ContainerLifecycle> builder)
    {
        builder.ToTable("container_lifecycles");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ContainerId)
            .IsRequired();

        builder.HasOne<Container>()
            .WithMany()
            .HasForeignKey(c => c.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.LineOperator)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Classification)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Condition)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.InVehicleNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.InVehicleType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.InDate)
            .IsRequired();

        builder.Property(c => c.OutVehicleNumber)
            .HasMaxLength(20);

        builder.Property(c => c.OutVehicleType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.OutDate);

        builder.Property(c => c.DeliveryOrderId);

        builder.Property(c => c.CurrentSlotId);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.LastModifiedBy)
            .HasMaxLength(100);

        // Frequent query index
        builder.HasIndex(c => c.Status);

        // Partial unique index: A container can only have AT MOST ONE active lifecycle (InYard) in the depot
        builder.HasIndex(c => new { c.TenantId, c.ContainerId })
            .IsUnique()
            .HasFilter("\"status\" = 'InYard'");

        // Optimistic concurrency using PostgreSQL xmin
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
