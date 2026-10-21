using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Persistence;

[Collection("PostgreSqlCollection")]
public sealed class DemoDatasetTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Dataset_HasConsistentLifecycleBookingQuotaAndHistory()
    {
        const string tenant = "SEED-DATASET";
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateContext(tenant);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:TenantId"] = tenant,
            ["Seed:DemoData"] = "true",
            ["Seed:UserPassword"] = "TestOnlyPassword!123"
        }).Build();
        var seeder = new DemoDataSeeder(db, config, NullLogger<DemoDataSeeder>.Instance);
        await seeder.SeedAsync(ct);

        var containers = await db.Containers.ToListAsync(ct);
        var lifecycles = await db.ContainerLifecycles.ToListAsync(ct);
        var bookings = await db.Bookings.ToListAsync(ct);
        var orders = await db.DeliveryOrders.Include(order => order.Items).ToListAsync(ct);
        var transactions = await db.GateTransactions.ToListAsync(ct);
        var movements = await db.ContainerMovements.ToListAsync(ct);
        Assert.Equal(3, await db.Customers.CountAsync(ct));
        Assert.Equal(3, await db.YardBlocks.CountAsync(ct));
        Assert.Equal(50, containers.Count);
        Assert.Equal(20, lifecycles.Count(lifecycle => lifecycle.IsInDepot));
        Assert.Equal(3, lifecycles.Count(lifecycle => lifecycle.Status == LifecycleStatus.GatedOut));
        Assert.Equal(27, containers.Count(container => lifecycles.All(lifecycle => lifecycle.ContainerId != container.Id)));
        Assert.Equal(15, lifecycles.Count(lifecycle => lifecycle.IsInDepot && lifecycle.CurrentSlotId != null));
        Assert.Equal(5, lifecycles.Count(lifecycle => lifecycle.IsInDepot && lifecycle.VirtualBlockId != null));
        Assert.Equal(23, transactions.Count(transaction => transaction.Type == GateTransactionType.GateIn));
        Assert.Equal(3, transactions.Count(transaction => transaction.Type == GateTransactionType.GateOut));
        Assert.Equal(8, bookings.Count(booking => booking.Status == BookingStatus.Reserved));
        Assert.Equal(3, bookings.Count(booking => booking.Status == BookingStatus.Shipped));
        foreach (var booking in bookings)
        {
            var container = Assert.Single(containers, container => container.Id == booking.ContainerId);
            var order = Assert.Single(orders, order => order.Id == booking.OrderId);
            var item = Assert.Single(order.Items, item => item.Id == booking.ItemId);
            Assert.Equal(item.Size, container.Size);
            Assert.Equal(item.Type, container.Type);
            Assert.Equal(order.LineOperator, container.OwnerCode is "MSC" or "MED" ? "MSC" : "MAEU");
            Assert.True(booking.ReservedAt <= order.ExpiryDate);
            if (booking.Status == BookingStatus.Reserved)
                Assert.Contains(lifecycles, lifecycle => lifecycle.ContainerId == container.Id && lifecycle.IsInDepot);
        }
        foreach (var container in containers.Where(container => container.ContainerNumber.SerialNumber is "900035" or "900036"))
            Assert.DoesNotContain(bookings, booking => booking.ContainerId == container.Id);
        foreach (var order in orders)
        foreach (var item in order.Items)
        {
            var shipped = bookings.Count(booking => booking.ItemId == item.Id && booking.Status == BookingStatus.Shipped);
            Assert.Equal(shipped, item.DeliveredQuantity);
            Assert.True(shipped <= item.RequiredQuantity);
        }
        foreach (var lifecycle in lifecycles)
        {
            var history = movements.Where(movement => movement.ContainerLifecycleId == lifecycle.Id).ToList();
            Assert.All(history.Where(movement => movement.EventType == ContainerMovementType.Place), movement =>
            {
                Assert.True(movement.OccurredAtUtc > lifecycle.InDate);
                if (lifecycle.OutDate is { } shippedAt) Assert.True(movement.OccurredAtUtc < shippedAt);
            });
            Assert.Contains(history, movement => movement.EventType == ContainerMovementType.GateIn && movement.OccurredAtUtc == lifecycle.InDate);
            var gateIn = Assert.Single(transactions, transaction => transaction.ContainerId == lifecycle.ContainerId && transaction.Type == GateTransactionType.GateIn);
            Assert.Equal(lifecycle.InDate, gateIn.CreatedAt);
            if (lifecycle.OutDate is { } outDate)
            {
                Assert.True(outDate > lifecycle.InDate);
                Assert.Contains(history, movement => movement.EventType == ContainerMovementType.GateOut && movement.OccurredAtUtc == outDate);
                var gateOut = Assert.Single(transactions, transaction => transaction.ContainerId == lifecycle.ContainerId && transaction.Type == GateTransactionType.GateOut);
                Assert.Equal(outDate, gateOut.CreatedAt);
            }
            if (lifecycle.CurrentSlotId.HasValue)
                Assert.Contains(history, movement => movement.EventType == ContainerMovementType.Place);
        }
        var occupied = await db.YardSlots.Where(slot => slot.IsOccupied).ToListAsync(ct);
        Assert.Equal(15, occupied.Count);
        Assert.All(occupied, slot => Assert.Equal(1, slot.Tier));
    }
}
