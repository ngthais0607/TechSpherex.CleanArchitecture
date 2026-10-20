namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record DailyGateTrafficDto(
    string Date,
    int GateInCount,
    int GateOutCount,
    int TotalCount);
