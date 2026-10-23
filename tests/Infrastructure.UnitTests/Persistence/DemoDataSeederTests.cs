using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Persistence;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Persistence;

public sealed class DemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenDisabled_DoesNotWriteOrNeedRelationalTransaction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options, Substitute.For<ITenantProvider>(),
            TimeProvider.System, Substitute.For<ICurrentUser>());
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Seed:DemoData"] = "false" }).Build();
        await new DemoDataSeeder(context, config, NullLogger<DemoDataSeeder>.Instance,
                Substitute.For<IHostEnvironment>(), Substitute.For<IPasswordHasher>(), TimeProvider.System)
            .SeedAsync(TestContext.Current.CancellationToken);
        Assert.Empty(await context.Containers.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
    }
}
