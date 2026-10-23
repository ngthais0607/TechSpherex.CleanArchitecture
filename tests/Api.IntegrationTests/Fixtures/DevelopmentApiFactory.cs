using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SnpDepotManagement.Infrastructure.Persistence.Seed;

namespace SnpDepotManagement.Api.IntegrationTests.Fixtures;

// Transport checks do not query the DB. Keep the HTTP pipeline real and replace only startup I/O.
public sealed class DevelopmentApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=transport_test;Username=test;Password=test");
        builder.UseSetting("ConnectionStrings:Redis", "");
        builder.UseSetting("Jwt:Secret", "TransportTestSecretWithAtLeastThirtyTwoCharacters!");
        builder.UseSetting("Jwt:Issuer", "TransportTest");
        builder.UseSetting("Jwt:Audience", "TransportTest");
        builder.UseSetting("https_port", "7146");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:4200");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppDbSeeder>();
            services.AddSingleton<IAppDbSeeder, NoStartupDatabase>();
        });
    }

    private sealed class NoStartupDatabase : IAppDbSeeder
    {
        public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
