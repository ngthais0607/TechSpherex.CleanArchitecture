using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("snp_depot_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Run migrations on the container
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(string.Empty);
        var currentUser = Substitute.For<ICurrentUser>();

        await using var context = new AppDbContext(options, tenantProvider, TimeProvider.System, currentUser);
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateContext(string? tenantId = null, Guid? userId = null, string? role = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(role);
        currentUser.IsAuthenticated.Returns(userId.HasValue);

        return new AppDbContext(options, tenantProvider, TimeProvider.System, currentUser);
    }
}

[CollectionDefinition("PostgreSqlCollection")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}
