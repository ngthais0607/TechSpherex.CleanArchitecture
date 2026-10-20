using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public static class ContainerNumberGenerator
{
    public static int CalculateRemainder(string tenChars)
    {
        if (string.IsNullOrEmpty(tenChars) || tenChars.Length < 10)
            throw new ArgumentException("Must provide at least 10 characters", nameof(tenChars));

        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = char.ToUpperInvariant(tenChars[i]);
            int charValue = char.IsLetter(c) ? GetLetterValue(c) : (c - '0');
            sum += charValue * (1 << i);
        }

        return sum % 11;
    }

    public static char ComputeCheckDigitChar(string ownerCode, string categoryId, string serial, bool useXForTen = true)
    {
        var ten = (ownerCode + categoryId + serial).ToUpperInvariant();
        int rem = CalculateRemainder(ten);
        if (rem == 10)
            return useXForTen ? 'X' : '0';
        return (char)('0' + rem);
    }

    public static string Generate(string ownerCode = "MSK", string category = "U", bool useXForTen = false)
    {
        ownerCode = ownerCode.PadRight(3, 'A').Substring(0, 3).ToUpperInvariant();
        category = string.IsNullOrWhiteSpace(category) ? "U" : category.Substring(0, 1).ToUpperInvariant();

        while (true)
        {
            var serial = Random.Shared.Next(100000, 999999).ToString();
            var checkChar = ComputeCheckDigitChar(ownerCode, category, serial, useXForTen);
            var num = $"{ownerCode}{category}{serial}{checkChar}";
            var res = ContainerNumber.Create(num);
            if (res.IsSuccess)
                return num;
        }
    }

    private static int GetLetterValue(char letter)
    {
        int index = letter - 'A' + 10;
        if (index >= 11) index++;
        if (index >= 22) index++;
        if (index >= 33) index++;
        return index;
    }
}
