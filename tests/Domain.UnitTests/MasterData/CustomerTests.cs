using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Domain.UnitTests.MasterData;

public sealed class CustomerTests
{
    [Theory]
    [InlineData("0123456789")]       // 10 digits
    [InlineData("0123456789001")]    // 13 digits
    public void Create_WithValidTaxCode_ShouldSucceed(string taxCode)
    {
        var result = Customer.Create(taxCode, "Saigon Newport Corporation");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TaxCode.Should().Be(taxCode);
        result.Value.Name.Should().Be("Saigon Newport Corporation");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTaxCodeIsEmpty_ShouldReturnError(string? taxCode)
    {
        var result = Customer.Create(taxCode!, "Saigon Newport Corporation");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.EmptyTaxCode");
    }

    [Theory]
    [InlineData("123456789")]        // 9 digits
    [InlineData("12345678901")]      // 11 digits
    [InlineData("123456789012")]     // 12 digits
    [InlineData("12345678901234")]   // 14 digits
    public void Create_WhenTaxCodeLengthInvalid_ShouldReturnError(string taxCode)
    {
        var result = Customer.Create(taxCode, "Saigon Newport Corporation");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.InvalidTaxCodeLength");
    }

    [Theory]
    [InlineData("012345678A")]
    [InlineData("012345678900A")]
    [InlineData("ABCDEFGHIJ")]
    public void Create_WhenTaxCodeContainsNonDigits_ShouldReturnError(string taxCode)
    {
        var result = Customer.Create(taxCode, "Saigon Newport Corporation");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.InvalidTaxCodeFormat");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameIsEmpty_ShouldReturnError(string? name)
    {
        var result = Customer.Create("0123456789", name!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.EmptyName");
    }
}
