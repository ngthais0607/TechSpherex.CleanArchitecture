using SnpDepotManagement.Application.Features.Containers.Queries.GetContainerByNumber;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class GetContainerByNumberQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetContainerByNumberQueryHandler _handler;

    public GetContainerByNumberQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetContainerByNumberQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenContainerExists_ShouldReturnContainerDto()
    {
        // Arrange
        var number = ContainerNumber.Create("CSQU3054383").Value!;
        var container = Container.Create(number, "22G1", ContainerSize.Ft20, ContainerType.Dry, 2200, 30480, null, "CSQU").Value!;
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        var query = new GetContainerByNumberQuery("csqu 3054383");

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContainerNumber.Should().Be("CSQU3054383");
        result.Value.OwnerCode.Should().Be("CSQU");
    }

    [Fact]
    public async Task Handle_WhenContainerNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetContainerByNumberQuery("CSQU3054383");

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.NotFound");
    }

    [Fact]
    public async Task Handle_WhenNumberIsInvalid_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetContainerByNumberQuery("INVALID");

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
