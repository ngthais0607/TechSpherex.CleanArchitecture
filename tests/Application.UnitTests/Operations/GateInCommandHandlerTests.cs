using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GateInCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GateInCommandHandler _handler;
    private readonly DateTimeOffset _utcNow = new(2026, 10, 10, 8, 30, 0, TimeSpan.Zero);

    public GateInCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GateInCommandHandler(_context, new FixedTimeProvider(_utcNow));
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
    public async Task Handle_WhenValid_ShouldOccupySlotAndCreateInYardLifecycle()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-IN", "Gate In Block", 3, 1, 2);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        var command = new GateInCommand(container.Id, "51C-99988", slot.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var slotInDb = await _context.YardSlots.FindAsync(slot.Id);
        slotInDb!.IsOccupied.Should().BeTrue();
        slotInDb.ContainerId.Should().Be(container.Id);

        var lifecycleInDb = await _context.ContainerLifecycles.FindAsync(result.Value);
        lifecycleInDb.Should().NotBeNull();
        lifecycleInDb!.Status.Should().Be(LifecycleStatus.InYard);
        lifecycleInDb.InDate.Should().Be(_utcNow.UtcDateTime);
        lifecycleInDb.InVehicleNumber.Should().Be("51C-99988");
        lifecycleInDb.CurrentSlotId.Should().Be(slot.Id);
    }

    [Fact]
    public async Task Handle_WhenSlotIsAlreadyOccupied_ShouldFailWithConflict()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-OCC", "Occ Block", 3, 1, 2);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.Occupy(Guid.NewGuid()); // already occupied

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        var command = new GateInCommand(container.Id, "51C-99988", slot.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardSlot.SlotAlreadyOccupied");
    }

    [Fact]
    public async Task Handle_WhenContainerAlreadyInYard_ShouldFailWithConflict()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-DUP", "Dup Block", 3, 1, 2);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        var slot1 = block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        var slot2 = block.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        // First Gate-In succeeds
        var command1 = new GateInCommand(container.Id, "51C-99988", slot1.Id);
        var res1 = await _handler.Handle(command1);
        res1.IsSuccess.Should().BeTrue();

        // Second Gate-In with same container should fail
        var command2 = new GateInCommand(container.Id, "51C-11122", slot2.Id);
        var res2 = await _handler.Handle(command2);

        // Assert
        res2.IsFailure.Should().BeTrue();
        res2.Error.Code.Should().Be("Lifecycle.ContainerAlreadyInYard");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}