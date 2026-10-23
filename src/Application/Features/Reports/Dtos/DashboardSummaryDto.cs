namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record DashboardSummaryDto(
    int TotalContainersInYard,
    int TotalTeuInYard,
    int ActiveOrdersCount,
    int TodayGateTransactionsCount);
