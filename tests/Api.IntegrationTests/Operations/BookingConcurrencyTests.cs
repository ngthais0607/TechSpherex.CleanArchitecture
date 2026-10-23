using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class BookingConcurrencyTests(ApiFactory factory)
{
    [Fact]
    public async Task ConcurrentAssign_SameContainerToTwoOrders_OneSucceeds_OneReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        // 1. Ensure LineOperator and Customer exist
        var lineOpCode = "BKG";
        await using (var db = factory.Context("DEPOT-A"))
        {
            if (!db.LineOperators.Any(l => l.Code == lineOpCode))
            {
                db.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create(lineOpCode, "Booking Line").Value!);
            }
            if (!db.Customers.Any(c => c.TaxCode == "0109998888"))
            {
                db.Customers.Add(Domain.Entities.MasterData.Customer.Create("0109998888", "Booking Customer").Value!);
            }
            await db.SaveChangesAsync(ct);
        }

        // 2. Setup Container InYard
        var block = YardBlock.CreateStandard("B" + Guid.NewGuid().ToString("N")[..6], "Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOpCode, "DEPOT-A");

        var order1Number = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var order2Number = "DO-" + Guid.NewGuid().ToString("N")[..8];

        var order1 = DeliveryOrder.Create(order1Number, lineOpCode, "0109998888", "Booking Customer", DateTime.UtcNow.AddDays(7), "VESSEL 1", "DEPOT-A").Value!;
        order1.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        var order2 = DeliveryOrder.Create(order2Number, lineOpCode, "0109998888", "Booking Customer", DateTime.UtcNow.AddDays(7), "VESSEL 2", "DEPOT-A").Value!;
        order2.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.DeliveryOrders.AddRange(order1, order2);

            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);
            var lifecycle = ContainerLifecycle.GateIn(container.Id, lineOpCode, Classification.A, ContainerCondition.Normal, "TRUCK-BKG", slot.Id, DateTime.UtcNow, "DEPOT-A");
            db.ContainerLifecycles.Add(lifecycle);

            await db.SaveChangesAsync(ct);
        }

        // 3. Concurrently assign same container to both orders
        var request = new AssignContainersRequest(new[] { container.ContainerNumber.Value });
        var task1 = client.PostAsJsonAsync($"/api/delivery-orders/{order1Number}/assign-containers", request, ct);
        var task2 = client.PostAsJsonAsync($"/api/delivery-orders/{order2Number}/assign-containers", request, ct);

        var responses = await Task.WhenAll(task1, task2);

        // One should succeed (200), one should conflict (409)
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        await using (var db = factory.Context("DEPOT-A"))
        {
            var reservedBookings = await db.Bookings
                .Where(b => b.ContainerId == container.Id && b.Status == BookingStatus.Reserved)
                .ToListAsync(ct);

            Assert.Single(reservedBookings);
        }

        foreach (var r in responses) r.Dispose();
    }

    [Fact]
    public async Task GateOut_ContainerAssignedToAnotherOrder_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOpCode = "BKG";
        var block = YardBlock.CreateStandard("B" + Guid.NewGuid().ToString("N")[..6], "Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOpCode, "DEPOT-A");

        var order1Number = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var order2Number = "DO-" + Guid.NewGuid().ToString("N")[..8];

        var order1 = DeliveryOrder.Create(order1Number, lineOpCode, "0109998888", "Booking Customer", DateTime.UtcNow.AddDays(7), "VESSEL 1", "DEPOT-A").Value!;
        order1.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        var order2 = DeliveryOrder.Create(order2Number, lineOpCode, "0109998888", "Booking Customer", DateTime.UtcNow.AddDays(7), "VESSEL 2", "DEPOT-A").Value!;
        order2.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.DeliveryOrders.AddRange(order1, order2);

            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);
            var lifecycle = ContainerLifecycle.GateIn(container.Id, lineOpCode, Classification.A, ContainerCondition.Normal, "TRUCK-BKG", slot.Id, DateTime.UtcNow, "DEPOT-A");
            db.ContainerLifecycles.Add(lifecycle);

            // Reserve for order1
            var booking = DeliveryOrderBooking.Reserve(order1.Id, order1.Items.First().Id, container.Id, "DEPOT-A", DateTime.UtcNow);
            db.Bookings.Add(booking);

            await db.SaveChangesAsync(ct);
        }

        // Try Gate-Out against order2
        var gateOutRes = await client.PostAsJsonAsync("/api/gate/out", new
        {
            ContainerId = container.Id,
            DeliveryOrderId = order2.Id,
            OutVehicleNumber = "OUT-99"
        }, ct);

        Assert.Equal(HttpStatusCode.Conflict, gateOutRes.StatusCode);

        // Try Gate-Out against order1 -> should succeed
        var gateOutOk = await client.PostAsJsonAsync("/api/gate/out", new
        {
            ContainerId = container.Id,
            DeliveryOrderId = order1.Id,
            OutVehicleNumber = "OUT-99"
        }, ct);

        Assert.Equal(HttpStatusCode.NoContent, gateOutOk.StatusCode);

        await using (var db = factory.Context("DEPOT-A"))
        {
            var updatedBooking = await db.Bookings.FirstAsync(b => b.ContainerId == container.Id, ct);
            Assert.Equal(BookingStatus.Shipped, updatedBooking.Status);
        }

        gateOutRes.Dispose();
        gateOutOk.Dispose();
    }
}
