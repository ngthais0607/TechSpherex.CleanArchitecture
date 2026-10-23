using SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class RegisterContainerCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly RegisterContainerCommandHandler _handler;

    public RegisterContainerCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new RegisterContainerCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenIsoNumberIsValidAndNotExists_ShouldRegisterAndReturnId()
    {
        // Arrange (CSQU3054383 is a valid ISO 6346 container number)
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            new DateTime(2021, 5, 10),
            Classification.B,
            ContainerCondition.Damaged);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var containerInDb = await _context.Containers.FindAsync(result.Value);
        containerInDb.Should().NotBeNull();
        containerInDb!.ContainerNumber.Value.Should().Be("CSQU3054383");
        containerInDb.OwnerCode.Should().Be("CSQ");
        containerInDb.Condition.Should().Be(ContainerCondition.Damaged);
        containerInDb.Classification.Should().Be(Classification.B);
    }

    [Fact]
    public async Task Handle_WhenIsoNumberIsInvalid_ShouldReturnFailure()
    {
        // Arrange (CSQU3054389 has wrong check digit; expected 3, got 9)
        var command = new RegisterContainerCommand(
            "CSQU3054389",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            null);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.CheckDigitMismatch");
    }

    [Fact]
    public async Task Handle_WhenContainerAlreadyExistsInDepot_ShouldReturnConflict()
    {
        // Arrange
        var existingNumber = ContainerNumber.Create("CSQU3054383").Value!;
        var existingContainer = Container.Create(
            existingNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            null).Value!;

        _context.Containers.Add(existingContainer);
        await _context.SaveChangesAsync();

        var command = new RegisterContainerCommand(
            "csqu 3054383", // same number with whitespace/lowercase
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            null);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.AlreadyExists");
    }

    [Theory]
    [InlineData("22G")]
    [InlineData("22G11")]
    [InlineData("ABCD")]
    [InlineData("")]
    public async Task Handle_WhenIsoCodeInvalidFormat_ShouldReturnError(string invalidIso)
    {
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            invalidIso,
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            null);

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidIsoCode");
    }

    [Fact]
    public async Task Handle_WhenIsoCodeDoesNotMatchSize_ShouldReturnError()
    {
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            "42G1", // 40ft prefix but command says Ft20
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            null);

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.IsoSizeMismatch");
    }

    [Fact]
    public async Task Handle_WhenManufactureDateInFuture_ShouldReturnError()
    {
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            DateTime.UtcNow.AddDays(10));

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.FutureManufactureDate");
    }
}
