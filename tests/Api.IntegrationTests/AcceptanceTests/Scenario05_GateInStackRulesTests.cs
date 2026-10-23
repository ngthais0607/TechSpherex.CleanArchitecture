using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Yard;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 5: Nhập container 20ft vào bay chẵn → 422 Y1; đặt tier 2 khi tier 1 trống → 422 Y4.
/// </summary>
[Collection("Api")]
public sealed class Scenario05_GateInStackRulesTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario05_GateIn_20FtEvenBay_Returns422_Y1()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 4, 1, 2, tenantId);
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        // Cố nhập 20ft vào bay chẵn (Bay = 2)
        var payloadEvenBay = new
        {
            ContainerNumber = container.ContainerNumber.Value,
            BlockCode = blockCode,
            Bay = 2,
            Row = 1,
            Tier = 1,
            VehicleNumber = "29C-11111"
        };

        var response = await client.PostAsJsonAsync("/api/gate/in", payloadEvenBay, ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("StackRule.EvenBayInvalidFor20Ft") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "Y1") ||
            (json.TryGetProperty("code", out var c) && c.GetString() == "StackRule.EvenBayInvalidFor20Ft"),
            $"Expected Y1 or StackRule.EvenBayInvalidFor20Ft error, got: {body}");
    }

    [Fact]
    public async Task Scenario05_GateIn_Tier2WhenTier1Empty_Returns422_Y4()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            var block = YardBlock.CreateStandard(blockCode, "Test Block", 4, 1, 2, tenantId);
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        // Cố nhập vào Tier 2 khi Tier 1 đang trống
        var payloadTier2 = new
        {
            ContainerNumber = container.ContainerNumber.Value,
            BlockCode = blockCode,
            Bay = 1,
            Row = 1,
            Tier = 2,
            VehicleNumber = "29C-22222"
        };

        var response = await client.PostAsJsonAsync("/api/gate/in", payloadTier2, ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("StackRule.SlotBelowEmpty") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "Y4") ||
            (json.TryGetProperty("code", out var c) && c.GetString() == "StackRule.SlotBelowEmpty"),
            $"Expected Y4 or StackRule.SlotBelowEmpty error, got: {body}");
    }
}
