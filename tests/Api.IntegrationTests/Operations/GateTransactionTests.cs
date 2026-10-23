using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Endpoints;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Application.Features.Gate.Queries.GetGateTransactions;
using SnpDepotManagement.Application.Features.Gate.Queries.ValidateGate;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class GateTransactionTests(ApiFactory factory)
{
    [Theory]
    [InlineData("Nguyễn Văn A", "nguyễn")]
    [InlineData("Nguyễn Văn A", "NGUYỄN VĂN A")]
    [InlineData("Tài xế %", "%")]
    [InlineData("Tài xế _", "_")]
    [InlineData("Tài xế \\", "\\")]
    [InlineData("Other driver", "59c-search")]
    public async Task Search_MatchesCaseInsensitiveLiteralText(string driverName, string search)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        await EnsureMasterDataAsync(factory, "SRH", ct);
        var container = OperationScenario.NewContainer("SRH", "DEPOT-A");
        var decoyContainer = OperationScenario.NewContainer("SRH", "DEPOT-A");
        var transaction = GateTransaction.CreateGateIn(container.Id, "59C-SEARCH", VehicleType.Truck,
            driverName, null, null, "DEPOT-A");
        var decoy = GateTransaction.CreateGateIn(decoyContainer.Id, "59C-OTHER", VehicleType.Truck,
            "Other driver", null, null, "DEPOT-A");
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.Containers.AddRange(container, decoyContainer);
            db.GateTransactions.AddRange(transaction, decoy);
            await db.SaveChangesAsync(ct);
        }

        var response = await client.GetAsync($"/api/gate/transactions?search={Uri.EscapeDataString(search)}&pageSize=100", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<GateTransactionDto>>(ct);
        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.Id == transaction.Id);
        Assert.DoesNotContain(result.Items, item => item.Id == decoy.Id);
    }

    private static async Task EnsureMasterDataAsync(ApiFactory factory, string lineOpCode, CancellationToken ct)
    {
        await using var db = factory.Context("DEPOT-A");
        if (!db.LineOperators.Any(l => l.Code == lineOpCode))
        {
            db.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create(lineOpCode, "Gate Line").Value!);
        }
        if (!db.Customers.Any(c => c.TaxCode == "0109997777"))
        {
            db.Customers.Add(Domain.Entities.MasterData.Customer.Create("0109997777", "Gate Customer").Value!);
        }
        await db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task ValidateGate_GateIn_FreeSlot_ReturnsPass()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "GAT";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var blockCode = "BK" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Gate Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOp, "DEPOT-A");

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        var response = await client.PostAsJsonAsync("/api/gate/validate", new ValidateGateQuery(
            container.ContainerNumber.Value,
            GateTransactionType.GateIn,
            BlockCode: blockCode,
            Bay: 1,
            Row: 1,
            Tier: 1), ct);

        var valBody = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Validate Failed: {response.StatusCode} - {valBody}");
        var result = await response.Content.ReadFromJsonAsync<ValidateGateResponseDto>(ct);
        Assert.NotNull(result);
        Assert.All(result.Checks, c => Assert.NotEqual(Application.Features.Gate.Evaluator.RuleState.Fail, c.State));
    }

    [Fact]
    public async Task GateIn_WithContainerNumberAndCoordinates_CreatesLifecycleAndTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "GTI";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var blockCode = "IN" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "In Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOp, "DEPOT-A");

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        var cmd = new GateInCommand
        {
            ContainerNumber = container.ContainerNumber.Value,
            VehicleNumber = "59C-11111",
            DriverName = "Nguyen Van A",
            BlockCode = blockCode,
            Bay = 1,
            Row = 1,
            Tier = 1,
            Classification = Classification.A,
            Condition = ContainerCondition.Normal,
            VehicleType = VehicleType.Truck
        };

        var response = await client.PostAsJsonAsync("/api/gate/in", cmd, ct);
        var errBody = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"GateIn Failed: {response.StatusCode} - {errBody}");

        await using (var db = factory.Context("DEPOT-A"))
        {
            var tx = await db.GateTransactions
                .Include(t => t.Changes)
                .FirstOrDefaultAsync(t => t.ContainerId == container.Id && t.Type == GateTransactionType.GateIn, ct);

            Assert.NotNull(tx);
            Assert.Equal("59C-11111", tx.VehicleNumber);
            Assert.Equal("Nguyen Van A", tx.DriverName);
            Assert.Equal(GateTransactionStatus.Active, tx.Status);

            var slot = await db.YardSlots.FirstOrDefaultAsync(s => s.BlockId == block.Id && s.Position.Bay == 1 && s.Position.Row == 1 && s.Position.Tier == 1, ct);
            Assert.NotNull(slot);
            Assert.True(slot.IsOccupied);
            Assert.Equal(container.Id, slot.ContainerId);
        }
    }

    [Fact]
    public async Task GateOut_Update_And_CancelTransaction_Flow_Verified()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "GTO";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var blockCode = "OT" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Out Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer(lineOp, "DEPOT-A");
        var orderNo = "DO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var order = DeliveryOrder.Create(orderNo, lineOp, "0109997777", "Gate Customer", DateTime.UtcNow.AddDays(5), "VESSEL OUT", "DEPOT-A").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        Guid slotId;
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.DeliveryOrders.Add(order);

            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);
            slotId = slot.Id;

            var lc = ContainerLifecycle.GateIn(container.Id, lineOp, Classification.A, ContainerCondition.Normal, "59C-00000", slot.Id, DateTime.UtcNow, "DEPOT-A");
            db.ContainerLifecycles.Add(lc);

            await db.SaveChangesAsync(ct);
        }

        // 1. GateOut
        var gateOutCmd = new GateOutCommand
        {
            ContainerNumber = container.ContainerNumber.Value,
            OrderNumber = orderNo,
            VehicleNumber = "59C-22222",
            DriverName = "Tran Van B",
            VehicleType = VehicleType.Truck
        };

        var outRes = await client.PostAsJsonAsync("/api/gate/out", gateOutCmd, ct);
        Assert.True(outRes.IsSuccessStatusCode);

        Guid txId;
        await using (var db = factory.Context("DEPOT-A"))
        {
            var tx = await db.GateTransactions.FirstOrDefaultAsync(t => t.ContainerId == container.Id && t.Type == GateTransactionType.GateOut, ct);
            Assert.NotNull(tx);
            Assert.Equal(GateTransactionStatus.Active, tx.Status);
            txId = tx.Id;

            var slot = await db.YardSlots.FindAsync([slotId], ct);
            Assert.False(slot!.IsOccupied);
        }

        // 2. Update transaction
        var updateReq = new UpdateTransactionRequest("59C-33333", VehicleType.Truck, "Tran Van B Updated", Classification.B, ContainerCondition.Dented, "Kiem tra lai");
        var putRes = await client.PutAsJsonAsync($"/api/gate/transactions/{txId}", updateReq, ct);
        var putBody = await putRes.Content.ReadAsStringAsync(ct);
        Assert.True(putRes.IsSuccessStatusCode, $"PUT failed: {putRes.StatusCode} - {putBody}");

        await using (var db = factory.Context("DEPOT-A"))
        {
            var tx = await db.GateTransactions.Include(t => t.Changes).FirstOrDefaultAsync(t => t.Id == txId, ct);
            Assert.NotNull(tx);
            Assert.Equal("59C-33333", tx.VehicleNumber);
            Assert.Equal("Tran Van B Updated", tx.DriverName);
            Assert.True(tx.Changes.Count >= 2);
        }

        // 3. Cancel GateOut transaction -> slot is restored!
        var cancelReq = new CancelTransactionRequest("Huy lenh xuat nham");
        var deleteRes = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/gate/transactions/{txId}/cancel")
        {
            Content = JsonContent.Create(cancelReq)
        }, ct);
        var delBody = await deleteRes.Content.ReadAsStringAsync(ct);
        Assert.True(deleteRes.IsSuccessStatusCode, $"DELETE failed: {deleteRes.StatusCode} - {delBody}");

        await using (var db = factory.Context("DEPOT-A"))
        {
            var tx = await db.GateTransactions.FindAsync([txId], ct);
            Assert.Equal(GateTransactionStatus.Cancelled, tx!.Status);

            var slot = await db.YardSlots.FindAsync([slotId], ct);
            Assert.True(slot!.IsOccupied);
            Assert.Equal(container.Id, slot.ContainerId);

            var lc = await db.ContainerLifecycles.FirstOrDefaultAsync(l => l.ContainerId == container.Id, ct);
            Assert.Equal(LifecycleStatus.InYard, lc!.Status);
        }
    }

    [Fact]
    public async Task CancelGateOut_WhenOriginalSlotOccupied_ReturnsConflict_G9()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var lineOp = "GTC";
        await EnsureMasterDataAsync(factory, lineOp, ct);

        var blockCode = "OC" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var block = YardBlock.CreateStandard(blockCode, "Conflict Block", 3, 1, 2, "DEPOT-A");
        var container1 = OperationScenario.NewContainer(lineOp, "DEPOT-A");
        var container2 = OperationScenario.NewContainer(lineOp, "DEPOT-A");
        var orderNo = "DO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var order = DeliveryOrder.Create(orderNo, lineOp, "0109997777", "Gate Customer", DateTime.UtcNow.AddDays(5), "VESSEL CONFLICT", "DEPOT-A").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        Guid slotId;
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.AddRange(container1, container2);
            db.DeliveryOrders.Add(order);

            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container1.Id);
            slotId = slot.Id;

            var lc = ContainerLifecycle.GateIn(container1.Id, lineOp, Classification.A, ContainerCondition.Normal, "59C-99999", slot.Id, DateTime.UtcNow, "DEPOT-A");
            db.ContainerLifecycles.Add(lc);

            await db.SaveChangesAsync(ct);
        }

        // Gate Out container1
        var outRes = await client.PostAsJsonAsync("/api/gate/out", new GateOutCommand
        {
            ContainerNumber = container1.ContainerNumber.Value,
            OrderNumber = orderNo,
            VehicleNumber = "59C-99999"
        }, ct);
        Assert.True(outRes.IsSuccessStatusCode);

        Guid txId;
        // Chiếm slot đó bằng container2
        await using (var db = factory.Context("DEPOT-A"))
        {
            var tx = await db.GateTransactions.FirstAsync(t => t.ContainerId == container1.Id && t.Type == GateTransactionType.GateOut, ct);
            txId = tx.Id;

            var slot = await db.YardSlots.FindAsync([slotId], ct);
            slot!.Occupy(container2.Id);
            await db.SaveChangesAsync(ct);
        }

        // Cố gắng hủy GateOut của container1 -> slot bị chiếm -> Expect 409 G9.SlotOccupied
        var cancelRes = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/gate/transactions/{txId}/cancel"), ct);
        Assert.Equal(HttpStatusCode.Conflict, cancelRes.StatusCode);

        var problem = await cancelRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        Assert.Equal("G9.SlotOccupied", problem.GetProperty("code").GetString());
    }
}
