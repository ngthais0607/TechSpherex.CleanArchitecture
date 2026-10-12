using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class CreateYardBlockCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly CreateYardBlockCommandHandler _handler;

    public CreateYardBlockCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new CreateYardBlockCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenStandardBlockIsCreated_ShouldGeneratePhysicalSlotsAtOddBays()
    {
        // Arrange (Bays: 5 -> odd bays are 1, 3, 5 -> 3 bays. Rows: 2, Tiers: 2 -> 3 * 2 * 2 = 12 slots)
        var command = new CreateYardBlockCommand("BLK-A", "Block A", IsVirtual: false, Bays: 5, Rows: 2, Tiers: 2);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var block = await _context.YardBlocks.Include(b => b.Slots).FirstOrDefaultAsync(b => b.Id == result.Value);
        block.Should().NotBeNull();
        block!.Code.Should().Be("BLK-A");
        block.IsVirtual.Should().BeFalse();
        block.Slots.Should().HaveCount(12);
        block.Slots.All(s => s.Bay % 2 != 0).Should().BeTrue(); // All physical slots are odd bays
    }

    [Fact]
    public async Task Handle_WhenVirtualBlockIsCreated_ShouldHaveZeroSlots()
    {
        // Arrange
        var command = new CreateYardBlockCommand("BLK-V", "Khu PTI", IsVirtual: true);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var block = await _context.YardBlocks.Include(b => b.Slots).FirstOrDefaultAsync(b => b.Id == result.Value);
        block.Should().NotBeNull();
        block!.IsVirtual.Should().BeTrue();
        block.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existing = YardBlock.CreateStandard("BLK-A", "Existing", 2, 2, 2);
        _context.YardBlocks.Add(existing);
        await _context.SaveChangesAsync();

        var command = new CreateYardBlockCommand("blk-a", "Duplicate", IsVirtual: false, Bays: 4, Rows: 4, Tiers: 4);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.AlreadyExists");
    }
}
