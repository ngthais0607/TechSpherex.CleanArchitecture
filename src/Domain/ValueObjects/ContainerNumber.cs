using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.ValueObjects;

public sealed record ContainerNumber
{
    public string Value { get; }
    public string OwnerCode => Value.Substring(0, 3);
    public char CategoryIdentifier => Value[3];
    public string SerialNumber => Value.Substring(4, 6);
    public int CheckDigit => Value[10] - '0';

    private ContainerNumber(string value) => Value = value;

    public static Result<ContainerNumber> Create(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.Empty", "Container number cannot be empty."));
        }

        var normalized = new string(rawInput.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

        if (normalized.Length != 11)
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.InvalidLength", "Container number must be exactly 11 characters."));
        }

        string ownerPart = normalized.Substring(0, 3);
        if (!ownerPart.All(c => c >= 'A' && c <= 'Z'))
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.InvalidOwnerCode", "Owner code must consist of 3 letters (A-Z)."));
        }

        char categoryChar = normalized[3];
        if (categoryChar != 'U' && categoryChar != 'J' && categoryChar != 'Z')
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.InvalidCategoryIdentifier", "Equipment category identifier must be U, J, or Z."));
        }

        string serialPart = normalized.Substring(4, 6);
        if (!serialPart.All(char.IsDigit))
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.InvalidSerialNumber", "Serial number must consist of 6 digits."));
        }

        char checkDigitChar = normalized[10];
        if (!char.IsDigit(checkDigitChar))
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.InvalidCheckDigit", "Check digit must be a digit (0-9)."));
        }

        int expectedCheckDigit = CalculateCheckDigit(normalized.Substring(0, 10));
        int actualCheckDigit = checkDigitChar - '0';

        if (expectedCheckDigit != actualCheckDigit)
        {
            return Result.Failure<ContainerNumber>(
                Error.Validation("ContainerNumber.CheckDigitMismatch", "Check digit does not match ISO 6346 Modulo 11 algorithm."));
        }

        return Result.Success(new ContainerNumber(normalized));
    }

    private static int CalculateCheckDigit(string tenChars)
    {
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = tenChars[i];
            int charValue = char.IsLetter(c) ? GetLetterValue(c) : (c - '0');
            sum += charValue * (1 << i);
        }

        int remainder = sum % 11;
        return remainder == 10 ? 0 : remainder;
    }

    private static int GetLetterValue(char letter)
    {
        int index = letter - 'A' + 10;
        if (index >= 11) index++;
        if (index >= 22) index++;
        if (index >= 33) index++;
        return index;
    }

    public override string ToString() => Value;
}
