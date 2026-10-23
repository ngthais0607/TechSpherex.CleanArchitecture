using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.MasterData;
using Xunit;
namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class MasterSearchTests(ApiFactory factory)
{
    [Fact]
    public async Task MasterSearch_FiltersBeforePaging()
    {
        var ct = TestContext.Current.CancellationToken;
        var name = "Search" + Guid.NewGuid().ToString("N");
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.Customers.Add(Customer.Create("9" + Random.Shared.Next(100000000, 999999999), name).Value!);
            db.Customers.Add(Customer.Create("8" + Random.Shared.Next(100000000, 999999999), "Other customer").Value!);
            db.LineOperators.Add(LineOperator.Create("Q" + Guid.NewGuid().ToString("N")[..7], name).Value!);
            db.LineOperators.Add(LineOperator.Create("R" + Guid.NewGuid().ToString("N")[..7], "Other operator").Value!);
            await db.SaveChangesAsync(ct);
        }
        using var client = await ApiTestClient.LoginAsync(factory);
        foreach (var route in new[] { "customers", "line-operators" })
        {
            var result = await client.GetFromJsonAsync<JsonElement>($"/api/{route}?search={name}&pageSize=1", ct);
            Assert.Equal(1, result.GetProperty("totalCount").GetInt32());
            Assert.Equal(name, result.GetProperty("items")[0].GetProperty("name").GetString());
        }
    }
}
