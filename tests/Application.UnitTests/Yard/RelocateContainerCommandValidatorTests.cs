using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class RelocateContainerCommandValidatorTests
{
    private readonly RelocateContainerCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RelocateContainerCommand(Guid.NewGuid(), 1, 1, 1, 3, 1, 1);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenSourceEqualsTarget_ShouldHaveValidationError()
    {
        var command = new RelocateContainerCommand(Guid.NewGuid(), 1, 1, 1, 1, 1, 1);
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 1, 1, 3, 1, 1)]
    [InlineData(1, 1, 1, 0, 1, 1)]
    public void Validate_WhenCoordinatesAreZeroOrNegative_ShouldHaveValidationError(
        int sBay, int sRow, int sTier, int tBay, int tRow, int tTier)
    {
        var command = new RelocateContainerCommand(Guid.NewGuid(), sBay, sRow, sTier, tBay, tRow, tTier);
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }
}
