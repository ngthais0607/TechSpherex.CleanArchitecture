using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class ContainerSummaryTests(ApiFactory factory)
{
    [Fact]
    public async Task GetSummary_Returns200WithAccurateGroupings()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        using var client = await ApiTestClient.LoginAsync(factory);

        var response = await client.GetAsync("/api/containers/summary", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.True(json.GetProperty("total").GetInt32() >= 3);
        Assert.True(json.TryGetProperty("bySize", out var bySize));
        Assert.True(json.TryGetProperty("byType", out var byType));
        Assert.True(json.TryGetProperty("byCondition", out var byCondition));
    }

    [Fact]
    public async Task GetContainerOrders_Returns200WithHistoryAndSummary()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        using var client = await ApiTestClient.LoginAsync(factory);

        // Assign first container to order
        await using (var db = factory.Context("DEPOT-A"))
        {
            var booking = DeliveryOrderBooking.Reserve(
                scenario.Order.Id,
                scenario.Order.Items.First().Id,
                scenario.First.Id,
                "DEPOT-A",
                DateTime.UtcNow);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }

        var number = scenario.First.ContainerNumber.Value;
        var response = await client.GetAsync($"/api/containers/{number}/orders", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.True(json.TryGetProperty("currentOrder", out var currentOrder));
        Assert.False(currentOrder.ValueKind == JsonValueKind.Null);
        Assert.Equal(scenario.Order.OrderNumber, currentOrder.GetProperty("orderNumber").GetString());

        Assert.True(json.TryGetProperty("summary", out var summary));
        Assert.Equal("Customer", summary.GetProperty("lastCustomer").GetString());
    }

    [Fact]
    public async Task SearchContainers_ReturnsMatchedContainer()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        using var client = await ApiTestClient.LoginAsync(factory);

        var number = scenario.Second.ContainerNumber.Value;
        var subStr = number.Substring(4, 5);

        var response = await client.GetAsync($"/api/containers?search={subStr}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("containerNumber").GetString() == number);
    }
}
