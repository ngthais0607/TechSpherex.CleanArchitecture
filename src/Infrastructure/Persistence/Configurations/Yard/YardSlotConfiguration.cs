using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Yard;

public sealed class YardSlotConfiguration : IEntityTypeConfiguration<YardSlot>
{
    public void Configure(EntityTypeBuilder<YardSlot> builder)
    {
        builder.ToTable("yard_slots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.BlockId)
            .IsRequired();

        builder.OwnsOne(s => s.Position, p =>
        {
            p.Property(x => x.Block).HasColumnName("position_block").HasMaxLength(20);
            p.Property(x => x.Bay).HasColumnName("position_bay");
            p.Property(x => x.Row).HasColumnName("position_row");
            p.Property(x => x.Tier).HasColumnName("position_tier");
        });

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.BlockId);
        // Note: Composite unique index on (tenant_id, block_id, position_bay, position_row, position_tier)
        // is added at migration level because EF Core doesn't allow cross-entity owned navigation member access in HasIndex.

        builder.Property(s => s.IsOccupied)
            .IsRequired();

        // Optimistic concurrency using PostgreSQL xmin system column
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
