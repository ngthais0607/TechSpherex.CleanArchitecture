using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 8: Xuất thành công → Booking Shipped, container Exited, số đã giao tăng 1, vị trí giải phóng.
/// </summary>
[Collection("Api")]
public sealed class Scenario08_GateOutSuccessTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario08_GateOut_Success_UpdatesBookingAndFreesSlot()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);
        Guid slotId;
        Guid orderId;
        Guid containerId;

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 8").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 3, 1, 1, tenantId);
            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);
            slotId = slot.Id;
            containerId = container.Id;

            var lc = ContainerLifecycle.GateIn(container.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-12345", slot.Id, DateTime.UtcNow, tenantId);

            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 8", DateTime.UtcNow.AddDays(7), "VESSEL 8", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);
            orderId = order.Id;

            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.ContainerLifecycles.Add(lc);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);

            // Giữ chỗ
            var booking = DeliveryOrderBooking.Reserve(order.Id, order.Items.First().Id, container.Id, tenantId, DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        // Act: GateOut container
        var payload = new
        {
            ContainerNumber = container.ContainerNumber.Value,
            OrderNumber = orderNumber,
            OutVehicleNumber = "51D-99999"
        };

        var response = await client.PostAsJsonAsync("/api/gate/out", payload, ct);
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent or HttpStatusCode.Created, $"GateOut failed: {await response.Content.ReadAsStringAsync(ct)}");

        // Assert DB state
        await using (var db = factory.Context(tenantId))
        {
            // 1. Slot is freed
            var slot = await db.YardSlots.FindAsync(new object[] { slotId }, ct);
            Assert.NotNull(slot);
            Assert.False(slot.IsOccupied);
            Assert.Null(slot.ContainerId);

            // 2. Booking is Shipped
            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.ContainerId == containerId && b.OrderId == orderId, ct);
            Assert.NotNull(booking);
            Assert.Equal(BookingStatus.Shipped, booking.Status);
            Assert.NotNull(booking.ShippedAt);

            // 3. Lifecycle is GatedOut
            var lc = await db.ContainerLifecycles.FirstOrDefaultAsync(l => l.ContainerId == containerId, ct);
            Assert.NotNull(lc);
            Assert.Equal(LifecycleStatus.GatedOut, lc.Status);
            Assert.NotNull(lc.OutDate);

            // 4. Delivered quantity increased
            var order = await db.DeliveryOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
            Assert.NotNull(order);
            Assert.Equal(1, order.Items.First().DeliveredQuantity);
        }
    }
}
