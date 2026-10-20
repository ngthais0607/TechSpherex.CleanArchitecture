using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Application.Features.Operations.Bookings.Commands.ReleaseBooking;

public sealed class ReleaseBookingCommandHandler : ICommandHandler<ReleaseBookingCommand>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public ReleaseBookingCommandHandler(IAppDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result> Handle(ReleaseBookingCommand command, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(
                Error.NotFound("Booking.NotFound", "Không tìm thấy thông tin giữ chỗ container."));
        }

        if (booking.Status != BookingStatus.Reserved)
        {
            return Result.Failure(
                Error.Conflict("Booking.NotReserved", "Chỉ có thể giải phóng lượt giữ chỗ ở trạng thái Reserved."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        booking.Cancel(utcNow);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
