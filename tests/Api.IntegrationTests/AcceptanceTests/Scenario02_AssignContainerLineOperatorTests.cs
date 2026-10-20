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
/// Kịch bản 2: Gán container Maersk Dry 20ft vào lệnh của hãng khác → 422 D2.
/// </summary>
[Collection("Api")]
public sealed class Scenario02_AssignContainerLineOperatorTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario02_AssignContainer_DifferentLineOperator_Returns422_D2()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var prefixMae = "MAE";
        var tenantId = "DEPOT-A";

        // Setup master data & container & order
        var container = OperationScenario.NewContainer(prefixMae, tenantId);
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);
            if (!db.LineOperators.Any(l => l.Code == "MAEU"))
                db.LineOperators.Add(LineOperator.Create("MAEU", "Maersk Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 2").Value!);

            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 2", DateTime.UtcNow.AddDays(7), "VESSEL 1", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

            var lifecycle = ContainerLifecycle.GateIn(container.Id, container.OwnerCode, Classification.A, ContainerCondition.Normal, "29C-11111", null, DateTime.UtcNow, tenantId);
            db.Containers.Add(container);
            db.ContainerLifecycles.Add(lifecycle);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);
        }

        // Act: Assign MAEU container to MSC DeliveryOrder
        var payload = new
        {
            ContainerNumbers = new[] { container.ContainerNumber.Value }
        };

        var response = await client.PostAsJsonAsync($"/api/delivery-orders/{orderNumber}/assign", payload, ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("OrderRule.LineOperatorMismatch") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "D2") ||
            (json.TryGetProperty("code", out var c) && c.GetString() == "OrderRule.LineOperatorMismatch"),
            $"Expected D2 or OrderRule.LineOperatorMismatch error, got: {body}");
    }
}
