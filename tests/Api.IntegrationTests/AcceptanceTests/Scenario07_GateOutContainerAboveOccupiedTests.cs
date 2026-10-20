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
/// Kịch bản 7: Xuất container đang bị container khác đè → 409 Y5.
/// </summary>
[Collection("Api")]
public sealed class Scenario07_GateOutContainerAboveOccupiedTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario07_GateOut_ContainerWithContainerAbove_Returns409_Y5()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var cBottom = OperationScenario.NewContainer("MSC", tenantId);
        var cTop = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 7").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Stack Block", 3, 1, 2, tenantId);
            var slotTier1 = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            var slotTier2 = block.Slots.First(s => s.Bay == 1 && s.Tier == 2);

            slotTier1.Occupy(cBottom.Id);
            slotTier2.Occupy(cTop.Id);

            var lcBottom = ContainerLifecycle.GateIn(cBottom.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-11111", slotTier1.Id, DateTime.UtcNow, tenantId);
            var lcTop = ContainerLifecycle.GateIn(cTop.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-22222", slotTier2.Id, DateTime.UtcNow, tenantId);

            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 7", DateTime.UtcNow.AddDays(7), "VESSEL 7", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

            db.YardBlocks.Add(block);
            db.Containers.AddRange(cBottom, cTop);
            db.ContainerLifecycles.AddRange(lcBottom, lcTop);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);

            // Giữ chỗ cho cBottom
            var booking = DeliveryOrderBooking.Reserve(order.Id, order.Items.First().Id, cBottom.Id, tenantId, DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        // Cố xuất cBottom khi cTop đang đè ở Tier 2
        var payload = new
        {
            ContainerNumber = cBottom.ContainerNumber.Value,
            OrderNumber = orderNumber,
            OutVehicleNumber = "51D-88888"
        };

        var response = await client.PostAsJsonAsync("/api/gate/out", payload, ct);

        Assert.True(response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("YardSlot.CannotRemoveWithContainersAbove") ||
            body.Contains("StackRule.ContainerAboveMustBeEmpty") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "Y5") ||
            (json.TryGetProperty("code", out var c) && (c.GetString() == "YardSlot.CannotRemoveWithContainersAbove" || c.GetString() == "StackRule.ContainerAboveMustBeEmpty")),
            $"Expected Y5 or StackRule.ContainerAboveMustBeEmpty error, got: {body}");
    }
}
