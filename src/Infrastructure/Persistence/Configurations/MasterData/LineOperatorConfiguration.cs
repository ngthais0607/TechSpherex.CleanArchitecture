using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.MasterData;

public sealed class LineOperatorConfiguration : IEntityTypeConfiguration<LineOperator>
{
    public void Configure(EntityTypeBuilder<LineOperator> builder)
    {
        builder.ToTable("line_operators");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(l => l.Code)
            .IsUnique();

        builder.Property(l => l.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.CreatedBy)
            .HasMaxLength(100);

        builder.Property(l => l.LastModifiedBy)
            .HasMaxLength(100);
    }
}
