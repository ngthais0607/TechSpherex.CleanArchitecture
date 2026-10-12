using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.ShrinkYardBlock;

public sealed record ShrinkYardBlockCommand(
    Guid BlockId,
    int NewBays,
    int NewRows,
    int NewTiers) : ICommand;
