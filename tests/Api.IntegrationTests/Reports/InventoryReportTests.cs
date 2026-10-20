using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class InventoryReportTests(ApiFactory factory)
{
    [Fact]
    public async Task ActiveLifecyclesCountFortyFootOnceAndIncludeUnplacedButExcludeOtherTenantAndGatedOut()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = "R" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        await using var db = factory.Context("DEPOT-A");
        var block = YardBlock.CreateStandard(owner, "Inventory", 5, 1, 2, "DEPOT-A");
        db.YardBlocks.Add(block);
        for (var i = 0; i < 5; i++)
        {
            var tenant = i == 4 ? "DEPOT-B" : "DEPOT-A";
            var container = OperationScenario.NewContainer(owner, tenant, i == 0 ? ContainerSize.Ft40 : ContainerSize.Ft20);
            db.Containers.Add(container);
            var slot = block.Slots.First(s => s.Bay == (i == 0 ? 1 : 5) && s.Tier == 1);
            var lifecycle = ContainerLifecycle.GateIn(container.Id, owner, Classification.A, i == 2 ? ContainerCondition.Damaged : ContainerCondition.Normal, "TRUCK", slot.Id, DateTime.UtcNow, tenant);
            if (i == 0) foreach (var x in block.Slots.Where(s => s.Tier == 1 && s.Bay < 5)) x.Occupy(container.Id);
            else if (i == 1) slot.Occupy(container.Id);
            else lifecycle.SetCurrentSlot(null);
            if (i == 3) lifecycle.GateOut("OUT", Guid.NewGuid(), DateTime.UtcNow);
            db.ContainerLifecycles.Add(lifecycle);
        }
        await db.SaveChangesAsync(ct);
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.GetAsync($"/api/reports/yard-inventory?lineOperator={owner}&pageSize=1", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(3, report.GetProperty("totalContainers").GetInt32());
        Assert.Equal(3, report.GetProperty("groups").GetProperty("totalCount").GetInt32());
        Assert.Single(report.GetProperty("groups").GetProperty("items").EnumerateArray());
        var all = await client.GetFromJsonAsync<JsonElement>($"/api/reports/yard-inventory?lineOperator={owner}", ct);
        var groups = all.GetProperty("groups").GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(1, groups.Single(g => g.GetProperty("blockId").ValueKind == JsonValueKind.Null).GetProperty("containerCount").GetInt32());
        Assert.Equal("Damaged", groups.Single(g => g.GetProperty("blockId").ValueKind == JsonValueKind.Null).GetProperty("condition").GetString());
        Assert.Equal(1, groups.Single(g => g.GetProperty("size").GetString() == "Ft40").GetProperty("containerCount").GetInt32());
        var filtered = await client.GetFromJsonAsync<JsonElement>($"/api/reports/yard-inventory?lineOperator={owner}&blockId={block.Id}&size=40", ct);
        Assert.Equal(1, filtered.GetProperty("totalContainers").GetInt32());
    }
}
