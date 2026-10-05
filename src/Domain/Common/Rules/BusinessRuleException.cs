namespace SnpDepotManagement.Domain.Common.Rules;

public sealed class BusinessRuleException : Exception
{
    public IBusinessRule BrokenRule { get; }
    public Error Error => BrokenRule.Error;

    public BusinessRuleException(IBusinessRule brokenRule)
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }

    public override string ToString() => $"{BrokenRule.GetType().FullName}: {BrokenRule.Message}";
}
