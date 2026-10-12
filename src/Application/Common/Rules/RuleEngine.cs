using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Application.Common.Rules;

public sealed class RuleEngine : IRuleEngine
{
    public RuleResult Evaluate(params IBusinessRule[] rules) => Evaluate((IEnumerable<IBusinessRule>)rules);

    public RuleResult Evaluate(IEnumerable<IBusinessRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.IsBroken())
            {
                return RuleResult.Failure(rule.Error);
            }
        }

        return RuleResult.Success();
    }
}
