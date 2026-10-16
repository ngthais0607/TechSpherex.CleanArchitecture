using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Domain.Rules;

public sealed class DeliveryOrderNotExpiredRule : IBusinessRule
{
    private readonly DateTime _expiryDate;
    private readonly DateTime _dispatchDate;

    public DeliveryOrderNotExpiredRule(DateTime expiryDate, DateTime dispatchDate)
    {
        _expiryDate = expiryDate;
        _dispatchDate = dispatchDate;
    }

    public Error Error => Error.Validation(
        "OrderRule.DeliveryOrderExpired",
        "Delivery order has expired and is no longer valid for dispatch.");

    public string Message => Error.Description;

    public bool IsBroken() => DepotCalendar.GetBusinessDate(_dispatchDate) > DepotCalendar.GetBusinessDate(_expiryDate);
}
