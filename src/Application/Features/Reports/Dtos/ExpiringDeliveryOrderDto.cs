namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record ExpiringDeliveryOrderDto(Guid Id, string OrderNumber, string LineOperator,
    string CustomerName, DateTime ExpiryDate, int DaysUntilExpiry, int RemainingQuantity);
