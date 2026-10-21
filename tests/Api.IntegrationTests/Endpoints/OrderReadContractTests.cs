using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class OrderReadContractTests(ApiFactory factory)
{
    [Fact]
    public async Task OrderClerk_ReadsBookingsAndReleasesRealId()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        var booking = DeliveryOrderBooking.Reserve(scenario.Order.Id, scenario.Order.Items.Single().Id,
            scenario.First.Id, "DEPOT-A", DateTime.UtcNow);
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
        }
        using var admin = await ApiTestClient.LoginAsync(factory);
        using var clerk = await RoleClientAsync(admin, "OrderClerk");
        using var response = await clerk.GetAsync($"/api/delivery-orders/{scenario.Order.OrderNumber}/bookings?pageSize=1", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var page = body.GetProperty("bookings");
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(booking.Id, page.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(1, body.GetProperty("itemCounts")[0].GetProperty("reservedQuantity").GetInt32());
        Assert.Equal(0, body.GetProperty("itemCounts")[0].GetProperty("availableQuantity").GetInt32());
        using var release = await clerk.DeleteAsync($"/api/delivery-orders/bookings/{booking.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, release.StatusCode);
        using var staleRelease = await clerk.DeleteAsync($"/api/delivery-orders/bookings/{booking.Id}", ct);
        Assert.Equal(HttpStatusCode.Conflict, staleRelease.StatusCode);
        using var emptyAssign = await clerk.PostAsJsonAsync($"/api/delivery-orders/{scenario.Order.OrderNumber}/assign-containers",
            new { containerNumbers = Array.Empty<string>() }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, emptyAssign.StatusCode);
        using var neverIn = await clerk.PostAsJsonAsync($"/api/delivery-orders/{scenario.Order.OrderNumber}/assign-containers",
            new { containerNumbers = new[] { scenario.Third.ContainerNumber.Value } }, ct);
        Assert.Equal(HttpStatusCode.Conflict, neverIn.StatusCode);
        var problem = await neverIn.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("Lifecycle.NotInYard", problem.GetProperty("code").GetString());
        using var yard = await RoleClientAsync(admin, "YardPlanner");
        using var denied = await yard.GetAsync($"/api/delivery-orders/{scenario.Order.OrderNumber}/bookings", ct);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task OrderSearchAndDetailStatus_AgreeWithList()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        await OperationScenario.CreateAsync(factory);
        using var client = await ApiTestClient.LoginAsync(factory);
        var list = await client.GetFromJsonAsync<JsonElement>($"/api/delivery-orders?search={scenario.Order.OrderNumber}&sort=orderNumber", ct);
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());
        Assert.Equal("Active", list.GetProperty("items")[0].GetProperty("status").GetString());
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/delivery-orders/{scenario.Order.Id}", ct);
        Assert.Equal("Active", detail.GetProperty("status").GetString());
        var byNumber = await client.GetFromJsonAsync<JsonElement>($"/api/delivery-orders/by-number/{scenario.Order.OrderNumber}", ct);
        Assert.Equal("Active", byNumber.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ForeignTenantBookingRead_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = DeliveryOrder.Create("FOREIGN-" + Guid.NewGuid().ToString("N"), "MSK", "0101234567", "Customer",
            DateTime.UtcNow.AddDays(7), "Vessel", "OTHER-DEPOT").Value!;
        await using (var db = factory.Context("OTHER-DEPOT"))
        {
            db.DeliveryOrders.Add(order);
            await db.SaveChangesAsync(ct);
        }
        using var client = await ApiTestClient.LoginAsync(factory);
        using var response = await client.GetAsync($"/api/delivery-orders/{order.OrderNumber}/bookings", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> RoleClientAsync(HttpClient admin, string role)
    {
        var name = "read" + Guid.NewGuid().ToString("N")[..10];
        using var response = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            userName = name, email = name + "@example.test", firstName = "Read", lastName = "Contract",
            password = ApiFactory.AdminPassword, role
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ApiTestClient.LoginAsync(factory, name);
    }
}
