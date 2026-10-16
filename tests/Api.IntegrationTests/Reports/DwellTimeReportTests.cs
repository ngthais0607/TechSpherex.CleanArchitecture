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
public sealed class DwellTimeReportTests(ApiFactory factory)
{
    [Fact]
    public async Task BusinessDatesCountTodayZeroAndReentrySeparatelyUsingOutDateForClosedVisit()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = "D" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var now = new DateTime(2026, 10, 15, 17, 30, 0, DateTimeKind.Utc); // Oct 16 00:30 in depot
        await using var db = factory.Context("DEPOT-A");
        var container = OperationScenario.NewContainer(owner);
        db.Containers.Add(container);
        var old = ContainerLifecycle.GateIn(container.Id, owner, Classification.A, ContainerCondition.Normal, "IN", Guid.NewGuid(), now.AddDays(-4), "DEPOT-A");
        old.GateOut("OUT", Guid.NewGuid(), now.AddDays(-2));
        db.ContainerLifecycles.Add(old);
        var active = ContainerLifecycle.GateIn(container.Id, owner, Classification.A, ContainerCondition.Normal, "IN", Guid.NewGuid(), now.AddMinutes(-20), "DEPOT-A");
        active.SetCurrentSlot(null);
        db.ContainerLifecycles.Add(active);
        var other = OperationScenario.NewContainer(owner, "DEPOT-B");
        db.Containers.Add(other);
        db.ContainerLifecycles.Add(ContainerLifecycle.GateIn(other.Id, owner, Classification.A, ContainerCondition.Normal, "IN", Guid.NewGuid(), now.AddDays(-10), "DEPOT-B"));
        await db.SaveChangesAsync(ct);
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        await using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new FixedClock(now)); }));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        var response = await client.GetAsync($"/api/reports/dwell-times?lineOperator={owner}&minDays=0&includeGatedOut=true", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        var rows = page.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(0, rows.Single(r => r.GetProperty("lifecycleId").GetGuid() == active.Id).GetProperty("daysInDepot").GetInt32());
        Assert.Equal(2, rows.Single(r => r.GetProperty("lifecycleId").GetGuid() == old.Id).GetProperty("daysInDepot").GetInt32());
        var activePage = await client.GetFromJsonAsync<JsonElement>($"/api/reports/dwell-times?lineOperator={owner}", ct);
        Assert.Equal(1, activePage.GetProperty("totalCount").GetInt32());
        var filtered = await client.GetFromJsonAsync<JsonElement>($"/api/reports/dwell-times?lineOperator={owner}&minDays=1&includeGatedOut=true", ct);
        Assert.Equal(1, filtered.GetProperty("totalCount").GetInt32());
    }

    internal sealed class FixedClock(DateTime utc) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(utc); }
}
