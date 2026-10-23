using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 6: Xuất container đã giữ chỗ cho DO-A bằng DO-B → 409 G4.
/// </summary>
[Collection("Api")]
public sealed class Scenario06_GateOutWrongOrderReservationTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario06_GateOut_ContainerReservedForDifferentOrder_Returns409_G4()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var orderA = "DO-A-" + Guid.NewGuid().ToString("N")[..6];
        var orderB = "DO-B-" + Guid.NewGuid().ToString("N")[..6];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 6").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 3, 1, 1, tenantId);
            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);

            var lc = ContainerLifecycle.GateIn(container.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-12345", slot.Id, DateTime.UtcNow, tenantId);

            var doA = DeliveryOrder.Create(orderA, "MSC", taxCode, "Customer 6", DateTime.UtcNow.AddDays(7), "VESSEL A", tenantId).Value!;
            doA.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

            var doB = DeliveryOrder.Create(orderB, "MSC", taxCode, "Customer 6", DateTime.UtcNow.AddDays(7), "VESSEL B", tenantId).Value!;
            doB.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.ContainerLifecycles.Add(lc);
            db.DeliveryOrders.AddRange(doA, doB);
            await db.SaveChangesAsync(ct);

            // Tạo booking Reserved cho DO-A
            var booking = DeliveryOrderBooking.Reserve(doA.Id, doA.Items.First().Id, container.Id, tenantId, DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        // Cố xuất container bằng lệnh DO-B
        var payload = new
        {
            ContainerNumber = container.ContainerNumber.Value,
            OrderNumber = orderB,
            OutVehicleNumber = "51D-99999"
        };

        var response = await client.PostAsJsonAsync("/api/gate/out", payload, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("GateOut.WrongOrderReservation") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "G4") ||
            (json.TryGetProperty("code", out var c) && c.GetString() == "GateOut.WrongOrderReservation"),
            $"Expected G4 or GateOut.WrongOrderReservation error, got: {body}");
    }
}
