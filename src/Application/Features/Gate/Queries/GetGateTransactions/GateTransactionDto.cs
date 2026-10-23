namespace SnpDepotManagement.Application.Features.Gate.Queries.GetGateTransactions;

public sealed record GateTransactionDto(
    Guid Id,
    DateTime OccurredAt,
    string Type,
    string Status,
    string ContainerNumber,
    string? Size,
    string? ContainerType,
    string VehicleNumber,
    string VehicleType,
    string? DriverName,
    string? Position,
    string? OrderNumber,
    string? CreatedBy,
    DateTime? CancelledAt,
    string? CancelledBy,
    IReadOnlyList<GateTransactionChangeDto> Changes
);

public sealed record GateTransactionChangeDto(
    Guid Id,
    string FieldName,
    string? OldValue,
    string? NewValue,
    DateTime ChangedAt,
    string? ChangedBy
);
