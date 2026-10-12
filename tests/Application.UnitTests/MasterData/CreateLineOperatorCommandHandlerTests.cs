using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.UnitTests.MasterData;

public class CreateLineOperatorCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly CreateLineOperatorCommandHandler _handler;

    public CreateLineOperatorCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new CreateLineOperatorCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenCodeIsUnique_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateLineOperatorCommand("MAEU", "Maersk Line");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var entity = await _context.LineOperators.FindAsync(result.Value);
        entity.Should().NotBeNull();
        entity!.Code.Should().Be("MAEU");
        entity.Name.Should().Be("Maersk Line");
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existing = LineOperator.Create("MAEU", "Existing Maersk").Value!;
        _context.LineOperators.Add(existing);
        await _context.SaveChangesAsync();

        var command = new CreateLineOperatorCommand("maeu", "Duplicate Maersk");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LineOperator.AlreadyExists");
    }
}
