using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Identity;
using SnpDepotManagement.Infrastructure.Persistence;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Persistence;

public sealed class AppDbSeederTests
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppDbSeeder> _logger;
    private readonly AppDbSeeder _sut;

    public AppDbSeederTests()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(string.Empty);
        var timeProvider = TimeProvider.System;
        var currentUser = Substitute.For<ICurrentUser>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, tenantProvider, timeProvider, currentUser);
        _passwordHasher = new PasswordHasher();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Admin:UserName", "superadmin" },
            { "Admin:TenantId", "DEPOT-A" },
            { "Admin:Email", "admin@snp.com.vn" },
            { "Admin:Password", "AdminPassword@123" },
            { "Admin:FirstName", "Root" },
            { "Admin:LastName", "Admin" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        _logger = Substitute.For<ILogger<AppDbSeeder>>();
        var demoDataSeeder = Substitute.For<IDemoDataSeeder>();

        _sut = new AppDbSeeder(_context, _passwordHasher, _configuration, _logger, demoDataSeeder);
    }

    [Fact]
    public async Task SeedAsync_ShouldSeedAllPredefinedRolesAndAdminUser()
    {
        await _sut.SeedAsync();

        var roles = await _context.Roles.ToListAsync();
        roles.Should().HaveCount(4);
        roles.Select(r => r.Name).Should().Contain(new[] { "Admin", "YardPlanner", "OrderClerk", "GateClerk" });

        var adminUser = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserName == "superadmin");

        adminUser.Should().NotBeNull();
        adminUser!.Email.Should().Be("admin@snp.com.vn");
        adminUser.Role.Should().Be("Admin");
        adminUser.UserRoles.Should().HaveCount(1);
        _passwordHasher.VerifyPassword("AdminPassword@123", adminUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenRunTwice_ShouldBeIdempotent()
    {
        await _sut.SeedAsync();
        await _sut.SeedAsync();

        var roles = await _context.Roles.ToListAsync();
        roles.Should().HaveCount(4);

        var users = await _context.Users.ToListAsync();
        users.Should().HaveCount(1);
    }
}
