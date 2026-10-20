using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.Lifecycles;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Gate.Commands.CancelTransaction;

public sealed class CancelTransactionCommandHandler : ICommandHandler<CancelTransactionCommand>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;

    public CancelTransactionCommandHandler(
        IAppDbContext context,
        TimeProvider timeProvider,
        ICurrentUser? currentUser = null)
    {
        _context = context;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var gateTx = await _context.GateTransactions
            .Include(t => t.Changes)
            .FirstOrDefaultAsync(t => t.Id == command.TransactionId, cancellationToken);

        if (gateTx is null)
        {
            return Result.Failure(
                Error.NotFound("GateTransaction.NotFound", "Không tìm thấy giao dịch cổng."));
        }

        if (gateTx.Status == GateTransactionStatus.Cancelled)
        {
            return Result.Failure(
                Error.Conflict("GateTransaction.AlreadyCancelled", "Giao dịch cổng đã được hủy trước đó."));
        }

        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == gateTx.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Không tìm thấy container."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (gateTx.Type == GateTransactionType.GateIn)
        {
            var lifecycle = await _context.ContainerLifecycles
                .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.InYard, cancellationToken);

            if (lifecycle is null)
            {
                return Result.Failure(
                    Error.Conflict("Lifecycle.NotInYard", "Container hiện không có trong bãi, không thể hủy nhập cổng."));
            }

            // Giải phóng vị trí bãi nếu có
            if (lifecycle.CurrentSlotId.HasValue)
            {
                var occupiedBlockIds = await _context.YardSlots.Where(s => s.ContainerId == container.Id)
                    .Select(s => s.BlockId).Distinct().ToListAsync(cancellationToken);
                var occupiedBlocks = await _context.YardBlocks.Include(b => b.Slots)
                    .Where(b => occupiedBlockIds.Contains(b.Id)).ToListAsync(cancellationToken);
                var occupiedSlots = occupiedBlocks.SelectMany(b => b.Slots).Where(s => s.ContainerId == container.Id).ToList();

                foreach (var occupied in occupiedSlots)
                {
                    var hasContainerAbove = occupiedBlocks.Single(b => b.Id == occupied.BlockId).Slots.Any(s =>
                        s.Bay == occupied.Bay && s.Row == occupied.Row && s.Tier > occupied.Tier && s.IsOccupied);
                    if (hasContainerAbove)
                        return Result.Failure(Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Có container đè phía trên, cần đảo chuyển trước khi hủy nhập cổng."));
                }

                foreach (var s in occupiedSlots)
                {
                    s.Release();
                }
            }

            lifecycle.CancelGateIn(utcNow);

            MovementHistory.Append(_context, lifecycle, ContainerMovementType.GateInCancelled, utcNow, _currentUser?.UserId,
                null, null, gateTx.VehicleNumber, gateTx.VehicleType);
        }
        else if (gateTx.Type == GateTransactionType.GateOut)
        {
            var lifecycle = await _context.ContainerLifecycles
                .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.GatedOut, cancellationToken);

            if (lifecycle is null)
            {
                return Result.Failure(
                    Error.Conflict("Lifecycle.ContainerAlreadyInYard", "Container hiện không ở trạng thái đã xuất bãi."));
            }

            // Phục hồi vị trí bãi
            if (gateTx.SlotId.HasValue)
            {
                var targetSlot = await _context.YardSlots
                    .FirstOrDefaultAsync(s => s.Id == gateTx.SlotId.Value, cancellationToken);

                if (targetSlot is not null)
                {
                    if (targetSlot.IsOccupied)
                    {
                        return Result.Failure(
                            Error.Conflict("G9.SlotOccupied", "Vị trí bãi ban đầu đã bị container khác chiếm chỗ."));
                    }

                    var targetBlock = await _context.YardBlocks.Include(b => b.Slots)
                        .FirstAsync(b => b.Id == targetSlot.BlockId, cancellationToken);

                    foreach (var support in targetBlock.Slots.Where(s => s.Row == targetSlot.Row && s.Tier == targetSlot.Tier &&
                                 (s.Bay == targetSlot.Bay || (container.Size == ContainerSize.Ft40 && s.Bay == targetSlot.Bay + 2))))
                    {
                        support.Occupy(container.Id);
                    }

                    lifecycle.CancelGateOut(targetSlot.Id, null);
                }
                else
                {
                    lifecycle.CancelGateOut(null, null);
                }
            }
            else if (!string.IsNullOrWhiteSpace(gateTx.VirtualBlockCode))
            {
                var vBlock = await _context.YardBlocks
                    .FirstOrDefaultAsync(b => b.Code == gateTx.VirtualBlockCode && b.IsVirtual, cancellationToken);
                lifecycle.CancelGateOut(null, vBlock?.Id);
            }
            else
            {
                lifecycle.CancelGateOut(null, null);
            }

            // Phục hồi lệnh giao & booking
            if (gateTx.OrderId.HasValue)
            {
                var order = await _context.DeliveryOrders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == gateTx.OrderId.Value, cancellationToken);

                if (order is not null)
                {
                    var targetItem = order.Items.FirstOrDefault(i => i.Size == container.Size && i.Type == container.Type);
                    targetItem?.Deallocate(1);
                    order.Reopen();
                }

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.ContainerId == container.Id && b.OrderId == gateTx.OrderId.Value && b.Status == BookingStatus.Shipped, cancellationToken);

                if (booking is not null)
                {
                    booking.RestoreReserved();
                }
            }

            MovementHistory.Append(_context, lifecycle, ContainerMovementType.GateOutCancelled, utcNow, _currentUser?.UserId,
                null, null, gateTx.VehicleNumber, gateTx.VehicleType, gateTx.OrderId);
        }

        gateTx.Cancel(utcNow, _currentUser?.UserId?.ToString());

        if (!string.IsNullOrWhiteSpace(command.Reason))
        {
            gateTx.RecordChange("CancelReason", null, command.Reason.Trim(), utcNow, _currentUser?.UserId?.ToString());
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
