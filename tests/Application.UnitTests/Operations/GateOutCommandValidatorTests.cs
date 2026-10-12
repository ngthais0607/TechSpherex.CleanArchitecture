using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GateOutCommandValidatorTests
{
    private readonly GateOutCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new GateOutCommand(Guid.NewGuid(), Guid.NewGuid(), "51C-99988");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_ShouldHaveValidationErrors()
    {
        var command = new GateOutCommand(Guid.Empty, Guid.Empty, "51C-99988");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContainerId);
        result.ShouldHaveValidationErrorFor(x => x.DeliveryOrderId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenVehicleNumberIsEmpty_ShouldHaveValidationError(string vehicle)
    {
        var command = new GateOutCommand(Guid.NewGuid(), Guid.NewGuid(), vehicle);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OutVehicleNumber);
    }
}
