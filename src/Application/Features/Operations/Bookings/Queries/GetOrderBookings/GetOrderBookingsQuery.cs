using SnpDepotManagement.Application.Abstractions.Messaging;
namespace SnpDepotManagement.Application.Features.Operations.Bookings.Queries.GetOrderBookings;
public sealed record GetOrderBookingsQuery(string OrderNumber, int PageIndex = 1, int PageSize = 8) : IQuery<OrderBookingsDto>;
