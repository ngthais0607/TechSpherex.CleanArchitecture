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
/// Kịch bản 14: POST /gate/validate trả kèm container (hãng khai thác, loại, ISO, cỡ, vị trí hiện tại)
/// và order khi đã nhập đủ số container và số lệnh.
/// </summary>
[Collection("Api")]
public sealed class Scenario14_GateValidateTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario14_ValidateGate_ReturnsContainerAndOrderInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Samsung Vietnam", tenantId).Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 3, 1, 1, tenantId);
            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);

            var lc = ContainerLifecycle.GateIn(container.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-12345", slot.Id, DateTime.UtcNow, tenantId);

            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Samsung Vietnam", DateTime.UtcNow.AddDays(7), "MSC OSCAR", tenantId).Value!;
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

        // Act: Validate GateOut with containerNumber and orderNumber
        var payload = new
        {
            ContainerNumber = container.ContainerNumber.Value,
            Type = "GateOut",
            OrderNumber = orderNumber
        };

        var response = await client.PostAsJsonAsync("/api/gate/validate", payload, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert Checks
        Assert.True(json.TryGetProperty("checks", out var checks));
        Assert.True(checks.GetArrayLength() > 0);

        // Assert Container Info
        Assert.True(json.TryGetProperty("container", out var contElement) && contElement.ValueKind != JsonValueKind.Null);
        Assert.Equal(container.ContainerNumber.Value, contElement.GetProperty("containerNumber").GetString());
        Assert.Equal("22G1", contElement.GetProperty("isoCode").GetString());
        Assert.Equal("MSC", contElement.GetProperty("ownerCode").GetString());
        Assert.Equal("Ft20", contElement.GetProperty("size").GetString());
        Assert.Equal("Dry", contElement.GetProperty("type").GetString());
        Assert.NotNull(contElement.GetProperty("currentLocation").GetString());

        // Assert Order Info
        Assert.True(json.TryGetProperty("order", out var orderElement) && orderElement.ValueKind != JsonValueKind.Null);
        Assert.Equal(orderNumber.ToUpperInvariant(), orderElement.GetProperty("orderNumber").GetString());
        Assert.Equal("MSC", orderElement.GetProperty("lineOperator").GetString());
        Assert.Equal("Samsung Vietnam", orderElement.GetProperty("customerName").GetString());
    }
}
