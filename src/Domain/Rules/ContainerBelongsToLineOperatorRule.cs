using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Domain.Rules;

public sealed class ContainerBelongsToLineOperatorRule : IBusinessRule
{
    private readonly string _containerLineOperator;
    private readonly string _orderLineOperator;

    public ContainerBelongsToLineOperatorRule(string containerLineOperator, string orderLineOperator)
    {
        _containerLineOperator = containerLineOperator;
        _orderLineOperator = orderLineOperator;
    }

    public Error Error => Error.Conflict(
        "OrderRule.LineOperatorMismatch",
        $"Container line operator '{_containerLineOperator}' does not match delivery order line operator '{_orderLineOperator}'.");

    public string Message => Error.Description;

    public bool IsBroken()
    {
        if (string.IsNullOrWhiteSpace(_containerLineOperator) || string.IsNullOrWhiteSpace(_orderLineOperator))
        {
            return true;
        }

        return !string.Equals(
            _containerLineOperator.Trim(),
            _orderLineOperator.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
