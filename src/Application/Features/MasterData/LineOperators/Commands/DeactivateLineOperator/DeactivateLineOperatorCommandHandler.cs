using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.DeactivateLineOperator;

public sealed class DeactivateLineOperatorCommandHandler : ICommandHandler<DeactivateLineOperatorCommand>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public DeactivateLineOperatorCommandHandler(IAppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeactivateLineOperatorCommand command, CancellationToken cancellationToken = default)
    {
        var lineOp = await _context.LineOperators
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);

        if (lineOp is null)
        {
            return Result.Failure(Error.NotFound("LineOperator.NotFound", "Không tìm thấy hãng khai thác."));
        }

        // Chặn nếu còn container trong bãi hoặc lệnh hiệu lực -> 409
        var hasContainersInYard = await _context.ContainerLifecycles
            .AnyAsync(lc => lc.Status == LifecycleStatus.InYard && lc.LineOperator == lineOp.Code, cancellationToken);

        if (hasContainersInYard)
        {
            return Result.Failure(Error.Conflict(
                "LineOperator.HasActiveResources",
                "Không thể ngưng hoạt động hãng tàu còn container trong bãi."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var hasActiveOrders = await _context.DeliveryOrders
            .AnyAsync(o => o.LineOperator == lineOp.Code && !o.IsCompleted && o.ExpiryDate >= utcNow, cancellationToken);

        if (hasActiveOrders)
        {
            return Result.Failure(Error.Conflict(
                "LineOperator.HasActiveResources",
                "Không thể ngưng hoạt động hãng tàu còn lệnh giao đang hiệu lực."));
        }

        lineOp.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
