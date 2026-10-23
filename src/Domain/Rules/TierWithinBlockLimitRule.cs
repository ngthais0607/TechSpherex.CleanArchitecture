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
        $"Tầng '{_tier}' vượt quá giới hạn tầng tối đa '{_maxTiers}' của dãy.");

    public string Message => Error.Description;

    public bool IsBroken() => _tier <= 0 || _tier > _maxTiers;
}
