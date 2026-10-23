using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Gate.Commands.UpdateTransaction;

public sealed class UpdateTransactionCommandHandler : ICommandHandler<UpdateTransactionCommand>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;

    public UpdateTransactionCommandHandler(
        IAppDbContext context,
        TimeProvider timeProvider,
        ICurrentUser? currentUser = null)
    {
        _context = context;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateTransactionCommand command, CancellationToken cancellationToken = default)
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
                Error.Conflict("GateTransaction.CancelledCannotUpdate", "Không thể chỉnh sửa giao dịch đã hủy."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = _currentUser?.UserId?.ToString();

        // Cập nhật thông tin xe / tài xế
        if (!string.IsNullOrWhiteSpace(command.VehicleNumber) || command.VehicleType.HasValue || command.DriverName is not null)
        {
            var newVehicleNo = !string.IsNullOrWhiteSpace(command.VehicleNumber) ? command.VehicleNumber : gateTx.VehicleNumber;
            var newVehicleType = command.VehicleType ?? gateTx.VehicleType;
            var newDriver = command.DriverName ?? gateTx.DriverName;

            gateTx.UpdateDetails(newVehicleNo, newVehicleType, newDriver, utcNow, userId);

            // Cập nhật lifecycle nếu container đang InYard
            var lifecycle = await _context.ContainerLifecycles
                .FirstOrDefaultAsync(l => l.ContainerId == gateTx.ContainerId && l.Status == LifecycleStatus.InYard, cancellationToken);
            if (lifecycle is not null && gateTx.Type == GateTransactionType.GateIn)
            {
                // Giữ InVehicleNumber khớp
            }
        }

        // Cập nhật Grade và Condition trên container và lifecycle
        if (command.Grade.HasValue || command.Condition.HasValue)
        {
            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.Id == gateTx.ContainerId, cancellationToken);

            if (container is not null)
            {
                if (command.Grade.HasValue && container.Classification != command.Grade.Value)
                {
                    gateTx.RecordChange("Grade", container.Classification.ToString(), command.Grade.Value.ToString(), utcNow, userId);
                }

                if (command.Condition.HasValue && container.Condition != command.Condition.Value)
                {
                    gateTx.RecordChange("Condition", container.Condition.ToString(), command.Condition.Value.ToString(), utcNow, userId);
                }

                container.UpdateConditionAndClassification(command.Condition, command.Grade);

                var lifecycle = await _context.ContainerLifecycles
                    .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.InYard, cancellationToken);
                lifecycle?.UpdateConditionAndClassification(command.Condition, command.Grade);
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            gateTx.RecordChange("Note", null, command.Note.Trim(), utcNow, userId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
