using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Domain.Rules;

public sealed class ContainerBelongsToLineOperatorRule : IBusinessRule
{
    private readonly string? _resolvedLineOperator;
    private readonly string _orderLineOperator;

    public ContainerBelongsToLineOperatorRule(string? resolvedLineOperator, string orderLineOperator)
    {
        _resolvedLineOperator = resolvedLineOperator;
        _orderLineOperator = orderLineOperator;
    }

    public Error Error => Error.Validation(
        "OrderRule.LineOperatorMismatch",
        "Container không thuộc hãng khai thác trong lệnh giao.");

    public string Message => Error.Description;

    public bool IsBroken()
    {
        if (string.IsNullOrWhiteSpace(_resolvedLineOperator) || string.IsNullOrWhiteSpace(_orderLineOperator))
        {
            return true;
        }

        return !string.Equals(
            _resolvedLineOperator.Trim(),
            _orderLineOperator.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
