using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
/// Kịch bản 9: Hủy giao dịch xuất → Booking về Reserved, container về đúng vị trí cũ; nếu vị trí bị chiếm → 409 G9.
/// </summary>
[Collection("Api")]
public sealed class Scenario09_CancelGateTransactionTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario09_CancelGateOut_ReturnsContainerToSlot_AndRevertsBooking()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);
        Guid slotId;
        Guid containerId;

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 9").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 3, 1, 1, tenantId);
            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);
            slotId = slot.Id;
            containerId = container.Id;

            var lc = ContainerLifecycle.GateIn(container.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-12345", slot.Id, DateTime.UtcNow, tenantId);

            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 9", DateTime.UtcNow.AddDays(7), "VESSEL 9", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.ContainerLifecycles.Add(lc);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);

            var booking = DeliveryOrderBooking.Reserve(order.Id, order.Items.First().Id, container.Id, tenantId, DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        // 1. GateOut
        var gateOutPayload = new
        {
            ContainerNumber = container.ContainerNumber.Value,
            OrderNumber = orderNumber,
            OutVehicleNumber = "51D-99999"
        };
        var gateOutRes = await client.PostAsJsonAsync("/api/gate/out", gateOutPayload, ct);
        Assert.True(gateOutRes.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent or HttpStatusCode.Created);

        // Lấy GateTransaction ID của giao dịch GateOut vừa tạo
        Guid transactionId;
        await using (var db = factory.Context(tenantId))
        {
            var tx = await db.GateTransactions
                .Where(t => t.ContainerId == containerId && t.Type == GateTransactionType.GateOut && t.Status == GateTransactionStatus.Active)
                .OrderByDescending(t => t.CreatedAt)
                .FirstAsync(ct);
            transactionId = tx.Id;
        }

        // 2. Hủy giao dịch GateOut khi slot còn trống
        var cancelRes = await client.DeleteAsync($"/api/gate/transactions/{transactionId}/cancel", ct);
        Assert.True(cancelRes.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent, $"Cancel failed: {await cancelRes.Content.ReadAsStringAsync(ct)}");

        // 3. Verify: Slot được tái chiếm, Booking về Reserved, Lifecycle InYard
        await using (var db = factory.Context(tenantId))
        {
            var slot = await db.YardSlots.FindAsync(new object[] { slotId }, ct);
            Assert.NotNull(slot);
            Assert.True(slot.IsOccupied);
            Assert.Equal(containerId, slot.ContainerId);

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.ContainerId == containerId, ct);
            Assert.NotNull(booking);
            Assert.Equal(BookingStatus.Reserved, booking.Status);

            var lc = await db.ContainerLifecycles.FirstOrDefaultAsync(l => l.ContainerId == containerId, ct);
            Assert.NotNull(lc);
            Assert.Equal(LifecycleStatus.InYard, lc.Status);
            Assert.Null(lc.OutDate);
        }
    }

    [Fact]
    public async Task Scenario09_CancelGateOut_WhenSlotOccupied_Returns409_G9()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var cOriginal = OperationScenario.NewContainer("MSC", tenantId);
        var cNew = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);
        Guid slotId;
        Guid origId;

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 9B").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 3, 1, 1, tenantId);
            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(cOriginal.Id);
            slotId = slot.Id;
            origId = cOriginal.Id;

            var lc = ContainerLifecycle.GateIn(cOriginal.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-12345", slot.Id, DateTime.UtcNow, tenantId);

            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 9B", DateTime.UtcNow.AddDays(7), "VESSEL 9", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

            db.YardBlocks.Add(block);
            db.Containers.AddRange(cOriginal, cNew);
            db.ContainerLifecycles.Add(lc);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);

            var booking = DeliveryOrderBooking.Reserve(order.Id, order.Items.First().Id, cOriginal.Id, tenantId, DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        // 1. GateOut cOriginal
        var gateOutPayload = new
        {
            ContainerNumber = cOriginal.ContainerNumber.Value,
            OrderNumber = orderNumber,
            OutVehicleNumber = "51D-99999"
        };
        var gateOutRes = await client.PostAsJsonAsync("/api/gate/out", gateOutPayload, ct);
        Assert.True(gateOutRes.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent or HttpStatusCode.Created);

        Guid transactionId;
        await using (var db = factory.Context(tenantId))
        {
            var tx = await db.GateTransactions
                .Where(t => t.ContainerId == origId && t.Type == GateTransactionType.GateOut && t.Status == GateTransactionStatus.Active)
                .OrderByDescending(t => t.CreatedAt)
                .FirstAsync(ct);
            transactionId = tx.Id;

            // 2. Chiếm slot cũ bởi cNew
            var slot = await db.YardSlots.FindAsync(new object[] { slotId }, ct);
            slot!.Occupy(cNew.Id);
            await db.SaveChangesAsync(ct);
        }

        // 3. Cố hủy GateOut khi slot đã bị cNew chiếm
        var cancelRes = await client.DeleteAsync($"/api/gate/transactions/{transactionId}/cancel", ct);
        Assert.Equal(HttpStatusCode.Conflict, cancelRes.StatusCode);

        var body = await cancelRes.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("G9.SlotOccupied") ||
            body.Contains("GateTransaction.SlotOccupied") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "G9") ||
            (json.TryGetProperty("code", out var c) && (c.GetString() == "G9.SlotOccupied" || c.GetString() == "GateTransaction.SlotOccupied")),
            $"Expected G9 or G9.SlotOccupied error, got: {body}");
    }
}
