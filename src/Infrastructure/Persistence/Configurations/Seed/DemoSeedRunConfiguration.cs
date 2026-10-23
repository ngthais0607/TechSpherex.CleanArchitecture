using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Infrastructure.Persistence.Seed;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Seed;

public sealed class DemoSeedRunConfiguration : IEntityTypeConfiguration<DemoSeedRun>
{
    public void Configure(EntityTypeBuilder<DemoSeedRun> builder)
    {
        builder.ToTable("demo_seed_runs");
        builder.HasKey(run => new { run.TenantId, run.DatasetVersion });
        builder.Property(run => run.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(run => run.DatasetVersion).HasMaxLength(64).IsRequired();
        builder.Property(run => run.CompletedAtUtc).IsRequired();
    }
}
