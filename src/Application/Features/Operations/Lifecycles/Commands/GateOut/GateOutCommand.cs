using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed record GateOutCommand(
    Guid ContainerId,
    Guid DeliveryOrderId,
    string OutVehicleNumber,
    VehicleType VehicleType = VehicleType.Truck) : ICommand;
