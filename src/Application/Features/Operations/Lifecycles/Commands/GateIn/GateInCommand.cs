using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

public sealed record GateInCommand : ICommand<Guid>
{
    public Guid? ContainerId { get; init; }
    public string? ContainerNumber { get; init; }
    public string? InVehicleNumber { get; init; }
    public string? VehicleNumber { get; init; }
    public Guid? SlotId { get; init; }
    public string? BlockCode { get; init; }
    public int? Bay { get; init; }
    public int? Row { get; init; }
    public int? Tier { get; init; }
    public string? VirtualBlockCode { get; init; }
    public string? DriverName { get; init; }
    public Classification Classification { get; init; } = Classification.A;
    public Classification? Grade { get; init; }
    public ContainerCondition Condition { get; init; } = ContainerCondition.Normal;
    public VehicleType VehicleType { get; init; } = VehicleType.Truck;

    public GateInCommand() { }

    public GateInCommand(
        Guid containerId,
        string inVehicleNumber,
        Guid slotId,
        Classification classification = Classification.A,
        ContainerCondition condition = ContainerCondition.Normal,
        VehicleType vehicleType = VehicleType.Truck)
    {
        ContainerId = containerId;
        InVehicleNumber = inVehicleNumber;
        SlotId = slotId;
        Classification = classification;
        Condition = condition;
        VehicleType = vehicleType;
    }

    public string EffectiveVehicleNumber => (!string.IsNullOrWhiteSpace(VehicleNumber) ? VehicleNumber : InVehicleNumber) ?? string.Empty;
    public Classification EffectiveGrade => Grade ?? Classification;
}
