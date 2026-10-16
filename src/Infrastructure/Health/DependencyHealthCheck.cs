using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SnpDepotManagement.Infrastructure.Persistence;
using StackExchange.Redis;

namespace SnpDepotManagement.Infrastructure.Health;

public sealed class DependencyHealthCheck(AppDbContext db, IConfiguration configuration, IConnectionMultiplexer? redis = null) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken)) return HealthCheckResult.Unhealthy("PostgreSQL unavailable.");
        }
        catch (Exception exception) { return HealthCheckResult.Unhealthy("PostgreSQL unavailable.", exception); }
        var required = configuration.GetValue<bool>("Redis:Required");
        if (redis is not null)
        {
            try
            {
                if (redis.IsConnected)
                {
                    await redis.GetDatabase().PingAsync().WaitAsync(cancellationToken);
                    return HealthCheckResult.Healthy("PostgreSQL and Redis available.");
                }
            }
            catch (Exception exception)
            {
                return required ? HealthCheckResult.Unhealthy("Required Redis unavailable.", exception)
                    : HealthCheckResult.Degraded("Optional Redis unavailable.", exception);
            }
        }
        return required ? HealthCheckResult.Unhealthy("Required Redis unavailable or not configured.")
            : HealthCheckResult.Degraded("PostgreSQL available; optional Redis unavailable or not configured.");
    }
}
