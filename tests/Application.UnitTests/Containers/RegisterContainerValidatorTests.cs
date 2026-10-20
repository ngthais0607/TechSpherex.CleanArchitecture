using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class RegisterContainerValidatorTests
{
    private readonly RegisterContainerCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            new DateTime(2020, 1, 1));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("CSQU30543")]    // 9 chars
    [InlineData("CSQU30543830")]  // 12 chars
    [InlineData("")]
    public void Validate_WhenNumberLengthIsNot11_ShouldHaveValidationError(string number)
    {
        var command = new RegisterContainerCommand(
            number,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200,
            30480,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContainerNumber);
    }

    [Fact]
    public void Validate_WhenTareWeightIsZeroOrNegative_ShouldHaveValidationError()
    {
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            0,
            30480,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TareWeight);
    }

    [Fact]
    public void Validate_WhenMaxGrossWeightIsLessThanOrEqualToTare_ShouldHaveValidationError()
    {
        var command = new RegisterContainerCommand(
            "CSQU3054383",
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            5000,
            4000,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.MaxGrossWeight);
    }
}
