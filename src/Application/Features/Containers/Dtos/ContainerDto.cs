using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Dtos;

public sealed record ContainerDto(
    Guid Id,
    string ContainerNumber,
    string IsoCode,
    ContainerSize Size,
    ContainerType Type,
    decimal TareWeight,
    decimal MaxGrossWeight,
    string OwnerCode,
    ContainerCondition Condition,
    Classification Classification,
    DateTime? DateOfManufacture,
    string DisplayStatus = "NeverIn",
    SlotDto? CurrentSlot = null,
    BookingSummaryDto? CurrentBooking = null,
    bool IsInYard = false);

public sealed record SlotDto(string BlockCode, int Bay, int Row, int Tier);

public sealed record BookingSummaryDto(string OrderNumber, DateTime OrderExpiry);
