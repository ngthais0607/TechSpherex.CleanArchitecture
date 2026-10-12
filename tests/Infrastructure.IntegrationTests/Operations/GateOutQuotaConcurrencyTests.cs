using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Operations;

[Collection("PostgreSqlCollection")]
public sealed class GateOutQuotaConcurrencyTests
{
    private readonly PostgreSqlFixture _fixture;

    public GateOutQuotaConcurrencyTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TwoConcurrentGateOuts_OnLastQuotaItem_ShouldAllowOnlyOneAndThrowConcurrencyExceptionOnSecond()
    {
        var tenantId = "QUOTA-DEPOT";
        var order = DeliveryOrder.Create(
            "DO-CONCURRENCY-001",
            "MAERSK",
            "0300123456",
            "Test Logistics",
            DateTime.UtcNow.AddDays(7),
            "Vessel Test",
            tenantId).Value!;

        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        await using (var setupContext = _fixture.CreateContext(tenantId))
        {
            setupContext.DeliveryOrders.Add(order);
            await setupContext.SaveChangesAsync();
        }

        var itemId = order.Items.First().Id;

        // Two concurrent contexts load the item with remaining quota = 1
        await using var contextA = _fixture.CreateContext(tenantId);
        await using var contextB = _fixture.CreateContext(tenantId);

        var itemA = await contextA.DeliveryOrderItems.FirstAsync(i => i.Id == itemId);
        var itemB = await contextB.DeliveryOrderItems.FirstAsync(i => i.Id == itemId);

        // Transaction A allocates 1 and saves
        itemA.Allocate(1).IsSuccess.Should().BeTrue();
        await contextA.SaveChangesAsync();

        // Transaction B attempts to allocate and save
        itemB.Allocate(1).IsSuccess.Should().BeTrue();
        var act = () => contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
