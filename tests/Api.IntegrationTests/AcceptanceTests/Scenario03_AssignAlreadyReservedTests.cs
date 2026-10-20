using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 3: Gán cùng một container cho hai lệnh → lần hai 409 D3.
/// </summary>
[Collection("Api")]
public sealed class Scenario03_AssignAlreadyReservedTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario03_AssignContainer_ToSecondOrder_Returns409_D3()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var orderNumber1 = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var orderNumber2 = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 3").Value!);

            var order1 = DeliveryOrder.Create(orderNumber1, "MSC", taxCode, "Customer 3", DateTime.UtcNow.AddDays(7), "VESSEL 1", tenantId).Value!;
            order1.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

            var order2 = DeliveryOrder.Create(orderNumber2, "MSC", taxCode, "Customer 3", DateTime.UtcNow.AddDays(7), "VESSEL 2", tenantId).Value!;
            order2.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

            var lifecycle = ContainerLifecycle.GateIn(container.Id, container.OwnerCode, Classification.A, ContainerCondition.Normal, "29C-11111", null, DateTime.UtcNow, tenantId);
            db.Containers.Add(container);
            db.ContainerLifecycles.Add(lifecycle);
            db.DeliveryOrders.AddRange(order1, order2);
            await db.SaveChangesAsync(ct);
        }

        // 1. Assign to Order 1 -> Success
        var payload = new { ContainerNumbers = new[] { container.ContainerNumber.Value } };
        var res1 = await client.PostAsJsonAsync($"/api/delivery-orders/{orderNumber1}/assign", payload, ct);
        Assert.True(res1.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        // 2. Assign same container to Order 2 -> Conflict 409
        var res2 = await client.PostAsJsonAsync($"/api/delivery-orders/{orderNumber2}/assign", payload, ct);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);

        var body2 = await res2.Content.ReadAsStringAsync(ct);
        var json2 = JsonSerializer.Deserialize<JsonElement>(body2);

        Assert.True(
            body2.Contains("Booking.ContainerAlreadyReserved") ||
            (json2.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "D3") ||
            (json2.TryGetProperty("code", out var c) && c.GetString() == "Booking.ContainerAlreadyReserved"),
            $"Expected D3 or Booking.ContainerAlreadyReserved error, got: {body2}");
    }
}
