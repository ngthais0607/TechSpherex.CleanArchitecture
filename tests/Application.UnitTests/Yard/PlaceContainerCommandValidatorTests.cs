using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class PlaceContainerCommandValidatorTests
{
    private readonly PlaceContainerCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new PlaceContainerCommand(Guid.NewGuid(), 1, 1, 1, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(-1, 1, 1)]
    public void Validate_WhenCoordinatesAreZeroOrNegative_ShouldHaveErrors(int bay, int row, int tier)
    {
        var command = new PlaceContainerCommand(Guid.NewGuid(), bay, row, tier, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_ShouldHaveErrors()
    {
        var command = new PlaceContainerCommand(Guid.Empty, 1, 1, 1, Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BlockId);
        result.ShouldHaveValidationErrorFor(x => x.ContainerId);
    }
}
