using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Operations;

public sealed class GateTransactionChange : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? ChangedBy { get; private set; }

    private GateTransactionChange() { }

    public static GateTransactionChange Create(
        Guid transactionId,
        string fieldName,
        string? oldValue,
        string? newValue,
        DateTime utcNow,
        string? changedBy = null)
    {
        return new GateTransactionChange
        {
            TransactionId = transactionId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = utcNow,
            ChangedBy = changedBy
        };
    }
}
