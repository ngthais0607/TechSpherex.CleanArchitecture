namespace SnpDepotManagement.Application.Features.Gate.Evaluator;

public sealed class GateRuleEvaluator
{
    public List<RuleCheckResult> Evaluate(IEnumerable<(string code, string label, Func<RuleState> check, string? detail)> rules)
    {
        return rules.Select(r =>
        {
            try
            {
                var state = r.check();
                return new RuleCheckResult(r.code, state, r.label, state == RuleState.Pass ? null : r.detail);
            }
            catch (Exception ex)
            {
                return new RuleCheckResult(r.code, RuleState.Fail, r.label, ex.Message);
            }
        }).ToList();
    }
}
