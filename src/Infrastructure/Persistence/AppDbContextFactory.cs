using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;

namespace SnpDepotManagement.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=snp_depot;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(
            optionsBuilder.Options,
            new DesignTimeTenantProvider(),
            TimeProvider.System,
            new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public TenantInfo? GetCurrentTenant() => null;
        public string? GetCurrentTenantId() => string.Empty;
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public string? UserName => null;
        public string? Role => null;
        public string? TenantId => null;
        public bool IsAuthenticated => false;
    }
}
