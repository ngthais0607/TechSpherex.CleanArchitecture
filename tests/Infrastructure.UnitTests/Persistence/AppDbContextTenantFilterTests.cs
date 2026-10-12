using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Persistence;

public sealed class AppDbContextTenantFilterTests
{
    private readonly ITenantProvider _tenantProvider = Substitute.For<ITenantProvider>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new AppDbContext(options, _tenantProvider, _timeProvider, _currentUser);
    }

    [Fact]
    public async Task QueryFilter_ShouldOnlyReturnEntitiesForCurrentTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        _tenantProvider.GetCurrentTenantId().Returns("DEPOT-01");
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        var now = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        _timeProvider.GetUtcNow().Returns(now);

        // Seed with different tenants
        using (var seedContext = CreateContext(dbName))
        {
            var c1 = CreateValidContainer("MSK", 100001, "DEPOT-01");
            var c2 = CreateValidContainer("MSK", 100002, "DEPOT-02");

            seedContext.Containers.AddRange(c1, c2);
            await seedContext.SaveChangesAsync();
        }

        // Query with current tenant = DEPOT-01
        using (var queryContext = CreateContext(dbName))
        {
            var results = await queryContext.Containers.ToListAsync();

            results.Should().HaveCount(1);
            results.First().TenantId.Should().Be("DEPOT-01");
            results.First().ContainerNumber.SerialNumber.Should().Be("100001");
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAutoFillTenantIdAndAuditFields()
    {
        var dbName = Guid.NewGuid().ToString();
        _tenantProvider.GetCurrentTenantId().Returns("DEPOT-SNP");
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        var now = new DateTimeOffset(2026, 10, 12, 12, 30, 0, TimeSpan.Zero);
        _timeProvider.GetUtcNow().Returns(now);

        using var context = CreateContext(dbName);
        // Container with empty tenantId
        var container = CreateValidContainer("TGH", 200001, "");

        context.Containers.Add(container);
        await context.SaveChangesAsync();

        container.TenantId.Should().Be("DEPOT-SNP");
        container.CreatedAt.Should().Be(now.UtcDateTime);
        container.CreatedBy.Should().Be(userId.ToString());
    }

    private static Container CreateValidContainer(string ownerCode, int serial, string tenantId)
    {
        string raw = $"{ownerCode}U{serial:D6}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = raw[i];
            int val = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
            if (char.IsLetter(c))
            {
                if (val >= 11) val++;
                if (val >= 22) val++;
                if (val >= 33) val++;
            }
            sum += val * (1 << i);
        }
        int rem = sum % 11;
        int checkDigit = rem == 10 ? 0 : rem;

        var number = ContainerNumber.Create($"{raw}{checkDigit}").Value!;
        return Container.Create(number, "22G1", ContainerSize.Ft20, ContainerType.Dry, 2200, 30480, null, ownerCode, tenantId).Value!;
    }
}
