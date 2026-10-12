using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Yard;

public sealed class YardBlockConfiguration : IEntityTypeConfiguration<YardBlock>
{
    public void Configure(EntityTypeBuilder<YardBlock> builder)
    {
        builder.ToTable("yard_blocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(b => new { b.TenantId, b.Code })
            .IsUnique();

        builder.Property(b => b.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.BlockType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.CreatedBy)
            .HasMaxLength(100);

        builder.Property(b => b.LastModifiedBy)
            .HasMaxLength(100);

        builder.HasMany(b => b.Slots)
            .WithOne()
            .HasForeignKey(s => s.BlockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Slots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
