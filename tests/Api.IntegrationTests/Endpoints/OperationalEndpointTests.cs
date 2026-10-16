using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class OperationalEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task RegisteredContainerLocationResolvesToTheCreatedResource()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var number = OperationScenario.NewContainer().ContainerNumber.Value;
        var created = await client.PostAsJsonAsync("/api/containers", new
        { ContainerNumber = number, IsoCode = "22G1", Size = 20, Type = 85, TareWeight = 2000, MaxGrossWeight = 30000, OwnerCode = "MSK" }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        using var resource = await client.GetAsync(created.Headers.Location, ct);
        Assert.Equal(HttpStatusCode.OK, resource.StatusCode);
        Assert.Equal(number, (await resource.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("containerNumber").GetString());
    }

    [Theory]
    [InlineData(999, 85)]
    [InlineData(20, 999)]
    public async Task UndefinedDeliveryOrderItemEnumsAreRejected(int size, int type)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.PostAsJsonAsync("/api/delivery-orders", new
        {
            OrderNumber = "INVALID" + Guid.NewGuid().ToString("N")[..8], LineOperator = "MSK",
            CustomerTaxCode = "0101234567", CustomerName = "Customer", OutboundVessel = "Vessel",
            ExpiryDate = DateTime.UtcNow.AddDays(7), Items = new[] { new { Size = size, Type = type, Quantity = 1 } }
        }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminCanCreateBlockViewLayoutAndFindPagedFreeSlots()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var created = await client.PostAsJsonAsync("/api/yard/blocks", new
            { Code = "Y" + Guid.NewGuid().ToString("N")[..6], Name = "HTTP yard", IsVirtual = false, Bays = 5, Rows = 1, Tiers = 2 }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        var layout = await client.GetAsync($"/api/yard/blocks/{id}/layout", ct);
        Assert.Equal(HttpStatusCode.OK, layout.StatusCode);
        var free = await client.GetAsync($"/api/yard/free-slots?blockId={id}&size=40&pageSize=1", ct);
        Assert.Equal(HttpStatusCode.OK, free.StatusCode);
        var page = await free.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task DeliveryOrderCanBeRetrievedByIdAndOrderNumber()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var number = "DO" + Guid.NewGuid().ToString("N")[..8];
        var created = await client.PostAsJsonAsync("/api/delivery-orders", new
        {
            OrderNumber = number, LineOperator = "MSK", CustomerTaxCode = "0101234567", CustomerName = "Customer",
            OutboundVessel = "Vessel", ExpiryDate = DateTime.UtcNow.AddDays(7),
            Items = new[] { new { Size = 20, Type = 85, Quantity = 3 } }
        }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        var byId = await client.GetAsync($"/api/delivery-orders/{id}", ct);
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        var byNumber = await client.GetAsync($"/api/delivery-orders/by-number/{number}", ct);
        Assert.Equal(HttpStatusCode.OK, byNumber.StatusCode);
    }
}
