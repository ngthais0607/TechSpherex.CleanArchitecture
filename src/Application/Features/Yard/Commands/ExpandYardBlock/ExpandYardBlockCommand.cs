using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.ExpandYardBlock;

public sealed record ExpandYardBlockCommand(
    Guid BlockId,
    int NewBays,
    int NewRows,
    int NewTiers) : ICommand;
