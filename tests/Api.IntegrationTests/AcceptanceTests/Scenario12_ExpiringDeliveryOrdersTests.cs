using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 12: GET /delivery-orders?status=expiring chỉ trả lệnh còn 3 ngày trở xuống và chưa giao đủ.
/// </summary>
[Collection("Api")]
public sealed class Scenario12_ExpiringDeliveryOrdersTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario12_GetDeliveryOrders_WithStatusExpiring_ReturnsOnlyExpiringOrders()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var prefix = "EXP" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        var orderExpiringSoon = "DO-SOON-" + prefix;
        var orderActive = "DO-ACTIVE-" + prefix;
        var orderFullyDelivered = "DO-DONE-" + prefix;

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == prefix))
                db.LineOperators.Add(LineOperator.Create(prefix, $"{prefix} Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 12").Value!);

            // 1. Sắp hết hạn (hạn sau 2 ngày), chưa giao đủ
            var do1 = DeliveryOrder.Create(orderExpiringSoon, prefix, taxCode, "Customer 12", DateTime.UtcNow.AddDays(2), "VESSEL 1", tenantId).Value!;
            do1.AddItem(ContainerSize.Ft20, ContainerType.Dry, 3);

            // 2. Còn hạn dài (hạn sau 15 ngày), chưa giao đủ
            var do2 = DeliveryOrder.Create(orderActive, prefix, taxCode, "Customer 12", DateTime.UtcNow.AddDays(15), "VESSEL 2", tenantId).Value!;
            do2.AddItem(ContainerSize.Ft20, ContainerType.Dry, 3);

            // 3. Hạn sau 2 ngày nhưng đã hoàn tất / giao đủ
            var do3 = DeliveryOrder.Create(orderFullyDelivered, prefix, taxCode, "Customer 12", DateTime.UtcNow.AddDays(2), "VESSEL 3", tenantId).Value!;
            do3.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);
            do3.Items.First().Allocate(2);
            do3.MarkCompleted();

            db.DeliveryOrders.AddRange(do1, do2, do3);
            await db.SaveChangesAsync(ct);
        }

        // Act: Query with status=expiring
        var response = await client.GetAsync($"/api/delivery-orders?lineOperator={prefix}&status=expiring", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var items = json.GetProperty("items").EnumerateArray().ToList();

        // Assert: Chỉ có orderExpiringSoon
        Assert.Contains(items, item => item.GetProperty("orderNumber").GetString() == orderExpiringSoon);
        Assert.DoesNotContain(items, item => item.GetProperty("orderNumber").GetString() == orderActive);
        Assert.DoesNotContain(items, item => item.GetProperty("orderNumber").GetString() == orderFullyDelivered);
    }
}
