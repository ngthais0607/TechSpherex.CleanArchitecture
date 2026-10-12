using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;

public sealed record RemoveContainerCommand(
    Guid BlockId,
    int Bay,
    int Row,
    int Tier) : ICommand;
