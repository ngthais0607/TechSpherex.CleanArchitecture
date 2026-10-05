namespace SnpDepotManagement.Domain.Common.Rules;

public sealed class DeliveryOrderNotExpiredRule : IBusinessRule
{
    private readonly DateTime _expiryDate;
    private readonly DateTime _utcNow;

    public DeliveryOrderNotExpiredRule(DateTime expiryDate, DateTime utcNow)
    {
        _expiryDate = expiryDate;
        _utcNow = utcNow;
    }

    public Error Error => Error.Validation("Order.Expired", "Delivery order has expired.");
    public string Message => Error.Description;
    public bool IsBroken() => _utcNow.Date > _expiryDate.Date;
}
