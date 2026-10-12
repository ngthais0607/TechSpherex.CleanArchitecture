using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;

public static class ContainerTestHelper
{
    public static ContainerNumber CreateValidContainerNumber(string prefix = "MSKU", int serial = 100000)
    {
        string tenChars = $"{prefix}{serial:D6}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = tenChars[i];
            int charValue = char.IsLetter(c) ? GetLetterValue(c) : (c - '0');
            sum += charValue * (1 << i);
        }

        int remainder = sum % 11;
        int checkDigit = remainder == 10 ? 0 : remainder;

        string fullNumber = $"{tenChars}{checkDigit}";
        var result = ContainerNumber.Create(fullNumber);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create valid container number for '{fullNumber}': {result.Error.Description}");
        }

        return result.Value!;
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
