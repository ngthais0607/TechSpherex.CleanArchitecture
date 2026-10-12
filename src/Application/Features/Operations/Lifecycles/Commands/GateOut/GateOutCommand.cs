using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed record GateOutCommand(
    Guid ContainerId,
    Guid DeliveryOrderId,
    string OutVehicleNumber) : ICommand;
