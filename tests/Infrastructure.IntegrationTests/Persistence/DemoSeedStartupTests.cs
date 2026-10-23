using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Infrastructure.Identity;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Persistence;

public sealed class DemoSeedStartupTests
{
    private static readonly DateTime Baseline = new(2026, 10, 21, 2, 0, 0, DateTimeKind.Utc);

    [Fact]
    public Task NullTenant_SecondContextSeedDoesNotDuplicate() => WithDatabase(async fixture =>
    {
        await StartAsync(fixture);
        await StartAsync(fixture);
        await using var db = fixture.CreateContext("DEPOT-A");
        Assert.Equal(50, await db.Containers.CountAsync(Ct));
        Assert.Equal(1, await db.DemoSeedRuns.CountAsync(Ct));
        var users = await db.Users.Include(user => user.UserRoles).ToListAsync(Ct);
        Assert.Equal(4, users.Count);
        foreach (var (name, role) in new[] { ("yard", "YardPlanner"), ("order", "OrderClerk"), ("gate", "GateClerk") })
        {
            var user = Assert.Single(users, user => user.UserName == name);
            Assert.Equal(role, user.Role);
            Assert.Equal("DEPOT-A", user.TenantId);
            Assert.Single(user.UserRoles);
            Assert.True(new PasswordHasher().VerifyPassword("DemoPassword!123", user.PasswordHash));
        }
        Assert.Equal(Baseline.AddDays(-20), await db.ContainerLifecycles.MinAsync(lifecycle => lifecycle.InDate, Ct));
    });

    [Fact]
    public Task ConcurrentFirstStartup_BothSucceed() => WithDatabase(async fixture =>
    {
        await Task.WhenAll(StartAsync(fixture), StartAsync(fixture));
        await using var db = fixture.CreateContext("DEPOT-A");
        Assert.Equal(4, await db.Roles.CountAsync(Ct));
        Assert.Equal(4, await db.Users.CountAsync(Ct));
        Assert.Equal(50, await db.Containers.CountAsync(Ct));
        Assert.Equal(1, await db.DemoSeedRuns.CountAsync(Ct));
    });

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Development", false)]
    public Task ProductionOrDisabled_NoDemoUsersOrData(string environment, bool enabled) => WithDatabase(async fixture =>
    {
        await StartAsync(fixture, environment, enabled);
        await using var db = fixture.CreateContext("DEPOT-A");
        Assert.Equal(1, await db.Users.CountAsync(Ct));
        Assert.Equal(0, await db.Containers.CountAsync(Ct));
        Assert.Equal(0, await db.DemoSeedRuns.CountAsync(Ct));
    });

    [Fact]
    public Task LegacyCodesWithoutMarker_RefusesToMutate() => WithDatabase(async fixture =>
    {
        await using (var setup = fixture.CreateContext("DEPOT-A"))
        {
            setup.YardBlocks.Add(YardBlock.CreateStandard("A", "Existing user block", 3, 2, 1, "DEPOT-A"));
            await setup.SaveChangesAsync(Ct);
        }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(fixture));
        Assert.Contains("without dataset marker", error.Message);
        await using var db = fixture.CreateContext("DEPOT-A");
        Assert.Equal(1, await db.YardBlocks.CountAsync(Ct));
        Assert.Equal(0, await db.Containers.CountAsync(Ct));
        Assert.Equal(0, await db.DemoSeedRuns.CountAsync(Ct));
    });

    [Theory]
    [InlineData("OTHER-DEPOT", "YardPlanner")]
    [InlineData("DEPOT-A", "GateClerk")]
    public Task ExistingDemoUserWrongTenantOrRole_IsConflict(string tenant, string role) => WithDatabase(async fixture =>
    {
        const string originalHash = "existing-user-password-must-not-change";
        await using (var setup = fixture.CreateContext())
        {
            setup.Users.Add(ApplicationUser.Create("yard", "existing@example.test", "Existing", "User", tenant, role, originalHash).Value!);
            await setup.SaveChangesAsync(Ct);
        }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(fixture));
        Assert.Contains("Demo user yard conflicts", error.Message);
        await using var db = fixture.CreateContext("DEPOT-A");
        var user = await db.Users.SingleAsync(user => user.UserName == "yard", Ct);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.Equal(tenant, user.TenantId);
        Assert.Equal(role, user.Role);
        Assert.Equal(0, await db.Containers.CountAsync(Ct));
        Assert.Equal(0, await db.DemoSeedRuns.CountAsync(Ct));
    });

    [Fact]
    public Task FailureBeforeMarker_RollsBackAllDemoDataAndUsers() => WithDatabase(async fixture =>
    {
        await using (var setup = fixture.CreateContext())
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION fail_demo_marker() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'injected seed completion failure'; END $$;
                CREATE TRIGGER fail_demo_marker BEFORE INSERT ON demo_seed_runs
                FOR EACH ROW EXECUTE FUNCTION fail_demo_marker();
                """, Ct);
        var error = await Assert.ThrowsAnyAsync<Exception>(() => StartAsync(fixture));
        Assert.Contains("injected seed completion failure", error.ToString());
        await using var db = fixture.CreateContext("DEPOT-A");
        Assert.Equal(0, await db.Containers.CountAsync(Ct));
        Assert.Equal(0, await db.YardBlocks.CountAsync(Ct));
        Assert.Equal(0, await db.LineOperators.CountAsync(Ct));
        Assert.Equal(0, await db.DemoSeedRuns.CountAsync(Ct));
        Assert.Equal(1, await db.Users.CountAsync(Ct));
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task WithDatabase(Func<PostgreSqlFixture, Task> test)
    {
        await using var fixture = new PostgreSqlFixture();
        await fixture.InitializeAsync();
        await test(fixture);
    }

    private static async Task StartAsync(PostgreSqlFixture fixture, string environment = "Development", bool enabled = true)
    {
        await using var db = fixture.CreateContext(); // Startup has no HttpContext tenant.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:TenantId"] = "DEPOT-A", ["Admin:UserName"] = "admin", ["Admin:Password"] = "AdminPassword!123",
            ["Seed:DemoData"] = enabled.ToString(), ["Seed:UserPassword"] = "DemoPassword!123"
        }).Build();
        var host = Substitute.For<IHostEnvironment>();
        host.EnvironmentName.Returns(environment);
        var hasher = new PasswordHasher();
        using var services = new ServiceCollection().AddSingleton(host)
            .AddSingleton<IPasswordHasher>(hasher).AddSingleton<TimeProvider>(new FixedTimeProvider()).BuildServiceProvider();
        var demo = ActivatorUtilities.CreateInstance<DemoDataSeeder>(services, db, configuration, NullLogger<DemoDataSeeder>.Instance);
        var bootstrap = new AppDbSeeder(db, hasher, configuration, NullLogger<AppDbSeeder>.Instance, demo);
        await bootstrap.SeedAsync(Ct);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Baseline);
    }
}
