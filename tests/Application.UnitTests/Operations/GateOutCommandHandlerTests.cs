using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GateOutCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GateOutCommandHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _utcNow = new(2026, 10, 10, 9, 30, 0, TimeSpan.Zero);

    public GateOutCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _timeProvider = new FixedTimeProvider(_utcNow);
        _handler = new GateOutCommandHandler(_context, new RuleEngine(), _timeProvider);
    }

    private static Container CreateValidContainer(string ownerPrefix, string serialNumber, ContainerSize size)
    {
        string raw = $"{ownerPrefix}U{serialNumber}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = raw[i];
            int val = char.IsLetter(c) ? (c - 'A' + 10) : (c - '0');
            if (char.IsLetter(c))
            {
                if (val >= 11) val++;
                if (val >= 22) val++;
                if (val >= 33) val++;
            }
            sum += val * (1 << i);
        }
        int rem = sum % 11;
        int checkDigit = rem == 10 ? 0 : rem;

        var number = ContainerNumber.Create($"{raw}{checkDigit}").Value!;
        return Container.Create(number, "22G1", size, ContainerType.Dry, 2200, 30480, null, ownerPrefix).Value!;
    }

    [Fact]
    public async Task Handle_WhenAllRulesPass_ShouldGateOutFulfillDoAndReleaseSlots()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-OUT", "Block", 3, 1, 2);
        var container = CreateValidContainer("MAE", "100001", ContainerSize.Ft20);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.Occupy(container.Id);

        var lifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            Classification.A,
            ContainerCondition.Normal,
            "51C-11111",
            slot.Id,
            _utcNow.UtcDateTime.AddHours(-1));

        var order = DeliveryOrder.Create(
            "DO-VALID-01",
            "MAE",
            "0101234567",
            "Cong Ty XNK",
            _utcNow.UtcDateTime.AddDays(7),
            "VESSEL-01").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        _context.ContainerLifecycles.Add(lifecycle);
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var command = new GateOutCommand(container.Id, order.Id, "51C-22222");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // 1. Check lifecycle updated
        var lifecycleInDb = await _context.ContainerLifecycles.FindAsync(lifecycle.Id);
        lifecycleInDb!.Status.Should().Be(LifecycleStatus.GatedOut);
        lifecycleInDb.OutVehicleNumber.Should().Be("51C-22222");
        lifecycleInDb.OutVehicleType.Should().Be(VehicleType.Truck);
        lifecycleInDb.OutDate.Should().Be(_utcNow.UtcDateTime);
        lifecycleInDb.DeliveryOrderId.Should().Be(order.Id);

        // 2. Check slot released
        var slotInDb = await _context.YardSlots.FindAsync(slot.Id);
        slotInDb!.IsOccupied.Should().BeFalse();

        // 3. Check order fulfilled
        var orderInDb = await _context.DeliveryOrders.Include(o => o.Items).FirstAsync(o => o.Id == order.Id);
        orderInDb.IsCompleted.Should().BeTrue();
        orderInDb.Items.First().DeliveredQuantity.Should().Be(1);
        orderInDb.Items.First().RemainingQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenLineOperatorMismatches_ShouldFailWithConflict()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-MISMATCH", "Block", 3, 1, 2);
        var container = CreateValidContainer("ONE", "100001", ContainerSize.Ft20); // ONE
        var slot = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.Occupy(container.Id);

        var lifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            Classification.A,
            ContainerCondition.Normal,
            "51C-11111",
            slot.Id,
            _utcNow.UtcDateTime.AddHours(-1));

        var order = DeliveryOrder.Create(
            "DO-MAEU-01",
            "MAE", // MAE does not match ONE
            "0101234567",
            "Cong Ty XNK",
            _utcNow.UtcDateTime.AddDays(7),
            "VESSEL-01").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        _context.ContainerLifecycles.Add(lifecycle);
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var command = new GateOutCommand(container.Id, order.Id, "51C-22222");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.LineOperatorMismatch");
    }

    [Fact]
    public async Task Handle_WhenDeliveryOrderIsExpired_ShouldFailWithValidationError()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-EXP", "Block", 3, 1, 2);
        var container = CreateValidContainer("MAE", "100001", ContainerSize.Ft20);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.Occupy(container.Id);

        var lifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            Classification.A,
            ContainerCondition.Normal,
            "51C-11111",
            slot.Id,
            _utcNow.UtcDateTime.AddHours(-1));

        var order = DeliveryOrder.Create(
            "DO-EXPIRED",
            "MAE",
            "0101234567",
            "Cong Ty XNK",
            _utcNow.UtcDateTime.AddDays(-2), // expired 2 days ago
            "VESSEL-01").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        _context.ContainerLifecycles.Add(lifecycle);
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var command = new GateOutCommand(container.Id, order.Id, "51C-22222");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.DeliveryOrderExpired");
    }

    [Fact]
    public async Task Handle_WhenNoRemainingQuantityInOrder_ShouldFailWithConflict()
    {
        // Arrange: order with item already fully delivered
        var block = YardBlock.CreateStandard("BLK-QTY", "Block", 3, 1, 2);
        var container = CreateValidContainer("MAE", "100001", ContainerSize.Ft20);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.Occupy(container.Id);

        var lifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            Classification.A,
            ContainerCondition.Normal,
            "51C-11111",
            slot.Id,
            _utcNow.UtcDateTime.AddHours(-1));

        var order = DeliveryOrder.Create(
            "DO-FULFILLED",
            "MAE",
            "0101234567",
            "Cong Ty XNK",
            _utcNow.UtcDateTime.AddDays(7),
            "VESSEL-01").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);
        order.Items.First().Allocate(1); // fully delivered

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        _context.ContainerLifecycles.Add(lifecycle);
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var command = new GateOutCommand(container.Id, order.Id, "51C-22222");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.NoRemainingQuantity");
    }

    [Fact]
    public async Task Handle_WhenContainerNotInYard_ShouldFailWithConflict()
    {
        // Arrange: Container without any active in-yard lifecycle
        var container = CreateValidContainer("MAE", "100001", ContainerSize.Ft20);
        var order = DeliveryOrder.Create("DO-NOYARD", "MAE", "0101234567", "A", _utcNow.UtcDateTime.AddDays(7), "V").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        _context.Containers.Add(container);
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var command = new GateOutCommand(container.Id, order.Id, "51C-22222");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lifecycle.NotInYard");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
