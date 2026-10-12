using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Commands.CreateVoyage;

public sealed class CreateVoyageCommandHandler : ICommandHandler<CreateVoyageCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateVoyageCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateVoyageCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedVessel = command.VesselName.Trim().ToUpperInvariant();
        var normalizedVoyage = command.VoyageNumber.Trim().ToUpperInvariant();

        var exists = await _context.Voyages
            .AnyAsync(v => v.VesselName == normalizedVessel && v.VoyageNumber == normalizedVoyage, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Voyage.AlreadyExists", $"Voyage '{normalizedVoyage}' for vessel '{normalizedVessel}' already exists."));
        }

        var voyageResult = Voyage.Create(normalizedVessel, normalizedVoyage, command.Etd);
        if (voyageResult.IsFailure)
        {
            return Result.Failure<Guid>(voyageResult.Error);
        }

        var voyage = voyageResult.Value!;
        _context.Voyages.Add(voyage);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(voyage.Id);
    }
}
