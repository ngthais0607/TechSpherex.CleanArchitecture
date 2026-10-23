using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Domain.Entities.Yard;

public static class YardBlockLimits
{
    public const int MaxBays = 100;
    public const int MaxRows = 30;
    public const int MaxTiers = 6;
    public const int MaxSlots = 20_000;

    public static Error InvalidSize => Error.Validation("Validation.Failed",
        $"Yard block dimensions must be between zero and {MaxBays} bays, {MaxRows} rows, {MaxTiers} tiers, with at most {MaxSlots} slots.");

    public static bool IsWithinLimits(int bays, int rows, int tiers) =>
        bays is >= 0 and <= MaxBays && rows is >= 0 and <= MaxRows && tiers is >= 0 and <= MaxTiers
        // Validate dimensions first, so even int.MaxValue inputs cannot overflow this product.
        && ((long)bays + 1) / 2 * rows * tiers <= MaxSlots;
}

internal sealed record YardBlockSizeRule(int Bays, int Rows, int Tiers) : IBusinessRule
{
    public Error Error => YardBlockLimits.InvalidSize;
    public bool IsBroken() => !YardBlockLimits.IsWithinLimits(Bays, Rows, Tiers);
}
