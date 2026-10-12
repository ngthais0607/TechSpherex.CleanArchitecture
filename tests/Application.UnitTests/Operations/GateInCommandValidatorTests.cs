using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GateInCommandValidatorTests
{
    private readonly GateInCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new GateInCommand(Guid.NewGuid(), "51C-12345", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenContainerIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new GateInCommand(Guid.Empty, "51C-12345", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContainerId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenVehicleNumberIsEmpty_ShouldHaveValidationError(string vehicle)
    {
        var command = new GateInCommand(Guid.NewGuid(), vehicle, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InVehicleNumber);
    }
}
