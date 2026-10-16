using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Features.Operations.Lifecycles;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;

public sealed class RemoveContainerCommandHandler : ICommandHandler<RemoveContainerCommand>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;

    public RemoveContainerCommandHandler(IAppDbContext context, TimeProvider? timeProvider = null, ICurrentUser? currentUser = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveContainerCommand command, CancellationToken cancellationToken = default)
    {
        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        bool is40Ft = command.Bay % 2 == 0;
        var slotsToRelease = new List<YardSlot>();

        if (!is40Ft)
        {
            var slot = block.Slots.FirstOrDefault(s => s.Bay == command.Bay && s.Row == command.Row && s.Tier == command.Tier);
            if (slot is null || !slot.IsOccupied)
            {
                return Result.Failure(
                    Error.NotFound("YardSlot.SlotNotOccupied", "No container found at the specified yard slot."));
            }

            // Check if there is a container directly on top
            bool hasContainerAbove = block.Slots.Any(s =>
                s.Bay == command.Bay &&
                s.Row == command.Row &&
                s.Tier == command.Tier + 1 &&
                s.IsOccupied);

            if (hasContainerAbove)
            {
                return Result.Failure(
                    Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Cannot remove container because another container is stacked on top of it."));
            }

            slotsToRelease.Add(slot);
        }
        else
        {
            int bayBefore = command.Bay - 1;
            int bayAfter = command.Bay + 1;

            var slot1 = block.Slots.FirstOrDefault(s => s.Bay == bayBefore && s.Row == command.Row && s.Tier == command.Tier);
            var slot2 = block.Slots.FirstOrDefault(s => s.Bay == bayAfter && s.Row == command.Row && s.Tier == command.Tier);

            if (slot1 is null || slot2 is null || !slot1.IsOccupied || !slot2.IsOccupied)
            {
                return Result.Failure(
                    Error.NotFound("YardSlot.SlotNotOccupied", "No container found at the specified 40ft yard position."));
            }

            // Check if there is any container on top of either slot
            bool hasContainerAbove = block.Slots.Any(s =>
                (s.Bay == bayBefore || s.Bay == bayAfter) &&
                s.Row == command.Row &&
                s.Tier == command.Tier + 1 &&
                s.IsOccupied);

            if (hasContainerAbove)
            {
                return Result.Failure(
                    Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Cannot remove container because another container is stacked on top of it."));
            }

            slotsToRelease.Add(slot1);
            slotsToRelease.Add(slot2);
        }

        var containerId = slotsToRelease[0].ContainerId;
        var container = await _context.Containers.FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);
        if (container is null) return Result.Failure(Error.NotFound("Container.NotFound", "Container not found."));
        if (is40Ft != (container.Size == ContainerSize.Ft40) || slotsToRelease.Any(s => s.ContainerId != container.Id))
            return Result.Failure(Error.Conflict("YardSlot.InvalidFootprint", "The source slots do not represent one container of the requested size."));
        var lifecycle = await _context.ContainerLifecycles.FirstOrDefaultAsync(
            l => l.ContainerId == containerId && l.Status == LifecycleStatus.InYard, cancellationToken);
        if (lifecycle is null)
            return Result.Failure(Error.Conflict("Lifecycle.NotInYard", "Container is not currently in the depot."));
        foreach (var slot in slotsToRelease) slot.Release();
        lifecycle.SetCurrentSlot(null);
        MovementHistory.Append(_context, lifecycle, ContainerMovementType.Remove, _timeProvider.GetUtcNow().UtcDateTime,
            _currentUser?.UserId, MovementHistory.Position(block, command.Bay, command.Row, command.Tier), null);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
