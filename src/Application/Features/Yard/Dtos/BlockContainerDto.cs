namespace SnpDepotManagement.Application.Features.Yard.Dtos;

public sealed record BlockContainerDto(
    Guid Id,
    string Number,
    string IsoCode,
    string Size,
    string Type,
    string Grade,
    string Condition,
    string OwnerCode,
    string? OrderNumber,
    DateTime? OrderExpiry);
