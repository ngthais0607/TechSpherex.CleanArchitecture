using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class CreateYardBlockValidatorTests
{
    [Theory]
    [InlineData(101, 1, 1)]
    [InlineData(1, 31, 1)]
    [InlineData(1, 1, 7)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    public void Validate_ExcessiveDimensions_ReturnsErrors(int bays, int rows, int tiers)
    {
        var result = new CreateYardBlockCommandValidator().TestValidate(new CreateYardBlockCommand("LIMIT", "Test", false, bays, rows, tiers));
        result.IsValid.Should().BeFalse();
    }

    private readonly CreateYardBlockCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenStandardBlockIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CreateYardBlockCommand("BLK-A", "Block A Standard", IsVirtual: false, Bays: 10, Rows: 6, Tiers: 5);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenVirtualBlockIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CreateYardBlockCommand("BLK-VIRTUAL", "Virtual Repair Area", IsVirtual: true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(5, 0, 5)]
    [InlineData(5, 5, 0)]
    public void Validate_WhenStandardBlockHasZeroDimensions_ShouldHaveValidationErrors(int bays, int rows, int tiers)
    {
        var command = new CreateYardBlockCommand("BLK-A", "Block A", IsVirtual: false, Bays: bays, Rows: rows, Tiers: tiers);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
    }
}
