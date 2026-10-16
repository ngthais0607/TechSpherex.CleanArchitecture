using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests;

[Collection("Api")]
public sealed class FullFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task RolesCompleteIntakeYardMovesDispatchQuotaAndReportsThroughHttp()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await ApiTestClient.LoginAsync(factory);
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var owner = "F" + suffix;
        async Task<JsonElement> Post(HttpClient client, string route, object body, HttpStatusCode status)
        {
            using var response = await client.PostAsJsonAsync(route, body, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            Assert.True(response.StatusCode == status, $"{route}: expected {status}, got {response.StatusCode}: {text}");
            return string.IsNullOrEmpty(text) ? default : JsonSerializer.Deserialize<JsonElement>(text);
        }
        await Post(admin, "/api/line-operators", new { Code = owner, Name = "Flow operator" }, HttpStatusCode.Created);
        await Post(admin, "/api/customers", new { TaxCode = Random.Shared.Next(1000000000, int.MaxValue).ToString(), Name = "Flow customer" }, HttpStatusCode.Created);
        await Post(admin, "/api/voyages", new { VesselName = "Flow vessel", VoyageNumber = suffix, Etd = DateTime.UtcNow.AddDays(5) }, HttpStatusCode.Created);
        await Post(admin, "/api/vehicles", new { PlateNumber = "51C" + suffix, Type = 0 }, HttpStatusCode.Created);
        var clients = new Dictionary<string, HttpClient>();
        try
        {
            foreach (var role in new[] { "YardPlanner", "OrderClerk", "GateClerk" })
            {
                var name = role + suffix;
                await Post(admin, "/api/auth/register", new { UserName = name, Email = name + "@example.test", FirstName = "Flow", LastName = role, Password = ApiFactory.AdminPassword, Role = role }, HttpStatusCode.Created);
                clients[role] = await ApiTestClient.LoginAsync(factory, name);
            }
            var yard = clients["YardPlanner"]; var orderClient = clients["OrderClerk"]; var gate = clients["GateClerk"];
            var block = await Post(admin, "/api/yard/blocks", new { Code = owner, Name = "Flow block", Bays = 7, Rows = 1, Tiers = 2, IsVirtual = false }, HttpStatusCode.Created);
            var blockId = block.GetProperty("id").GetGuid();
            var layout = await admin.GetFromJsonAsync<JsonElement>($"/api/yard/blocks/{blockId}/layout", ct);
            var order = await Post(orderClient, "/api/delivery-orders", new { OrderNumber = owner, LineOperator = owner, CustomerTaxCode = "0101234567", CustomerName = "Flow customer", OutboundVessel = "Flow vessel", ExpiryDate = DateTime.UtcNow.AddDays(7), Items = new[] { new { Size = 20, Type = 85, Quantity = 3 } } }, HttpStatusCode.Created);
            var orderId = order.GetProperty("id").GetGuid();
            var ids = new List<Guid>(); var numbers = new List<string>();
            for (var i = 0; i < 4; i++)
            {
                var number = OperationScenario.NewContainer().ContainerNumber.Value; numbers.Add(number);
                var registered = await Post(gate, "/api/containers", new { ContainerNumber = number, IsoCode = "22G1", Size = 20, Type = 85, TareWeight = 2000, MaxGrossWeight = 30000, OwnerCode = owner }, HttpStatusCode.Created);
                var id = registered.GetProperty("id").GetGuid(); ids.Add(id);
                var slotId = layout.GetProperty("slots").EnumerateArray().Single(s => s.GetProperty("bay").GetInt32() == i * 2 + 1 && s.GetProperty("tier").GetInt32() == 1).GetProperty("id").GetGuid();
                await Post(gate, "/api/gate/in", new { ContainerId = id, InVehicleNumber = "FLOW-TRUCK", SlotId = slotId, VehicleType = 0 }, HttpStatusCode.Created);
            }
            async Task<int> Inventory() => (await admin.GetFromJsonAsync<JsonElement>($"/api/reports/yard-inventory?lineOperator={owner}", ct)).GetProperty("totalContainers").GetInt32();
            async Task<int> Remaining() => (await orderClient.GetFromJsonAsync<JsonElement>($"/api/delivery-orders/{orderId}", ct)).GetProperty("items")[0].GetProperty("remainingQuantity").GetInt32();
            Assert.Equal(4, await Inventory()); Assert.Equal(3, await Remaining());
            await Post(yard, "/api/yard/removals", new { BlockId = blockId, Bay = 1, Row = 1, Tier = 1 }, HttpStatusCode.NoContent);
            Assert.Equal(4, await Inventory());
            await Post(yard, "/api/yard/placements", new { ContainerId = ids[0], BlockId = blockId, Bay = 1, Row = 1, Tier = 1 }, HttpStatusCode.NoContent);
            await Post(yard, "/api/yard/relocations", new { BlockId = blockId, SourceBay = 1, SourceRow = 1, SourceTier = 1, TargetBay = 3, TargetRow = 1, TargetTier = 2 }, HttpStatusCode.NoContent);
            for (var i = 0; i < 3; i++)
            {
                await Post(gate, "/api/gate/out", new { ContainerId = ids[i], DeliveryOrderId = orderId, OutVehicleNumber = "FLOW-OUT", VehicleType = 0 }, HttpStatusCode.NoContent);
                Assert.Equal(2 - i, await Remaining());
            }
            var quota = await Post(gate, "/api/gate/out", new { ContainerId = ids[3], DeliveryOrderId = orderId, OutVehicleNumber = "FLOW-OUT" }, HttpStatusCode.UnprocessableEntity);
            Assert.Equal("OrderRule.NoRemainingQuantity", quota.GetProperty("code").GetString());
            await Post(gate, "/api/gate/out", new { ContainerId = ids[0], DeliveryOrderId = orderId, OutVehicleNumber = "FLOW-OUT" }, HttpStatusCode.Conflict);
            Assert.Equal(1, await Inventory());
            var history = await yard.GetFromJsonAsync<JsonElement>($"/api/reports/containers/{numbers[0]}/history", ct);
            Assert.Equal(new[] { "GateIn", "Remove", "Place", "Relocate", "GateOut" }, history.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("eventType").GetString()));
            Assert.All(history.GetProperty("items").EnumerateArray(), e => Assert.Equal("Movement", e.GetProperty("source").GetString()));
            var dwell = await yard.GetFromJsonAsync<JsonElement>($"/api/reports/dwell-times?lineOperator={owner}&includeGatedOut=true", ct);
            Assert.Equal(4, dwell.GetProperty("totalCount").GetInt32());
            var expiry = await orderClient.GetFromJsonAsync<JsonElement>($"/api/reports/expiring-orders?lineOperator={owner}", ct);
            Assert.Equal(0, expiry.GetProperty("totalCount").GetInt32());
        }
        finally { foreach (var client in clients.Values) client.Dispose(); }
    }
}
