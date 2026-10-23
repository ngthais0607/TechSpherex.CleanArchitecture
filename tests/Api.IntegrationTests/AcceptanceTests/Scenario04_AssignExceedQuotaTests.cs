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
/// Kịch bản 4: Gán vượt số lượng dòng lệnh → 422 D2.
/// </summary>
[Collection("Api")]
public sealed class Scenario04_AssignExceedQuotaTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario04_AssignContainers_ExceedingQuota_Returns422_D2()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var c1 = OperationScenario.NewContainer("MSC", tenantId);
        var c2 = OperationScenario.NewContainer("MSC", tenantId);
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 4").Value!);

            // Lệnh chỉ có số lượng = 1
            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 4", DateTime.UtcNow.AddDays(7), "VESSEL 4", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

            var lc1 = ContainerLifecycle.GateIn(c1.Id, c1.OwnerCode, Classification.A, ContainerCondition.Normal, "29C-11111", null, DateTime.UtcNow, tenantId);
            var lc2 = ContainerLifecycle.GateIn(c2.Id, c2.OwnerCode, Classification.A, ContainerCondition.Normal, "29C-22222", null, DateTime.UtcNow, tenantId);

            db.Containers.AddRange(c1, c2);
            db.ContainerLifecycles.AddRange(lc1, lc2);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);
        }

        // Gán 2 containers vào lệnh có quota = 1
        var payload = new
        {
            ContainerNumbers = new[] { c1.ContainerNumber.Value, c2.ContainerNumber.Value }
        };

        var response = await client.PostAsJsonAsync($"/api/delivery-orders/{orderNumber}/assign", payload, ct);

        Assert.True(
            response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict,
            $"Expected 422 or 409, got {response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("Booking.QuantityExceeded") ||
            body.Contains("Booking.QuotaExceeded") ||
            body.Contains("OrderRule.InsufficientQuota") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "D2") ||
            (json.TryGetProperty("code", out var c) && (c.GetString() == "Booking.QuantityExceeded" || c.GetString() == "Booking.QuotaExceeded" || c.GetString() == "OrderRule.InsufficientQuota")),
            $"Expected D2 or quota exceeded error, got: {body}");
    }
}
