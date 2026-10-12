using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Application.Abstractions.Rules;

public interface IRuleEngine
{
    RuleResult Evaluate(params IBusinessRule[] rules);
    RuleResult Evaluate(IEnumerable<IBusinessRule> rules);
}
