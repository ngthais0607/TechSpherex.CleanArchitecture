using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using SnpDepotManagement.Application.Abstractions.Identity;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoDataSeeder(
    AppDbContext context,
    IConfiguration configuration,
    ILogger<DemoDataSeeder> logger,
    IHostEnvironment environment,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IDemoDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Seed:DemoData")) return;
        if (!environment.IsDevelopment())
        {
            logger.LogInformation("Skipping demo seed outside Development.");
            return;
        }
        var tenant = configuration["Admin:TenantId"]?.Trim();
        if (string.IsNullOrWhiteSpace(tenant))
            throw new InvalidOperationException("Admin:TenantId must be configured before demo seeding.");
        await new DemoSeedCoordinator(context, configuration, passwordHasher, timeProvider).RunAsync(tenant, cancellationToken);
        logger.LogInformation("Demo dataset {DatasetVersion} is ready for tenant {TenantId}.", DemoDataset.Version, tenant);
    }
}
