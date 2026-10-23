using SnpDepotManagement.Domain.Common;
namespace SnpDepotManagement.Application.Features.Operations.Bookings.Queries.GetOrderBookings;
public sealed record OrderBookingsDto(PagedResult<OrderBookingDto> Bookings, IReadOnlyList<OrderItemBookingCountsDto> ItemCounts);
public sealed record OrderBookingDto(Guid Id, Guid ItemId, string ContainerNumber, string Status, DateTime ReservedAt, DateTime? ShippedAt);
public sealed record OrderItemBookingCountsDto(Guid ItemId, int ReservedQuantity, int ShippedQuantity, int AvailableQuantity);
