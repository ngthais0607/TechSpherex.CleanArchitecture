using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.UnitTests.Containers;

public sealed class ContainerNumberTests
{
    private const string ValidContainerNumber = "CSQU3054383";

    [Fact]
    public void Create_WithValidNumber_ShouldSucceed()
    {
        var result = ContainerNumber.Create(ValidContainerNumber);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(ValidContainerNumber);
        result.Value.OwnerCode.Should().Be("CSQ");
        result.Value.CategoryIdentifier.Should().Be('U');
        result.Value.SerialNumber.Should().Be("305438");
        result.Value.CheckDigit.Should().Be(3);
    }

    [Theory]
    [InlineData("csqu3054383")]
    [InlineData(" CSQU 305438 3 ")]
    [InlineData("csqu 305438 3")]
    public void Create_WithLowerCaseOrWhitespace_ShouldNormalizeSuccessfully(string input)
    {
        var result = ContainerNumber.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(ValidContainerNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenEmptyOrWhitespace_ShouldReturnEmptyError(string? input)
    {
        var result = ContainerNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.Empty");
    }

    [Theory]
    [InlineData("CSQU305438")]     // 10 chars
    [InlineData("CSQU30543839")]   // 12 chars
    public void Create_WhenLengthInvalid_ShouldReturnInvalidLengthError(string input)
    {
        var result = ContainerNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.InvalidLength");
    }

    [Theory]
    [InlineData("1SQU3054383")]
    [InlineData("C1QU3054383")]
    [InlineData("CS1U3054383")]
    public void Create_WhenOwnerCodeContainsNonLetter_ShouldReturnInvalidOwnerCodeError(string input)
    {
        var result = ContainerNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.InvalidOwnerCode");
    }

    [Theory]
    [InlineData("CSQA3054383")]
    [InlineData("CSQX3054383")]
    public void Create_WhenCategoryIdentifierInvalid_ShouldReturnInvalidCategoryIdentifierError(string input)
    {
        var result = ContainerNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.InvalidCategoryIdentifier");
    }

    [Theory]
    [InlineData("CSQUA054383")]
    [InlineData("CSQU30543A3")]
    public void Create_WhenSerialNumberContainsNonDigit_ShouldReturnInvalidSerialNumberError(string input)
    {
        var result = ContainerNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.InvalidSerialNumber");
    }

    [Fact]
    public void Create_WhenCheckDigitIsNonDigit_ShouldReturnInvalidCheckDigitError()
    {
        var result = ContainerNumber.Create("CSQU305438A");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.InvalidCheckDigit");
    }

    [Fact]
    public void Create_WhenCheckDigitMismatch_ShouldReturnCheckDigitMismatchError()
    {
        // Valid is 3, pass 9
        var result = ContainerNumber.Create("CSQU3054389");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContainerNumber.CheckDigitMismatch");
    }

    [Fact]
    public void Create_WhenRemainderIs10_ShouldAcceptBothXAndZero()
    {
        // "RSKU123456" has Modulo 11 remainder = 10
        var resultWithX = ContainerNumber.Create("RSKU123456X");
        resultWithX.IsSuccess.Should().BeTrue();
        resultWithX.Value!.CheckDigit.Should().Be(10);

        var resultWithZero = ContainerNumber.Create("RSKU1234560");
        resultWithZero.IsSuccess.Should().BeTrue();
        resultWithZero.Value!.CheckDigit.Should().Be(0);

        var resultWithWrongDigit = ContainerNumber.Create("RSKU1234561");
        resultWithWrongDigit.IsFailure.Should().BeTrue();
        resultWithWrongDigit.Error.Code.Should().Be("ContainerNumber.CheckDigitMismatch");
    }
}
