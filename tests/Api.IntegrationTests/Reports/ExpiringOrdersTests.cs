using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class ExpiringOrdersTests(ApiFactory factory)
{
    [Fact]
    public async Task LocalMidnightUsesSameValidityForExpiryReportAndGateOut()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = "E" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var now = new DateTime(2026, 10, 15, 17, 30, 0, DateTimeKind.Utc);
        await using var db = factory.Context("DEPOT-A");
        var orders = new List<DeliveryOrder>();
        foreach (var (day, empty, tenant) in new[] { (15, false, "DEPOT-A"), (16, false, "DEPOT-A"), (23, false, "DEPOT-A"), (24, false, "DEPOT-A"), (16, true, "DEPOT-A"), (16, false, "DEPOT-B") })
        {
            var order = DeliveryOrder.Create("E" + Guid.NewGuid().ToString("N")[..8], owner, "0101234567", "Customer", new DateTime(2026, 10, day, 12, 0, 0, DateTimeKind.Utc), "Vessel", tenant).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);
            if (empty) order.Items.Single().Allocate(1);
            db.DeliveryOrders.Add(order); orders.Add(order);
        }
        var container = OperationScenario.NewContainer(owner); db.Containers.Add(container);
        var lifecycle = ContainerLifecycle.GateIn(container.Id, owner, Classification.A, ContainerCondition.Normal, "IN", Guid.NewGuid(), now.AddDays(-1), "DEPOT-A");
        lifecycle.SetCurrentSlot(null); db.ContainerLifecycles.Add(lifecycle);
        await db.SaveChangesAsync(ct);
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        await using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new DwellTimeReportTests.FixedClock(now)); }));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        var response = await client.GetAsync($"/api/reports/expiring-orders?days=0&lineOperator={owner}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var today = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(1, today.GetProperty("totalCount").GetInt32());
        Assert.Equal(orders[1].Id, today.GetProperty("items")[0].GetProperty("id").GetGuid());
        var week = await client.GetFromJsonAsync<JsonElement>($"/api/reports/expiring-orders?days=7&lineOperator={owner}", ct);
        Assert.Equal(2, week.GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/reports/expiring-orders?days=366", ct)).StatusCode);
        var expired = await client.PostAsJsonAsync("/api/gate/out", new { ContainerId = container.Id, DeliveryOrderId = orders[0].Id, OutVehicleNumber = "OUT" }, ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, expired.StatusCode);
        var valid = await client.PostAsJsonAsync("/api/gate/out", new { ContainerId = container.Id, DeliveryOrderId = orders[1].Id, OutVehicleNumber = "OUT" }, ct);
        Assert.Equal(HttpStatusCode.NoContent, valid.StatusCode);
    }
}
