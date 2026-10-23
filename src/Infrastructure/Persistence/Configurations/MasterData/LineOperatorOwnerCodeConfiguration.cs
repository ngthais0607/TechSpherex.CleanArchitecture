using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.MasterData;

public sealed class LineOperatorOwnerCodeConfiguration : IEntityTypeConfiguration<LineOperatorOwnerCode>
{
    public void Configure(EntityTypeBuilder<LineOperatorOwnerCode> builder)
    {
        builder.ToTable("line_operator_owner_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OwnerCode).HasMaxLength(4).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.OwnerCode }).IsUnique();
    }
}
