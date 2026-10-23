using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class HistoryReportTests(ApiFactory factory)
{
    [Fact]
    public async Task LegacyVisitClosedAfterRolloutHasOnlyOneGateOut()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = factory.Context("DEPOT-A");
        var container = OperationScenario.NewContainer();
        db.Containers.Add(container);
        var now = DateTime.UtcNow;
        var lifecycle = ContainerLifecycle.GateIn(container.Id, "MSK", Classification.A, ContainerCondition.Normal, "IN", Guid.NewGuid(), now.AddDays(-2), "DEPOT-A");
        db.ContainerLifecycles.Add(lifecycle);
        await db.SaveChangesAsync(ct);
        await db.ContainerLifecycles.Where(l => l.Id == lifecycle.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.MovementHistoryVersion, 0), ct);
        await db.Entry(lifecycle).ReloadAsync(ct);
        lifecycle.GateOut("OUT", Guid.NewGuid(), now);
        db.ContainerMovements.Add(ContainerMovement.Create(container.Id, lifecycle.Id, ContainerMovementType.GateOut, now, null, null, null, "DEPOT-A", "OUT", VehicleType.Truck, lifecycle.DeliveryOrderId).Value!);
        await db.SaveChangesAsync(ct);
        using var client = await ApiTestClient.LoginAsync(factory);
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/reports/containers/{container.ContainerNumber.Value}/history", ct);
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        var gateOut = Assert.Single(page.GetProperty("items").EnumerateArray(), r => r.GetProperty("eventType").GetString() == "GateOut");
        Assert.Equal("Movement", gateOut.GetProperty("source").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyTimelineKeepsRealGateEventsAlongsideNewMovesAndPagesStably(bool closedLegacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = factory.Context("DEPOT-A");
        var container = OperationScenario.NewContainer(); db.Containers.Add(container);
        var time = DateTime.UtcNow.AddDays(-5);
        var legacy = ContainerLifecycle.GateIn(container.Id, "MSK", Classification.A, ContainerCondition.Normal, "OLD-IN", Guid.NewGuid(), time, "DEPOT-A");
        legacy.SetCurrentSlot(null);
        if (closedLegacy) legacy.GateOut("OLD-OUT", Guid.NewGuid(), time.AddDays(2));
        db.ContainerLifecycles.Add(legacy);
        await db.SaveChangesAsync(ct);
        await db.ContainerLifecycles.Where(l => l.Id == legacy.Id).ExecuteUpdateAsync(set => set.SetProperty(l => l.MovementHistoryVersion, 0), ct);
        var position = new MovementPosition(Guid.NewGuid(), "OLD-BLOCK", 1, 1, 1);
        db.ContainerMovements.Add(ContainerMovement.Create(container.Id, legacy.Id, ContainerMovementType.Remove, time.AddDays(1), null, position, null, "DEPOT-A").Value!);
        db.ContainerMovements.Add(ContainerMovement.Create(container.Id, legacy.Id, ContainerMovementType.Place, time.AddDays(1), null, null, position, "DEPOT-A").Value!);
        if (closedLegacy)
        {
            var current = ContainerLifecycle.GateIn(container.Id, "MSK", Classification.A, ContainerCondition.Normal, "NEW-IN", Guid.NewGuid(), time.AddDays(3), "DEPOT-A");
            db.ContainerLifecycles.Add(current);
            db.ContainerMovements.Add(ContainerMovement.Create(container.Id, current.Id, ContainerMovementType.GateIn, time.AddDays(3), null, null, position, "DEPOT-A", "NEW-IN", VehicleType.Truck).Value!);
        }
        await db.SaveChangesAsync(ct);
        using var client = await ApiTestClient.LoginAsync(factory);
        var route = $"/api/reports/containers/{container.ContainerNumber.Value}/history";
        var response = await client.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var expected = closedLegacy ? 5 : 3;
        Assert.Equal(expected, page.GetProperty("totalCount").GetInt32());
        var rows = page.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(closedLegacy ? 2 : 1, rows.Count(r => r.GetProperty("source").GetString() == "LegacyLifecycle"));
        Assert.Equal(closedLegacy ? 3 : 2, rows.Count(r => r.GetProperty("source").GetString() == "Movement"));
        Assert.Contains(rows, r => r.GetProperty("vehicleNumber").GetString() == "OLD-IN");
        var ids = rows.Select(r => r.GetProperty("eventId").GetString()).ToArray();
        Assert.Equal(expected, ids.Distinct().Count());
        for (var i = 1; i <= expected; i++)
        {
            var single = await client.GetFromJsonAsync<JsonElement>($"{route}?pageIndex={i}&pageSize=1", ct);
            Assert.Equal(ids[i - 1], single.GetProperty("items")[0].GetProperty("eventId").GetString());
        }
    }

    [Fact]
    public async Task MissingContainerReturns404AndRegisteredWithoutVisitsReturnsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var container = OperationScenario.NewContainer();
        var route = $"/api/reports/containers/{container.ContainerNumber.Value}/history";
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route, ct)).StatusCode);
        await using var db = factory.Context("DEPOT-A"); db.Containers.Add(container); await db.SaveChangesAsync(ct);
        var response = await client.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("totalCount").GetInt32());
    }
}
