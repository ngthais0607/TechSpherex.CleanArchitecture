using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

public sealed record GateInCommand(
    Guid ContainerId,
    string InVehicleNumber,
    Guid SlotId,
    Classification Classification = Classification.A,
    ContainerCondition Condition = ContainerCondition.Normal) : ICommand<Guid>;
