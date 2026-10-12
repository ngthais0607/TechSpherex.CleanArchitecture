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

        builder.OwnsOne(s => s.Position, p =>
        {
            p.Property(x => x.Block).HasColumnName("position_block").HasMaxLength(20);
            p.Property(x => x.Bay).HasColumnName("position_bay");
            p.Property(x => x.Row).HasColumnName("position_row");
            p.Property(x => x.Tier).HasColumnName("position_tier");
        });
    }
}
