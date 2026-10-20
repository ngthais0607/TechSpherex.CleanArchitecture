using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.MasterData;

public sealed class DepotConfiguration : IEntityTypeConfiguration<Depot>
{
    public void Configure(EntityTypeBuilder<Depot> builder)
    {
        builder.ToTable("depots");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Timezone)
            .HasMaxLength(50)
            .IsRequired();
    }
}
