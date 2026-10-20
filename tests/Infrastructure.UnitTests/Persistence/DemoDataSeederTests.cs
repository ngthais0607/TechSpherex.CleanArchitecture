using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.Persistence;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Persistence;

public sealed class DemoDataSeederTests
{
    private readonly AppDbContext _context;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeederTests()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns("DEPOT-A");
        var timeProvider = TimeProvider.System;
        var currentUser = Substitute.For<ICurrentUser>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, tenantProvider, timeProvider, currentUser);
        _logger = Substitute.For<ILogger<DemoDataSeeder>>();
    }

    [Fact]
    public async Task SeedAsync_WhenDisabled_ShouldDoNothing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Seed:DemoData", "false" }
            })
            .Build();

        var seeder = new DemoDataSeeder(_context, config, _logger);
        await seeder.SeedAsync();

        (await _context.YardBlocks.CountAsync()).Should().Be(0);
        (await _context.Containers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedAsync_WhenEnabled_ShouldSeedAllDemoData()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Seed:DemoData", "true" },
                { "Admin:TenantId", "DEPOT-A" }
            })
            .Build();

        var seeder = new DemoDataSeeder(_context, config, _logger);
        await seeder.SeedAsync();

        // LineOperators & OwnerCodes
        (await _context.LineOperators.CountAsync()).Should().Be(2);
        (await _context.LineOperatorOwnerCodes.CountAsync()).Should().Be(4);

        // Customers
        (await _context.Customers.CountAsync()).Should().Be(3);

        // YardBlocks
        (await _context.YardBlocks.CountAsync()).Should().Be(3);

        // Containers
        (await _context.Containers.CountAsync()).Should().Be(50);

        // DeliveryOrders
        (await _context.DeliveryOrders.CountAsync()).Should().Be(5);

        // Bookings
        var bookings = await _context.Bookings.ToListAsync();
        bookings.Count.Should().Be(13); // 3+2+2+3 (Reserved) + 3 (Shipped)
        bookings.Count(b => b.Status == BookingStatus.Reserved).Should().Be(10);
        bookings.Count(b => b.Status == BookingStatus.Shipped).Should().Be(3);

        // Idempotency check: running again shouldn't duplicate
        await seeder.SeedAsync();
        (await _context.Containers.CountAsync()).Should().Be(50);
    }
}
