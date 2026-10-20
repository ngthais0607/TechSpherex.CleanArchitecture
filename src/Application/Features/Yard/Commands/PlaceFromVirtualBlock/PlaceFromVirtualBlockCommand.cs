using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.PlaceFromVirtualBlock;

public sealed record PlaceFromVirtualBlockCommand(
    string VirtualBlockCode,
    Guid ContainerId,
    string TargetBlockCode,
    int Bay,
    int Row,
    int Tier) : ICommand;
