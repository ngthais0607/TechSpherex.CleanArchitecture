using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Gate.Evaluator;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Gate.Queries.ValidateGate;

public sealed record ValidateGateQuery(
    string ContainerNumber,
    GateTransactionType Type,
    string? OrderNumber = null,
    string? BlockCode = null,
    int? Bay = null,
    int? Row = null,
    int? Tier = null,
    string? VirtualBlockCode = null
) : IQuery<ValidateGateResponseDto>;

public sealed record ValidateGateResponseDto(
    IReadOnlyList<RuleCheckResult> Checks,
    ContainerGateInfoDto? Container,
    OrderGateInfoDto? Order
);

public sealed record ContainerGateInfoDto(
    Guid Id,
    string ContainerNumber,
    string IsoCode,
    string Size,
    string Type,
    string OwnerCode,
    string DisplayStatus,
    string? CurrentLocation,
    string? CurrentBookingOrder
);

public sealed record OrderGateInfoDto(
    Guid Id,
    string OrderNumber,
    string LineOperator,
    string CustomerName,
    DateTime ExpiryDate,
    IReadOnlyList<OrderItemGateDto> Items
);

public sealed record OrderItemGateDto(
    Guid Id,
    string Size,
    string Type,
    int Quantity,
    int ReservedQuantity,
    int ShippedQuantity,
    int RemainingQuantity
);
