using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class RemoveContainerCommandValidatorTests
{
    private readonly RemoveContainerCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RemoveContainerCommand(Guid.NewGuid(), 1, 1, 1);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void Validate_WhenCoordinatesAreZeroOrNegative_ShouldHaveErrors(int bay, int row, int tier)
    {
        var command = new RemoveContainerCommand(Guid.NewGuid(), bay, row, tier);
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenBlockIdIsEmpty_ShouldHaveError()
    {
        var command = new RemoveContainerCommand(Guid.Empty, 1, 1, 1);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BlockId);
    }
}
