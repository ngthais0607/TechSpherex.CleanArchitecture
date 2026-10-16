using System.Net;
using System.Net.Http.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class AuthorizationMatrixTests(ApiFactory factory)
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("YardPlanner")]
    [InlineData("OrderClerk")]
    [InlineData("GateClerk")]
    public async Task OperationalRoutesEnforceRoleMatrixWithValidRequests(string role)
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await ApiTestClient.LoginAsync(factory);
        var user = "matrix" + Guid.NewGuid().ToString("N")[..8];
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/auth/register", new
        { UserName = user, Email = user + "@example.test", FirstName = "Matrix", LastName = "Test", Password = ApiFactory.AdminPassword, Role = role }, ct)).StatusCode);
        using var client = await ApiTestClient.LoginAsync(factory, user);
        var s = await OperationScenario.CreateAsync(factory);
        var yard = role is "Admin" or "YardPlanner";
        var gate = role is "Admin" or "GateClerk";
        var orders = role is "Admin" or "OrderClerk";
        async Task Post(string route, object body, bool allowed, HttpStatusCode success = HttpStatusCode.NoContent)
        {
            using var response = await client.PostAsJsonAsync(route, body, ct);
            Assert.True(response.StatusCode == (allowed ? success : HttpStatusCode.Forbidden),
                $"{route}: expected {(allowed ? success : HttpStatusCode.Forbidden)}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
        }
        async Task Get(string route, bool allowed)
            => Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync(route, ct)).StatusCode);

        await Get("/api/containers", true);
        await Get($"/api/containers/by-number/{s.First.ContainerNumber.Value}", true);
        await Get($"/api/yard/blocks/{s.Block.Id}/layout", role != "OrderClerk");
        await Get($"/api/yard/free-slots?blockId={s.Block.Id}&size=20", role != "OrderClerk");
        await Get("/api/delivery-orders", role != "YardPlanner");
        await Get($"/api/delivery-orders/{s.Order.Id}", role != "YardPlanner");
        await Get($"/api/delivery-orders/by-number/{s.Order.OrderNumber}", role != "YardPlanner");
        await Post("/api/yard/blocks", new { Code = "M" + Guid.NewGuid().ToString("N")[..6], Name = "Matrix", Bays = 3, Rows = 1, Tiers = 2, IsVirtual = false }, yard, HttpStatusCode.Created);
        await Post($"/api/yard/blocks/{s.Block.Id}/expand", new { NewBays = 7, NewRows = 1, NewTiers = 2 }, yard);
        await Post($"/api/yard/blocks/{s.Block.Id}/shrink", new { NewBays = 5, NewRows = 1, NewTiers = 2 }, yard);
        await Post("/api/yard/placements", new { ContainerId = s.Second.Id, BlockId = s.Block.Id, Bay = 3, Row = 1, Tier = 1 }, yard);
        // Admin arranges the preconditions when the tested role cannot manage the yard.
        if (!yard) Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync("/api/yard/placements", new { ContainerId = s.Second.Id, BlockId = s.Block.Id, Bay = 3, Row = 1, Tier = 1 }, ct)).StatusCode);
        await Post("/api/yard/removals", new { BlockId = s.Block.Id, Bay = 3, Row = 1, Tier = 1 }, yard);
        if (!yard) Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync("/api/yard/removals", new { BlockId = s.Block.Id, Bay = 3, Row = 1, Tier = 1 }, ct)).StatusCode);
        await Post("/api/yard/relocations", new { BlockId = s.Block.Id, SourceBay = 1, SourceRow = 1, SourceTier = 1, TargetBay = 3, TargetRow = 1, TargetTier = 1 }, yard);
        await Post($"/api/containers/{s.Second.Id}/damage", new { Condition = 1 }, yard);
        var fresh = OperationScenario.NewContainer();
        await Post("/api/containers", new { ContainerNumber = fresh.ContainerNumber.Value, IsoCode = "22G1", Size = 20, Type = 85, TareWeight = 2000, MaxGrossWeight = 30000, OwnerCode = "MSK" }, gate, HttpStatusCode.Created);
        await Post("/api/delivery-orders", new { OrderNumber = "M" + Guid.NewGuid().ToString("N")[..8], LineOperator = "MSK", CustomerTaxCode = "0101234567", CustomerName = "Customer", OutboundVessel = "Vessel", ExpiryDate = DateTime.UtcNow.AddDays(7), Items = new[] { new { Size = 20, Type = 85, Quantity = 1 } } }, orders, HttpStatusCode.Created);
        var slot = s.Block.Slots.First(x => x.Bay == 5 && x.Tier == 1);
        await Post("/api/gate/in", new { ContainerId = s.Third.Id, InVehicleNumber = "TRUCK", SlotId = slot.Id }, gate, HttpStatusCode.Created);
        await Post("/api/gate/out", new { ContainerId = s.First.Id, DeliveryOrderId = s.Order.Id, OutVehicleNumber = "TRUCK" }, gate);
    }
}
