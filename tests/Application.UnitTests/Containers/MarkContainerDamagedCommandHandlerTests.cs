using SnpDepotManagement.Application.Features.Containers.Commands.MarkContainerDamaged;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class MarkContainerDamagedCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly MarkContainerDamagedCommandHandler _handler;

    public MarkContainerDamagedCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new MarkContainerDamagedCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenContainerExists_ShouldMarkDamagedWithoutChangingClassification()
    {
        // Arrange
        var number = ContainerNumber.Create("CSQU3054383").Value!;
        var container = Container.Create(number, "22G1", ContainerSize.Ft20, ContainerType.Dry, 2200, 30480, null, "CSQU").Value!;
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        var command = new MarkContainerDamagedCommand(container.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.Containers.FindAsync(container.Id);
        updated!.Condition.Should().Be(ContainerCondition.Damaged);
        updated.Classification.Should().Be(Classification.A);
    }

    [Fact]
    public async Task Handle_WhenContainerNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new MarkContainerDamagedCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.NotFound");
    }
}
