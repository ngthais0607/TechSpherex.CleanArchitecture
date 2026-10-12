using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Identity.Commands.Login;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public class LoginValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new LoginCommand("admin", "P@ssword123");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenUserNameIsEmpty_ShouldHaveValidationError(string? userName)
    {
        var command = new LoginCommand(userName!, "P@ssword123");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenPasswordIsEmpty_ShouldHaveValidationError(string? password)
    {
        var command = new LoginCommand("admin", password!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
