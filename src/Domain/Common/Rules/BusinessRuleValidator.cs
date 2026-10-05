namespace SnpDepotManagement.Domain.Common.Rules;

public static class BusinessRuleValidator
{
    public static void Check(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleException(rule);
        }
    }

    public static void Validate(params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleException(rule);
            }
        }
    }

    public static void Validate(IEnumerable<IBusinessRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleException(rule);
            }
        }
    }
}
