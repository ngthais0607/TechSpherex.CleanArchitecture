using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Yard.Commands.ShrinkYardBlock;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class ShrinkYardBlockCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly ShrinkYardBlockCommandHandler _handler;

    public ShrinkYardBlockCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new ShrinkYardBlockCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenSlotsAreNotOccupied_ShouldShrinkSuccessfully()
    {
        // Arrange (Initial: Bays: 3 (bays 1, 3); Rows: 1; Tiers: 1 -> 2 slots)
        var block = YardBlock.CreateStandard("BLK-SHRINK", "Shrinkable", 3, 1, 1);
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        // Act: Shrink to Bays: 1 -> slot at bay 3 will be removed
        var command = new ShrinkYardBlockCommand(block.Id, 1, 1, 1);
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstOrDefaultAsync(b => b.Id == block.Id);
        updatedBlock!.Slots.Should().HaveCount(1);
        updatedBlock.MaxBays.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenRemovedSlotIsOccupiedByContainer_ShouldReturnConflictAndBlockShrink()
    {
        // Arrange: Bays: 3 (bays 1, 3); slot at bay 3 is occupied
        var block = YardBlock.CreateStandard("BLK-BUSY", "Busy Block", 3, 1, 1);
        var slotToOccupy = block.Slots.First(s => s.Bay == 3);
        slotToOccupy.Occupy(Guid.NewGuid());

        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        // Act: Attempt to shrink to Bays: 1 (which removes Bay 3 where container sits)
        var command = new ShrinkYardBlockCommand(block.Id, 1, 1, 1);
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.CannotShrinkOccupiedSlots");

        // Verify block was NOT modified
        var blockInDb = await _context.YardBlocks.Include(b => b.Slots).FirstOrDefaultAsync(b => b.Id == block.Id);
        blockInDb!.MaxBays.Should().Be(3);
        blockInDb.Slots.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenBlockIsVirtual_ShouldReturnConflict()
    {
        // Arrange
        var block = YardBlock.CreateVirtual("BLK-VIRT", "Virtual Area");
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var command = new ShrinkYardBlockCommand(block.Id, 0, 0, 0);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.VirtualCannotShrink");
    }
}
