using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class ContainerFilterTests(ApiFactory factory)
{
    [Fact]
    public async Task FiltersApplyBeforePagination_AndSummaryIgnoresOnlyType()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = "FILTER" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        var dry = OperationScenario.NewContainer("QZX");
        var reeferNumber = OperationScenario.NewContainer("QZX").ContainerNumber;
        var reefer = Container.Create(reeferNumber, "22R1", ContainerSize.Ft20, ContainerType.Reefer, 2000, 30000, null,
            Classification.A, ContainerCondition.Normal, "DEPOT-A").Value!;
        var large = OperationScenario.NewContainer("QZX", size: ContainerSize.Ft40);
        await using (var db = factory.Context("DEPOT-A"))
        {
            var op = LineOperator.Create(code, "Filter operator").Value!;
            db.LineOperators.Add(op);
            db.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(op.Id, "QZX", "DEPOT-A"));
            db.Containers.AddRange(dry, reefer, large, OperationScenario.NewContainer("QZX", "OTHER-FILTER-TENANT"));
            var order = DeliveryOrder.Create("FILTER-" + Guid.NewGuid().ToString("N")[..8], code, "0101234567", "Customer",
                DateTime.UtcNow.AddDays(7), "Vessel", "DEPOT-A").Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 5);
            db.DeliveryOrders.Add(order);
            db.Bookings.Add(DeliveryOrderBooking.Reserve(order.Id, order.Items.Single().Id, dry.Id, "DEPOT-A", DateTime.UtcNow));
            foreach (var container in new[] { dry, reefer })
                db.ContainerLifecycles.Add(ContainerLifecycle.GateIn(container.Id, code, Classification.A,
                    ContainerCondition.Normal, "TRUCK", null, DateTime.UtcNow, "DEPOT-A"));
            await db.SaveChangesAsync(ct);
        }
        using var client = await ApiTestClient.LoginAsync(factory);
        var root = $"/api/containers?lineOperator={code}";
        var filtered = await ReadAsync(client, root + "&type=Reefer&pageSize=1");
        Assert.Equal(1, filtered.GetProperty("totalCount").GetInt32());
        Assert.Equal(reefer.ContainerNumber.Value, filtered.GetProperty("items")[0].GetProperty("containerNumber").GetString());
        var empty = await ReadAsync(client, root + "&type=Reefer&pageSize=1&pageIndex=2");
        Assert.Equal(1, empty.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, empty.GetProperty("items").GetArrayLength());
        var summary = await ReadAsync(client, $"/api/containers/summary?lineOperator={code}&type=Dry");
        Assert.Equal(3, summary.GetProperty("total").GetInt32());
        Assert.Equal(1, summary.GetProperty("byType").GetProperty("Reefer").GetInt32());
        var booked = await ReadAsync(client, root + "&booking=assigned");
        Assert.Equal(1, booked.GetProperty("totalCount").GetInt32());
        Assert.Equal(dry.ContainerNumber.Value, booked.GetProperty("items")[0].GetProperty("containerNumber").GetString());
        var inYard = await ReadAsync(client, root + "&booking=unassigned&displayStatus=InYard");
        Assert.Equal(1, inYard.GetProperty("totalCount").GetInt32());
        var sorted = await ReadAsync(client, root + "&sort=size&descending=true&pageSize=1");
        Assert.Equal(3, sorted.GetProperty("totalCount").GetInt32());
        Assert.Equal(large.ContainerNumber.Value, sorted.GetProperty("items")[0].GetProperty("containerNumber").GetString());
    }

    [Theory]
    [InlineData("sort", "unknown")]
    [InlineData("type", "NotAContainerType")]
    [InlineData("booking", "unknown")]
    [InlineData("displayStatus", "unknown")]
    public async Task InvalidSortOrEnum_Returns400(string key, string value)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        using var response = await client.GetAsync($"/api/containers?{key}={value}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<JsonElement> ReadAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }
}
