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
/// Kịch bản 13: Container có Booking Reserved có displayStatus là "Reserved" (Chờ xuất);
/// Container ở block ảo là "VirtualBlock" (Chờ phân vị trí); sau khi xuất là "OutOfYard" (Đã xuất).
/// </summary>
[Collection("Api")]
public sealed class Scenario13_ContainerDisplayStatusTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario13_ContainerDisplayStatus_ReturnsCorrectValues()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var cReserved = OperationScenario.NewContainer("MSC", tenantId);
        var cVirtual = OperationScenario.NewContainer("MSC", tenantId);
        var cOut = OperationScenario.NewContainer("MSC", tenantId);

        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        var vbCode = "VB-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        var orderNumber = "DO-" + Guid.NewGuid().ToString("N")[..8];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 13").Value!);

            var stdBlock = YardBlock.CreateStandard(blockCode, "Standard Block", 3, 1, 1, tenantId);
            var virtBlock = YardBlock.CreateVirtual(vbCode, "Virtual Block", tenantId);

            var slot = stdBlock.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(cReserved.Id);

            // 1. cReserved: InYard + Booking Reserved
            var lcReserved = ContainerLifecycle.GateIn(cReserved.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-11111", slot.Id, DateTime.UtcNow, tenantId);
            var order = DeliveryOrder.Create(orderNumber, "MSC", taxCode, "Customer 13", DateTime.UtcNow.AddDays(7), "VESSEL 13", tenantId).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

            // 2. cVirtual: InYard ở Block ảo
            var lcVirtual = ContainerLifecycle.GateIn(cVirtual.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-22222", null, DateTime.UtcNow, tenantId, virtualBlockId: virtBlock.Id);

            // 3. cOut: GatedOut
            var lcOut = ContainerLifecycle.GateIn(cOut.Id, "MSC", Classification.A, ContainerCondition.Normal, "29C-33333", null, DateTime.UtcNow.AddDays(-2), tenantId);
            lcOut.GateOut("51D-99999", order.Id, DateTime.UtcNow.AddDays(-1));

            db.YardBlocks.AddRange(stdBlock, virtBlock);
            db.Containers.AddRange(cReserved, cVirtual, cOut);
            db.ContainerLifecycles.AddRange(lcReserved, lcVirtual, lcOut);
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);

            var booking = DeliveryOrderBooking.Reserve(order.Id, order.Items.First().Id, cReserved.Id, tenantId, DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        // Act & Assert 1: cReserved -> "Reserved"
        var res1 = await client.GetAsync($"/api/containers/by-number/{cReserved.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var json1 = await res1.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("Reserved", json1.GetProperty("displayStatus").GetString());

        // Act & Assert 2: cVirtual -> "VirtualBlock"
        var res2 = await client.GetAsync($"/api/containers/by-number/{cVirtual.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var json2 = await res2.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("VirtualBlock", json2.GetProperty("displayStatus").GetString());

        // Act & Assert 3: cOut -> "OutOfYard"
        var res3 = await client.GetAsync($"/api/containers/by-number/{cOut.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
        var json3 = await res3.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("OutOfYard", json3.GetProperty("displayStatus").GetString());
    }
}
