using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Queries.GetContainerLifecycleHistory;
using SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Scenarios;

public class DepotEndToEndWorkflowTests
{
    private readonly TestAppDbContext _context;
    private readonly RuleEngine _ruleEngine;
    private readonly TimeProvider _timeProvider;

    public DepotEndToEndWorkflowTests()
    {
        _context = TestAppDbContext.Create();
        _ruleEngine = new RuleEngine();
        _timeProvider = TimeProvider.System;
    }

    [Fact]
    public async Task CompleteDepotLifecycle_FromRegistrationToGateOut_ShouldExecuteSuccessfully()
    {
        // =========================================================================
        // BƯỚC 1: KHỞI TẠO MASTER DATA (HÃNG TÀU & KHÁCH HÀNG)
        // =========================================================================
        var lineOpHandler = new CreateLineOperatorCommandHandler(_context);
        var lineOpResult = await lineOpHandler.Handle(new CreateLineOperatorCommand("CSQU", "Cosco Shipping Lines"));
        lineOpResult.IsSuccess.Should().BeTrue();

        var customerHandler = new CreateCustomerCommandHandler(_context);
        var custResult = await customerHandler.Handle(new CreateCustomerCommand("0101234567", "Cong Ty TNHH Logistics Quoc Te A"));
        custResult.IsSuccess.Should().BeTrue();

        // =========================================================================
        // BƯỚC 2: ĐĂNG KÝ CONTAINER RỖNG CHUẨN ISO 6346 (CSQU3054383)
        // =========================================================================
        var regContainerHandler = new RegisterContainerCommandHandler(_context);
        var regCommand = new RegisterContainerCommand(
            "CSQU3054383",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            new DateTime(2022, 1, 1),
            "CSQU");
        var regResult = await regContainerHandler.Handle(regCommand);
        regResult.IsSuccess.Should().BeTrue();
        var containerId = regResult.Value;

        // =========================================================================
        // BƯỚC 3: KHỞI TẠO BLOCK BÃI TIÊU CHUẨN (3 BAYS, 1 ROW, 2 TIERS)
        // =========================================================================
        var yardBlockHandler = new CreateYardBlockCommandHandler(_context);
        var blockResult = await yardBlockHandler.Handle(new CreateYardBlockCommand(
            "BLK-E2E",
            "Block E2E Test Area",
            false,
            3,
            1,
            2));
        blockResult.IsSuccess.Should().BeTrue();
        var blockId = blockResult.Value;

        var blockInDb = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == blockId);
        var targetSlot = blockInDb.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);

        // =========================================================================
        // BƯỚC 4: CONTAINER VÀO CỔNG DEPOT (GATE-IN) VÀ HẠ VÀO SLOT
        // =========================================================================
        var gateInHandler = new GateInCommandHandler(_context, _timeProvider);
        var gateInResult = await gateInHandler.Handle(new GateInCommand(
            containerId,
            "51C-77788",
            targetSlot.Id,
            Classification.A,
            ContainerCondition.Normal));
        gateInResult.IsSuccess.Should().BeTrue();

        // Kiểm tra sau Gate-In: slot bị chiếm, lifecycle InYard
        var slotAfterGateIn = await _context.YardSlots.FindAsync(targetSlot.Id);
        slotAfterGateIn!.IsOccupied.Should().BeTrue();
        slotAfterGateIn.ContainerId.Should().Be(containerId);

        // =========================================================================
        // BƯỚC 5: TẠO LỆNH GIAO NHẬN (DELIVERY ORDER) CHO KHÁCH HÀNG
        // =========================================================================
        var doHandler = new CreateDeliveryOrderCommandHandler(_context);
        var doResult = await doHandler.Handle(new CreateDeliveryOrderCommand(
            "DO-SNP-2026-001",
            "CSQU",
            "0101234567",
            "Cong Ty TNHH Logistics Quoc Te A",
            DateTime.UtcNow.AddDays(7),
            "COSCO PRIDE",
            new List<CreateDeliveryOrderItemInput>
            {
                new(ContainerSize.Ft20, ContainerType.Dry, 1)
            }));
        doResult.IsSuccess.Should().BeTrue();
        var deliveryOrderId = doResult.Value;

        // =========================================================================
        // BƯỚC 6: XUẤT CONTAINER RA KHỎI CẢNG (GATE-OUT)
        // =========================================================================
        var gateOutHandler = new GateOutCommandHandler(_context, _ruleEngine, _timeProvider);
        var gateOutResult = await gateOutHandler.Handle(new GateOutCommand(
            containerId,
            deliveryOrderId,
            "51C-99999"));
        gateOutResult.IsSuccess.Should().BeTrue();

        // =========================================================================
        // BƯỚC 7: XÁC THỰC TOÀN DIỆN TRẠNG THÁI HỆ THỐNG SAU KHI GATE-OUT
        // =========================================================================
        // 1. Lệnh DO đã hoàn tất
        var orderInDb = await _context.DeliveryOrders.Include(o => o.Items).FirstAsync(o => o.Id == deliveryOrderId);
        orderInDb.IsCompleted.Should().BeTrue();
        orderInDb.Items.First().DeliveredQuantity.Should().Be(1);
        orderInDb.Items.First().RemainingQuantity.Should().Be(0);

        // 2. Slot bãi đã được tự động giải phóng
        var slotAfterGateOut = await _context.YardSlots.FindAsync(targetSlot.Id);
        slotAfterGateOut!.IsOccupied.Should().BeFalse();
        slotAfterGateOut.ContainerId.Should().BeNull();

        // 3. Vòng đời container đã ghi nhận xuất cổng
        var historyHandler = new GetContainerLifecycleHistoryQueryHandler(_context);
        var historyResult = await historyHandler.Handle(new GetContainerLifecycleHistoryQuery(containerId));
        historyResult.IsSuccess.Should().BeTrue();
        historyResult.Value.Should().HaveCount(1);

        var finalLifecycle = historyResult.Value.First();
        finalLifecycle.Status.Should().Be(LifecycleStatus.GatedOut);
        finalLifecycle.InVehicleNumber.Should().Be("51C-77788");
        finalLifecycle.OutVehicleNumber.Should().Be("51C-99999");
        finalLifecycle.DeliveryOrderId.Should().Be(deliveryOrderId);
        finalLifecycle.OutDate.Should().NotBeNull();
    }
}
