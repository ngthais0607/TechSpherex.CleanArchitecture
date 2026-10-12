using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;

namespace SnpDepotManagement.Application.UnitTests.MasterData;

public class CustomerValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator = new();

    [Theory]
    [InlineData("0312345678")]     // 10 digits
    [InlineData("0312345678001")]  // 13 digits
    public void Validate_WhenTaxCodeHasValidLength_ShouldNotHaveTaxCodeErrors(string taxCode)
    {
        var command = new CreateCustomerCommand(taxCode, "Cong ty TNHH Van Tai Bien");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.TaxCode);
    }

    [Theory]
    [InlineData("123456789")]      // 9 digits
    [InlineData("12345678901")]    // 11 digits
    [InlineData("123456789012")]   // 12 digits
    [InlineData("12345678901234")] // 14 digits
    public void Validate_WhenTaxCodeLengthIsNeither10Nor13_ShouldHaveValidationError(string taxCode)
    {
        var command = new CreateCustomerCommand(taxCode, "Cong ty A");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TaxCode);
    }

    [Theory]
    [InlineData("031234567A")]     // Contains letters
    [InlineData("031234567-01")]   // Contains hyphen
    public void Validate_WhenTaxCodeContainsNonDigits_ShouldHaveValidationError(string taxCode)
    {
        var command = new CreateCustomerCommand(taxCode, "Cong ty A");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TaxCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError(string? name)
    {
        var command = new CreateCustomerCommand("0312345678", name!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
