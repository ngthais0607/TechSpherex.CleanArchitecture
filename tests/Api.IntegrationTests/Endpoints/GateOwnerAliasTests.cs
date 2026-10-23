using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.MasterData;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class GateOwnerAliasTests(ApiFactory factory)
{
    [Fact]
    public async Task GateInOwnerAlias_AppearsUnderMappedOperatorInventory()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        var container = OperationScenario.NewContainer("RZX");
        var code = "AL" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        await using (var db = factory.Context("DEPOT-A"))
        {
            var line = LineOperator.Create(code, "Alias line").Value!;
            db.LineOperators.Add(line);
            db.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(line.Id, "RZX", "DEPOT-A"));
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }
        using var client = await ApiTestClient.LoginAsync(factory);
        using var response = await client.PostAsJsonAsync("/api/gate/in", new
        {
            containerNumber = container.ContainerNumber.Value, blockCode = scenario.Block.Code,
            bay = 5, row = 1, tier = 1, vehicleNumber = "ALIAS-TRUCK"
        }, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var inventory = await client.GetFromJsonAsync<JsonElement>(
            $"/api/reports/yard-inventory?lineOperator={code}&blockId={scenario.Block.Id}", ct);
        Assert.Equal(1, inventory.GetProperty("totalContainers").GetInt32());
    }
}
