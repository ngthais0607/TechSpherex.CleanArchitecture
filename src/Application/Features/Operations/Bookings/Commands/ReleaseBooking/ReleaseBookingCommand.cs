using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Operations.Bookings.Commands.ReleaseBooking;

public sealed record ReleaseBookingCommand(Guid Id) : ICommand;
