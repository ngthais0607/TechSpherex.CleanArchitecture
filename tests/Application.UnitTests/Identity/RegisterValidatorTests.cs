using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Application.Features.Identity.Commands.Register;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public class RegisterValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RegisterCommand("newuser", "user@snp.com.vn", "Nguyen", "Van B", "Password123", Roles.YardPlanner);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@missing-username.com")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveValidationError(string email)
    {
        var command = new RegisterCommand("newuser", email, "Nguyen", "Van B", "Password123", Roles.GateClerk);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("12345")] // 5 chars, min is 6
    [InlineData("")]
    public void Validate_WhenPasswordIsTooShort_ShouldHaveValidationError(string password)
    {
        var command = new RegisterCommand("newuser", "user@snp.com.vn", "Nguyen", "Van B", password, Roles.OrderClerk);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Guest")]
    [InlineData("RandomRole")]
    public void Validate_WhenRoleIsInvalid_ShouldHaveValidationError(string role)
    {
        var command = new RegisterCommand("newuser", "user@snp.com.vn", "Nguyen", "Van B", "Password123", role);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
