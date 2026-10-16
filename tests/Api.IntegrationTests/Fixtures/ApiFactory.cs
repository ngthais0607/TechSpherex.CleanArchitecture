using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Fixtures;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine").WithDatabase("snp_api_test")
        .WithUsername("test_user").WithPassword("test_password").Build();

    public const string AdminPassword = "TestOnlyPassword!123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "",
                ["Jwt:Secret"] = "TestOnlySecretWithAtLeastThirtyTwoCharacters!",
                ["Jwt:Issuer"] = "ApiTests",
                ["Jwt:Audience"] = "ApiTests",
                ["Admin:UserName"] = "admin",
                ["Admin:Password"] = AdminPassword,
                ["Admin:TenantId"] = "DEPOT-A"
            };
        // Host settings are available before Minimal API registers Infrastructure.
        // ConfigureAppConfiguration alone is replayed too late for captured connection strings.
        foreach (var setting in settings) builder.UseSetting(setting.Key, setting.Value);
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options;
        var tenant = Substitute.For<ITenantProvider>();
        var user = Substitute.For<ICurrentUser>();
        await using var context = new AppDbContext(options, tenant, TimeProvider.System, user);
        await context.Database.MigrateAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public AppDbContext Context(string tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options;
        var tenant = Substitute.For<ITenantProvider>();
        tenant.GetCurrentTenantId().Returns(tenantId);
        return new AppDbContext(options, tenant, TimeProvider.System, Substitute.For<ICurrentUser>());
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory> { }
