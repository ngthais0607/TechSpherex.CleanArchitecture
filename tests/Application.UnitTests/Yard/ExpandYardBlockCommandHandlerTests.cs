using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Yard.Commands.ExpandYardBlock;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class ExpandYardBlockCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly ExpandYardBlockCommandHandler _handler;

    public ExpandYardBlockCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new ExpandYardBlockCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenExpandingStandardBlock_ShouldIncreaseSlots()
    {
        // Arrange (Initial: Bays: 1 -> bay 1; Rows: 1; Tiers: 1 -> 1 slot)
        var block = YardBlock.CreateStandard("BLK-EXP", "Expandable Block", 1, 1, 1);
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var initialSlots = block.Slots.Count; // 1 slot

        // Act: Expand to Bays: 3 (bays 1, 3), Rows: 1, Tiers: 1 -> 2 slots
        var command = new ExpandYardBlockCommand(block.Id, 3, 1, 1);
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstOrDefaultAsync(b => b.Id == block.Id);
        updatedBlock!.Slots.Count.Should().BeGreaterThan(initialSlots);
        updatedBlock.MaxBays.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenBlockNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new ExpandYardBlockCommand(Guid.NewGuid(), 5, 5, 5);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.NotFound");
    }
}
