using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class DashboardTests(ApiFactory factory)
{
    [Fact]
    public async Task GetLookups_ReturnsAllEnumsWithVietnameseLabels()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var response = await client.GetAsync("/api/lookups", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.True(data.TryGetProperty("containerSizes", out var sizes));
        Assert.True(data.TryGetProperty("containerTypes", out var types));
        Assert.True(data.TryGetProperty("containerConditions", out var conditions));
        Assert.True(data.TryGetProperty("classifications", out var classifications));
        Assert.True(data.TryGetProperty("vehicleTypes", out var vehicles));

        Assert.Equal("20 feet", sizes.GetProperty("Ft20").GetString());
        Assert.Equal("40 feet", sizes.GetProperty("Ft40").GetString());
        Assert.Equal("Container khô", types.GetProperty("Dry").GetString());
        Assert.Equal("Container lạnh", types.GetProperty("Reefer").GetString());
        Assert.Equal("Bình thường", conditions.GetProperty("Normal").GetString());
        Assert.Equal("Loại A", classifications.GetProperty("A").GetString());
        Assert.Equal("Xe tải", vehicles.GetProperty("Truck").GetString());
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsSummaryStats()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var response = await client.GetAsync("/api/reports/summary", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.True(data.TryGetProperty("totalContainersInYard", out var totalConts));
        Assert.True(data.TryGetProperty("totalTeuInYard", out var totalTeu));
        Assert.True(data.TryGetProperty("activeOrdersCount", out var activeOrders));
        Assert.True(data.TryGetProperty("todayGateTransactionsCount", out var todayTx));

        Assert.True(totalConts.GetInt32() >= 0);
        Assert.True(totalTeu.GetInt32() >= 0);
        Assert.True(activeOrders.GetInt32() >= 0);
        Assert.True(todayTx.GetInt32() >= 0);
    }

    [Fact]
    public async Task GetGateTraffic_ReturnsDaysStats()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var response = await client.GetAsync("/api/reports/gate-traffic?days=7", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<JsonElement[]>(ct);
        Assert.NotNull(list);
        Assert.Equal(7, list.Length);

        var item = list[0];
        Assert.True(item.TryGetProperty("date", out var dateProp));
        Assert.True(item.TryGetProperty("gateInCount", out var gateInProp));
        Assert.True(item.TryGetProperty("gateOutCount", out var gateOutProp));
        Assert.True(item.TryGetProperty("totalCount", out var totalProp));
        Assert.Equal(gateInProp.GetInt32() + gateOutProp.GetInt32(), totalProp.GetInt32());
    }

    [Fact]
    public async Task GetBlockCapacity_ReturnsBlocksCapacity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var response = await client.GetAsync("/api/reports/block-capacity", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<JsonElement[]>(ct);
        Assert.NotNull(list);

        if (list.Length > 0)
        {
            var item = list[0];
            Assert.True(item.TryGetProperty("blockId", out _));
            Assert.True(item.TryGetProperty("blockCode", out _));
            Assert.True(item.TryGetProperty("totalCapacityTeu", out _));
            Assert.True(item.TryGetProperty("occupiedTeu", out _));
            Assert.True(item.TryGetProperty("occupancyRate", out _));
            Assert.True(item.TryGetProperty("containersCount", out _));
        }
    }

    [Fact]
    public async Task Customer_Deactivate_FailsWhenActiveOrders_SucceedsWhenNoActiveOrders()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var taxCode = "01" + Random.Shared.Next(10000000, 99999999).ToString();
        // 1. Create customer
        var createResp = await client.PostAsJsonAsync("/api/customers", new
        {
            TaxCode = taxCode,
            Name = "Customer Test Active Orders"
        }, ct);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var custObj = await createResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var custId = custObj.GetProperty("id").GetGuid();

        // 2. Create active delivery order
        var orderNum = "DO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await using (var db = factory.Context("DEPOT-A"))
        {
            var order = DeliveryOrder.Create(orderNum, "OP1", taxCode, "Customer Test Active Orders", DateTime.UtcNow.AddDays(5), "VESSEL1", "DEPOT-A").Value!;
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);
        }

        // 3. Deactivate should return 409 Conflict
        var deactResp = await client.PostAsync($"/api/customers/{custId}/deactivate", null, ct);
        Assert.Equal(HttpStatusCode.Conflict, deactResp.StatusCode);
        var err = await deactResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("Customer.HasActiveOrders", err.GetProperty("code").GetString());

        // 4. Create another customer without orders
        var taxCode2 = "02" + Random.Shared.Next(10000000, 99999999).ToString();
        var createResp2 = await client.PostAsJsonAsync("/api/customers", new
        {
            TaxCode = taxCode2,
            Name = "Clean Customer"
        }, ct);
        Assert.Equal(HttpStatusCode.Created, createResp2.StatusCode);
        var custId2 = (await createResp2.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();

        // 5. Update name
        var updateResp = await client.PutAsJsonAsync($"/api/customers/{custId2}", new UpdateCustomerRequest("Clean Customer Updated"), ct);
        Assert.True(updateResp.IsSuccessStatusCode);

        // 6. Deactivate succeeds
        var deactResp2 = await client.PostAsync($"/api/customers/{custId2}/deactivate", null, ct);
        Assert.True(deactResp2.IsSuccessStatusCode);

        // 7. Activate succeeds
        var actResp = await client.PostAsync($"/api/customers/{custId2}/activate", null, ct);
        Assert.True(actResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task LineOperator_Deactivate_FailsWhenContainersInYard_SucceedsWhenClean()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var opCode = "O" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        // 1. Create operator
        var createResp = await client.PostAsJsonAsync("/api/line-operators", new
        {
            Code = opCode,
            Name = "Operator Test Conts"
        }, ct);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var opObj = await createResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var opId = opObj.GetProperty("id").GetGuid();

        // 2. Put container in yard for this operator
        await using (var db = factory.Context("DEPOT-A"))
        {
            var cont = OperationScenario.NewContainer(opCode, "DEPOT-A");
            db.Containers.Add(cont);
            var lc = ContainerLifecycle.GateIn(cont.Id, opCode, Classification.A, ContainerCondition.Normal, "TRUCK", null, DateTime.UtcNow, "DEPOT-A");
            db.ContainerLifecycles.Add(lc);
            await db.SaveChangesAsync(ct);
        }

        // 3. Deactivate should return 409 Conflict
        var deactResp = await client.PostAsync($"/api/line-operators/{opId}/deactivate", null, ct);
        Assert.Equal(HttpStatusCode.Conflict, deactResp.StatusCode);
        var err = await deactResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("LineOperator.HasActiveResources", err.GetProperty("code").GetString());

        // 4. Create another clean operator
        var opCode2 = "C" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var createResp2 = await client.PostAsJsonAsync("/api/line-operators", new
        {
            Code = opCode2,
            Name = "Clean Operator"
        }, ct);
        Assert.Equal(HttpStatusCode.Created, createResp2.StatusCode);
        var opId2 = (await createResp2.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();

        // 5. Update name
        var updateResp = await client.PutAsJsonAsync($"/api/line-operators/{opId2}", new UpdateLineOperatorRequest("Clean Operator Updated"), ct);
        Assert.True(updateResp.IsSuccessStatusCode);

        // 6. Deactivate succeeds
        var deactResp2 = await client.PostAsync($"/api/line-operators/{opId2}/deactivate", null, ct);
        Assert.True(deactResp2.IsSuccessStatusCode);

        // 7. Activate succeeds
        var actResp = await client.PostAsync($"/api/line-operators/{opId2}/activate", null, ct);
        Assert.True(actResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetCustomersAndOperators_ReturnsEnrichedFields()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var custResp = await client.GetAsync("/api/customers?pageIndex=1&pageSize=10", ct);
        Assert.Equal(HttpStatusCode.OK, custResp.StatusCode);
        var custData = await custResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var custItems = custData.GetProperty("items").EnumerateArray().ToList();
        if (custItems.Count > 0)
        {
            var first = custItems[0];
            Assert.True(first.TryGetProperty("isActive", out _));
            Assert.True(first.TryGetProperty("orderCount", out _));
            Assert.True(first.TryGetProperty("activeOrderCount", out _));
        }

        var opResp = await client.GetAsync("/api/line-operators?pageIndex=1&pageSize=10", ct);
        Assert.Equal(HttpStatusCode.OK, opResp.StatusCode);
        var opData = await opResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var opItems = opData.GetProperty("items").EnumerateArray().ToList();
        if (opItems.Count > 0)
        {
            var first = opItems[0];
            Assert.True(first.TryGetProperty("isActive", out _));
            Assert.True(first.TryGetProperty("containersInYard", out _));
            Assert.True(first.TryGetProperty("orderCount", out _));
            Assert.True(first.TryGetProperty("activeOrderCount", out _));
        }
    }
}
