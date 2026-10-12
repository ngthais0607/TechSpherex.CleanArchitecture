using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

public sealed class GateInCommandHandler : ICommandHandler<GateInCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public GateInCommandHandler(IAppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(GateInCommand command, CancellationToken cancellationToken = default)
    {
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == command.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("Container.NotFound", "Container not found."));
        }

        var slot = await _context.YardSlots
            .FirstOrDefaultAsync(s => s.Id == command.SlotId, cancellationToken);

        if (slot is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("YardSlot.NotFound", "Yard slot not found."));
        }

        if (slot.IsOccupied)
        {
            return Result.Failure<Guid>(
                Error.Conflict("YardSlot.SlotAlreadyOccupied", "Yard slot is already occupied."));
        }

        var activeLifecycle = await _context.ContainerLifecycles
            .FirstOrDefaultAsync(l => l.ContainerId == command.ContainerId && l.Status == LifecycleStatus.InYard, cancellationToken);

        if (activeLifecycle is not null)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Lifecycle.ContainerAlreadyInYard", "Container is already marked as in yard."));
        }

        // Occupy slot
        slot.Occupy(container.Id);

        // Create lifecycle record
        var lifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            command.Classification,
            command.Condition,
            command.InVehicleNumber,
            command.SlotId,
            _timeProvider.GetUtcNow().UtcDateTime);

        _context.ContainerLifecycles.Add(lifecycle);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(lifecycle.Id);
    }
}
