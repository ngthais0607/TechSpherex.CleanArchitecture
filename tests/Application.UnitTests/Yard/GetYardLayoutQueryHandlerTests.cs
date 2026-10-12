using SnpDepotManagement.Application.Features.Yard.Queries.GetYardLayout;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class GetYardLayoutQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetYardLayoutQueryHandler _handler;

    public GetYardLayoutQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetYardLayoutQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenBlockExists_ShouldReturnYardLayoutWithSlots()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-L", "Layout Block", 3, 2, 2);
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var query = new GetYardLayoutQuery(block.Id);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BlockId.Should().Be(block.Id);
        result.Value.Code.Should().Be("BLK-L");
        result.Value.Slots.Should().HaveCount(block.Slots.Count);
    }

    [Fact]
    public async Task Handle_WhenBlockDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetYardLayoutQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.NotFound");
    }
}
