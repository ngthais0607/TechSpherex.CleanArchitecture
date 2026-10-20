using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Endpoints;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Application.Features.Yard.Queries.GetVirtualBlockContainers;
using SnpDepotManagement.Application.Features.Yard.Queries.YardSearch;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Yard;

[Collection("Api")]
public sealed class YardSearchTests(ApiFactory factory)
{
    private static async Task EnsureMasterDataAsync(ApiFactory factory, string lineOpCode, CancellationToken ct)
    {
        await using var db = factory.Context("DEPOT-A");
        if (!db.LineOperators.Any(l => l.Code == lineOpCode))
        {
            db.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create(lineOpCode, "Yard Line").Value!);
        }
        await db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task GetBlocks_ReturnsListOfBlocksWithOddBaysAndOccupancy()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var blockCode = "YB" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Block Test", 7, 2, 3, "DEPOT-A");

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            await db.SaveChangesAsync(ct);
        }

        var res = await client.GetAsync("/api/yard/blocks", ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var blocks = await res.Content.ReadFromJsonAsync<List<YardBlockDto>>(ct);
        Assert.NotNull(blocks);
        Assert.Contains(blocks, b => b.Code == blockCode && b.OddBays == 4 && b.MaxBays == 7);
    }

    [Fact]
    public async Task GetYardLayout_ByCode_WithTierFilter_AndContainers()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "YLO";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var blockCode = "YL" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Layout Test Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOp, "DEPOT-A");

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        // Gate-in into slot (1, 1, 1)
        var gateInRes = await client.PostAsJsonAsync("/api/gate/in", new GateInCommand
        {
            ContainerNumber = container.ContainerNumber.Value,
            VehicleNumber = "59C-99999",
            DriverName = "Driver Layout",
            BlockCode = blockCode,
            Bay = 1,
            Row = 1,
            Tier = 1,
            Classification = Classification.A,
            Condition = ContainerCondition.Normal,
            VehicleType = VehicleType.Truck
        }, ct);
        Assert.Equal(HttpStatusCode.Created, gateInRes.StatusCode);

        // Query layout by code with tier filter = 1
        var layoutRes = await client.GetAsync($"/api/yard/blocks/{blockCode}/layout?tier=1", ct);
        Assert.Equal(HttpStatusCode.OK, layoutRes.StatusCode);

        var layout = await layoutRes.Content.ReadFromJsonAsync<YardLayoutDto>(ct);
        Assert.NotNull(layout);
        Assert.Equal(blockCode, layout.Code);
        Assert.All(layout.Slots, s => Assert.Equal(1, s.Tier));
        Assert.Contains(layout.Containers, c => c.Number == container.ContainerNumber.Value);
    }

    [Fact]
    public async Task YardSearch_ReturnsInBlock_Virtual_OutOfYard_NotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "YSR";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        // 1. NotFound for non-existent container
        var resNotFound = await client.GetAsync("/api/yard/search?containerNumber=NONEXIST999", ct);
        Assert.Equal(HttpStatusCode.OK, resNotFound.StatusCode);
        var searchNotFound = await resNotFound.Content.ReadFromJsonAsync<YardSearchResult>(ct);
        Assert.NotNull(searchNotFound);
        Assert.Equal("NotFound", searchNotFound.Status);
        Assert.Null(searchNotFound.Location);

        // 2. OutOfYard for registered container with no active lifecycle
        var containerOut = OperationScenario.NewContainer(lineOp, "DEPOT-A");
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.Containers.Add(containerOut);
            await db.SaveChangesAsync(ct);
        }
        var resOut = await client.GetAsync($"/api/yard/search?containerNumber={containerOut.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, resOut.StatusCode);
        var searchOut = await resOut.Content.ReadFromJsonAsync<YardSearchResult>(ct);
        Assert.NotNull(searchOut);
        Assert.Equal("OutOfYard", searchOut.Status);

        // 3. Virtual for container in virtual block
        var vBlockCode = "VB" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var vBlock = YardBlock.CreateVirtual(vBlockCode, "Virtual Test", "DEPOT-A");
        var containerVirt = OperationScenario.NewContainer(lineOp, "DEPOT-A");
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(vBlock);
            db.Containers.Add(containerVirt);
            await db.SaveChangesAsync(ct);
        }

        var gateInVirt = await client.PostAsJsonAsync("/api/gate/in", new GateInCommand
        {
            ContainerNumber = containerVirt.ContainerNumber.Value,
            VehicleNumber = "59C-88888",
            DriverName = "Driver Virt",
            VirtualBlockCode = vBlockCode,
            VehicleType = VehicleType.Truck
        }, ct);
        Assert.Equal(HttpStatusCode.Created, gateInVirt.StatusCode);

        var resVirt = await client.GetAsync($"/api/yard/search?containerNumber={containerVirt.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, resVirt.StatusCode);
        var searchVirt = await resVirt.Content.ReadFromJsonAsync<YardSearchResult>(ct);
        Assert.NotNull(searchVirt);
        Assert.Equal("Virtual", searchVirt.Status);
        Assert.NotNull(searchVirt.Location);
        Assert.Equal(vBlockCode, searchVirt.Location.BlockCode);
        Assert.True(searchVirt.Location.IsVirtual);

        // 4. InBlock for container in physical block
        var blockCode = "PS" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Phys Test", 3, 1, 2, "DEPOT-A");
        var containerIn = OperationScenario.NewContainer(lineOp, "DEPOT-A");
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(containerIn);
            await db.SaveChangesAsync(ct);
        }

        var gateInPhys = await client.PostAsJsonAsync("/api/gate/in", new GateInCommand
        {
            ContainerNumber = containerIn.ContainerNumber.Value,
            VehicleNumber = "59C-77777",
            DriverName = "Driver Phys",
            BlockCode = blockCode,
            Bay = 1,
            Row = 1,
            Tier = 1,
            VehicleType = VehicleType.Truck
        }, ct);
        Assert.Equal(HttpStatusCode.Created, gateInPhys.StatusCode);

        var resIn = await client.GetAsync($"/api/yard/search?containerNumber={containerIn.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, resIn.StatusCode);
        var searchIn = await resIn.Content.ReadFromJsonAsync<YardSearchResult>(ct);
        Assert.NotNull(searchIn);
        Assert.Equal("InBlock", searchIn.Status);
        Assert.NotNull(searchIn.Location);
        Assert.Equal(blockCode, searchIn.Location.BlockCode);
        Assert.Equal(1, searchIn.Location.Bay);
        Assert.Equal(1, searchIn.Location.Row);
        Assert.Equal(1, searchIn.Location.Tier);
        Assert.False(searchIn.Location.IsVirtual);
    }

    [Fact]
    public async Task VirtualBlock_ListContainers_And_PlaceToPhysicalSlot()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "YVB";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var vBlockCode = "VB" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var vBlock = YardBlock.CreateVirtual(vBlockCode, "Virtual Yard Test", "DEPOT-A");
        var targetBlockCode = "TG" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var targetBlock = YardBlock.CreateStandard(targetBlockCode, "Target Physical Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOp, "DEPOT-A");

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(vBlock);
            db.YardBlocks.Add(targetBlock);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        // 1. Gate in to virtual block
        var gateInRes = await client.PostAsJsonAsync("/api/gate/in", new GateInCommand
        {
            ContainerNumber = container.ContainerNumber.Value,
            VehicleNumber = "59C-66666",
            DriverName = "Driver VB Place",
            VirtualBlockCode = vBlockCode,
            VehicleType = VehicleType.Truck
        }, ct);
        Assert.Equal(HttpStatusCode.Created, gateInRes.StatusCode);

        // 2. List containers in virtual block
        var listRes = await client.GetAsync($"/api/yard/virtual-blocks/{vBlockCode}/containers", ct);
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var list = await listRes.Content.ReadFromJsonAsync<List<VirtualBlockContainerDto>>(ct);
        Assert.NotNull(list);
        Assert.Contains(list, c => c.ContainerNumber == container.ContainerNumber.Value);

        // 3. Place from virtual block to physical block slot (1, 1, 1)
        var placeRes = await client.PostAsJsonAsync($"/api/yard/virtual-blocks/{vBlockCode}/containers/{container.Id}/place",
            new PlaceFromVirtualBlockRequest(targetBlockCode, 1, 1, 1), ct);
        Assert.Equal(HttpStatusCode.NoContent, placeRes.StatusCode);

        // 4. Verify search now returns InBlock at target slot
        var searchRes = await client.GetAsync($"/api/yard/search?containerNumber={container.ContainerNumber.Value}", ct);
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);
        var search = await searchRes.Content.ReadFromJsonAsync<YardSearchResult>(ct);
        Assert.NotNull(search);
        Assert.Equal("InBlock", search.Status);
        Assert.Equal(targetBlockCode, search.Location?.BlockCode);
        Assert.Equal(1, search.Location?.Bay);

        // 5. Verify virtual block containers list is now empty for this container
        var listAfterRes = await client.GetAsync($"/api/yard/virtual-blocks/{vBlockCode}/containers", ct);
        var listAfter = await listAfterRes.Content.ReadFromJsonAsync<List<VirtualBlockContainerDto>>(ct);
        Assert.NotNull(listAfter);
        Assert.DoesNotContain(listAfter, c => c.ContainerNumber == container.ContainerNumber.Value);
    }

    [Fact]
    public async Task ShrinkBlock_WithOccupiedSlot_ReturnsY6_WithContainerDetails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "YSK";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var blockCode = "SK" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Shrink Block", 5, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOp, "DEPOT-A");

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        // Place container in bay 5 (which will be removed if shrunk to bay 3)
        var gateInRes = await client.PostAsJsonAsync("/api/gate/in", new GateInCommand
        {
            ContainerNumber = container.ContainerNumber.Value,
            VehicleNumber = "59C-55555",
            DriverName = "Driver Shrink",
            BlockCode = blockCode,
            Bay = 5,
            Row = 1,
            Tier = 1,
            VehicleType = VehicleType.Truck
        }, ct);
        Assert.Equal(HttpStatusCode.Created, gateInRes.StatusCode);

        // Attempt shrink to 3 bays
        var shrinkRes = await client.PostAsJsonAsync($"/api/yard/blocks/{block.Id}/shrink",
            new YardBlockSizeRequest(3, 1, 2), ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, shrinkRes.StatusCode);
        var errBody = await shrinkRes.Content.ReadAsStringAsync(ct);
        Assert.Contains(container.ContainerNumber.Value, errBody);
        Assert.Contains("05-01-01", errBody);
        Assert.Contains("\"ruleCode\":\"Y6\"", errBody);
    }
}
