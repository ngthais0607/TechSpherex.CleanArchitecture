using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoDataSeeder(
    AppDbContext context,
    IConfiguration configuration,
    ILogger<DemoDataSeeder> logger) : IDemoDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Seed:DemoData")) return;
        var tenant = configuration["Admin:TenantId"] ?? "DEPOT-A";
        if (await context.YardBlocks.IgnoreQueryFilters().AnyAsync(block => block.TenantId == tenant, cancellationToken))
        {
            logger.LogInformation("Demo data already exists for tenant {TenantId}, skipping.", tenant);
            return;
        }
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await DemoDatasetWriter.WriteAsync(context, tenant, DateTime.UtcNow, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Seeded demo dataset {DatasetVersion} for tenant {TenantId}.", DemoDataset.Version, tenant);
    }
}
