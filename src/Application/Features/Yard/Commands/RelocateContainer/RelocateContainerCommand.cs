using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;

public sealed record RelocateContainerCommand(
    Guid BlockId,
    int SourceBay,
    int SourceRow,
    int SourceTier,
    int TargetBay,
    int TargetRow,
    int TargetTier) : ICommand;
