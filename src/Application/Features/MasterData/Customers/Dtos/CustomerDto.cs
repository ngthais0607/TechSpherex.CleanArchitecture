namespace SnpDepotManagement.Application.Features.MasterData.Customers.Dtos;

public sealed record CustomerDto(
    Guid Id,
    string TaxCode,
    string Name,
    bool IsActive = true,
    int OrderCount = 0,
    int ActiveOrderCount = 0);
