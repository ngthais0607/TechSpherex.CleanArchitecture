using SnpDepotManagement.Application.Features.MasterData.Vehicles.Commands.CreateVehicle;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.MasterData;

public class CreateVehicleCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly CreateVehicleCommandHandler _handler;

    public CreateVehicleCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new CreateVehicleCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenPlateNumberIsUnique_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateVehicleCommand("51C-12345", VehicleType.Truck);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var entity = await _context.Vehicles.FindAsync(result.Value);
        entity.Should().NotBeNull();
        entity!.PlateNumber.Should().Be("51C-12345");
        entity.Type.Should().Be(VehicleType.Truck);
    }

    [Fact]
    public async Task Handle_WhenPlateNumberAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existing = Vehicle.Create("51C-12345", VehicleType.Barge).Value!;
        _context.Vehicles.Add(existing);
        await _context.SaveChangesAsync();

        var command = new CreateVehicleCommand("51c-12345", VehicleType.Truck);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Vehicle.AlreadyExists");
    }
}
