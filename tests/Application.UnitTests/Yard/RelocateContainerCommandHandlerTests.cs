using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class RelocateContainerCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly RelocateContainerCommandHandler _handler;

    public RelocateContainerCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new RelocateContainerCommandHandler(_context, new RuleEngine());
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
    public async Task Handle_WhenMovingToValidFreeSlot_ShouldRelocateSuccessfully()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-RELO", "Relocate Block", 5, 3, 4);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(container.Id);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        var command = new RelocateContainerCommand(block.Id, 1, 1, 1, 3, 1, 1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == block.Id);
        var oldSlot = updatedBlock.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        var newSlot = updatedBlock.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1);

        oldSlot.IsOccupied.Should().BeFalse();
        newSlot.IsOccupied.Should().BeTrue();
        newSlot.ContainerId.Should().Be(container.Id);
    }

    [Fact]
    public async Task Handle_WhenTargetSlotIsOccupied_ShouldFailAndPreserveSource()
    {
        // Arrange: 2 containers at bay 1 and bay 3
        var block = YardBlock.CreateStandard("BLK-OCC", "Block", 5, 3, 4);
        var c1 = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        var c2 = CreateValidContainer("ONE", "100002", ContainerSize.Ft20);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(c1.Id);
        block.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1).Occupy(c2.Id);

        _context.YardBlocks.Add(block);
        _context.Containers.AddRange(c1, c2);
        await _context.SaveChangesAsync();

        // Attempt to move c1 to bay 3 where c2 already is
        var command = new RelocateContainerCommand(block.Id, 1, 1, 1, 3, 1, 1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardRule.SlotAlreadyOccupied");

        // Verify c1 is still at bay 1
        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == block.Id);
        var slot1 = updatedBlock.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot1.IsOccupied.Should().BeTrue();
        slot1.ContainerId.Should().Be(c1.Id);
    }
}
