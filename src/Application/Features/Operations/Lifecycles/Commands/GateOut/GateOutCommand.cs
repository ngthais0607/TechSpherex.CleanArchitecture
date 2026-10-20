using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed record GateOutCommand : ICommand
{
    public Guid? ContainerId { get; init; }
    public string? ContainerNumber { get; init; }
    public Guid? DeliveryOrderId { get; init; }
    public string? OrderNumber { get; init; }
    public string? OutVehicleNumber { get; init; }
    public string? VehicleNumber { get; init; }
    public string? DriverName { get; init; }
    public VehicleType VehicleType { get; init; } = VehicleType.Truck;

    public GateOutCommand() { }

    public GateOutCommand(
        Guid containerId,
        Guid deliveryOrderId,
        string outVehicleNumber,
        VehicleType vehicleType = VehicleType.Truck)
    {
        ContainerId = containerId;
        DeliveryOrderId = deliveryOrderId;
        OutVehicleNumber = outVehicleNumber;
        VehicleType = vehicleType;
    }

    public string EffectiveVehicleNumber => (!string.IsNullOrWhiteSpace(VehicleNumber) ? VehicleNumber : OutVehicleNumber) ?? string.Empty;
}
