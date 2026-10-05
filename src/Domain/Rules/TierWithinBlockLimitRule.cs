using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Domain.Rules;

public sealed class TierWithinBlockLimitRule : IBusinessRule
{
    private readonly int _tier;
    private readonly int _maxTiers;

    public TierWithinBlockLimitRule(int tier, int maxTiers)
    {
        _tier = tier;
        _maxTiers = maxTiers;
    }

    public Error Error => Error.Validation(
        "YardRule.TierExceedsBlockLimit",
        $"Tier '{_tier}' exceeds the maximum allowed tier '{_maxTiers}' for this block.");

    public string Message => Error.Description;

    public bool IsBroken() => _tier <= 0 || _tier > _maxTiers;
}
