using System.Net;
using System.Net.Http.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Yard;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Yard;

[Collection("Api")]
public sealed class DemoYardLimitsTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateBlock_SeedDimensions_RemainSupported()
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.PostAsJsonAsync("/api/yard/blocks", new
        {
            code = "S" + Guid.NewGuid().ToString("N")[..8], name = "Seed size", isVirtual = false,
            bays = 10, rows = 6, tiers = 4
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(2000, 1, 1)]
    [InlineData(101, 1, 1)]
    [InlineData(1, 31, 1)]
    [InlineData(1, 1, 7)]
    public async Task CreateBlock_ExcessiveDimensions_ReturnsBadRequest(int bays, int rows, int tiers)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.PostAsJsonAsync("/api/yard/blocks", new
        {
            code = "L" + Guid.NewGuid().ToString("N")[..8], name = "Limit test", isVirtual = false, bays, rows, tiers
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("expand")]
    [InlineData("shrink")]
    public async Task ResizeBlock_ExcessiveDimensions_ReturnsBadRequest(string operation)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var block = YardBlock.CreateStandard("R" + Guid.NewGuid().ToString("N")[..8], "Resize test", 1, 1, 1, "DEPOT-A");
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var response = await client.PostAsJsonAsync($"/api/yard/blocks/{block.Id}/{operation}",
            new { newBays = 101, newRows = 1, newTiers = 1 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
