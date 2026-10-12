using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;

public sealed record PlaceContainerCommand(
    Guid BlockId,
    int Bay,
    int Row,
    int Tier,
    Guid ContainerId) : ICommand;
