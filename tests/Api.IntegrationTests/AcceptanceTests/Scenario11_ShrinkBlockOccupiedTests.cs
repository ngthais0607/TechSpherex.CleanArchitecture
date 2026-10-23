using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Yard;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 11: Thu hẹp block khi còn container ngoài vùng mới → 409 hoặc 422 Y6 (YardBlock.CannotShrinkOccupiedSlots).
/// </summary>
[Collection("Api")]
public sealed class Scenario11_ShrinkBlockOccupiedTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario11_ShrinkBlock_WhenCutSlotOccupied_ReturnsError_Y6()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var blockCode = "B" + Guid.NewGuid().ToString("N")[..6];
        Guid blockId;

        await using (var db = factory.Context(tenantId))
        {
            // Block ban đầu có 5 bays (slots ở bay 1, 3, 5)
            var block = YardBlock.CreateStandard(blockCode, "Shrinkable Block", 5, 2, 2, tenantId);
            blockId = block.Id;

            // Đặt container ở bay 5 (sẽ bị cắt khi thu hẹp về 3 bays)
            var slotAtBay5 = block.Slots.First(s => s.Bay == 5 && s.Row == 1 && s.Tier == 1);
            slotAtBay5.Occupy(container.Id);

            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        // Thu hẹp block từ 5 bays về 3 bays
        var payload = new
        {
            Bays = 3,
            Rows = 2,
            Tiers = 2
        };

        var response = await client.PostAsJsonAsync($"/api/yard/blocks/{blockId}/shrink", payload, ct);

        Assert.True(
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity,
            $"Expected Conflict or UnprocessableEntity, got {response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");

        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            body.Contains("YardBlock.CannotShrinkOccupiedSlots") ||
            (json.TryGetProperty("ruleCode", out var rc) && rc.GetString() == "Y6") ||
            (json.TryGetProperty("code", out var c) && c.GetString() == "YardBlock.CannotShrinkOccupiedSlots"),
            $"Expected Y6 or YardBlock.CannotShrinkOccupiedSlots error, got: {body}");
    }
}
