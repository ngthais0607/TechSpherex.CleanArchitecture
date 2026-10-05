using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.UnitTests.MasterData;

public sealed class VehicleTests
{
    [Theory]
    [InlineData("51C-12345", VehicleType.Truck)]
    [InlineData("SG-8888", VehicleType.Barge)]
    public void Create_WithValidData_ShouldSucceed(string plateNumber, VehicleType type)
    {
        var result = Vehicle.Create(plateNumber, type);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PlateNumber.Should().Be(plateNumber);
        result.Value.Type.Should().Be(type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenPlateNumberIsEmpty_ShouldReturnError(string? plateNumber)
    {
        var result = Vehicle.Create(plateNumber!, VehicleType.Truck);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Vehicle.EmptyPlateNumber");
    }
}
