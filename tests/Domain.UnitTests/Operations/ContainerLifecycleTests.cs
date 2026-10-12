using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.UnitTests.Operations;

public sealed class ContainerLifecycleTests
{
    private readonly Guid _containerId = Guid.NewGuid();
    private readonly Guid _slotId = Guid.NewGuid();
    private readonly Guid _deliveryOrderId = Guid.NewGuid();
    private readonly DateTime _gateInTime = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);
    private readonly DateTime _gateOutTime = new(2026, 10, 10, 10, 45, 0, DateTimeKind.Utc);

    [Fact]
    public void GateIn_ShouldInitializeLifecycleInYard()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            containerId: _containerId,
            lineOperator: "MSC",
            classification: Classification.A,
            condition: ContainerCondition.Normal,
            inVehicleNumber: "51C-12345",
            slotId: _slotId,
            utcNow: _gateInTime);

        lifecycle.ContainerId.Should().Be(_containerId);
        lifecycle.LineOperator.Should().Be("MSC");
        lifecycle.Classification.Should().Be(Classification.A);
        lifecycle.Condition.Should().Be(ContainerCondition.Normal);
        lifecycle.InVehicleNumber.Should().Be("51C-12345");
        lifecycle.InDate.Should().Be(_gateInTime);
        lifecycle.CurrentSlotId.Should().Be(_slotId);
        lifecycle.Status.Should().Be(LifecycleStatus.InYard);
        lifecycle.IsInDepot.Should().BeTrue();
        lifecycle.OutDate.Should().BeNull();
    }

    [Fact]
    public void GateOut_WhenContainerInYard_ShouldSucceed()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            containerId: _containerId,
            lineOperator: "MSC",
            classification: Classification.A,
            condition: ContainerCondition.Normal,
            inVehicleNumber: "51C-12345",
            slotId: _slotId,
            utcNow: _gateInTime);

        var result = lifecycle.GateOut("51D-99999", _deliveryOrderId, _gateOutTime);

        result.IsSuccess.Should().BeTrue();
        lifecycle.Status.Should().Be(LifecycleStatus.GatedOut);
        lifecycle.OutVehicleNumber.Should().Be("51D-99999");
        lifecycle.DeliveryOrderId.Should().Be(_deliveryOrderId);
        lifecycle.OutDate.Should().Be(_gateOutTime);
        lifecycle.CurrentSlotId.Should().BeNull();
        lifecycle.IsInDepot.Should().BeFalse();
    }

    [Fact]
    public void GateOut_WhenAlreadyGatedOut_ShouldBeRejected()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            containerId: _containerId,
            lineOperator: "MSC",
            classification: Classification.A,
            condition: ContainerCondition.Normal,
            inVehicleNumber: "51C-12345",
            slotId: _slotId,
            utcNow: _gateInTime);

        // First gate-out succeeds
        lifecycle.GateOut("51D-99999", _deliveryOrderId, _gateOutTime).IsSuccess.Should().BeTrue();

        // Second gate-out must fail
        var secondResult = lifecycle.GateOut("51D-88888", _deliveryOrderId, _gateOutTime.AddMinutes(1));

        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("Lifecycle.AlreadyGatedOut");
    }

    [Fact]
    public void GateIn_And_GateOut_ShouldRecordVehicleTypes()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            containerId: _containerId,
            lineOperator: "MSC",
            classification: Classification.A,
            condition: ContainerCondition.Normal,
            inVehicleNumber: "51C-12345",
            slotId: _slotId,
            utcNow: _gateInTime,
            vehicleType: VehicleType.Truck);

        lifecycle.InVehicleType.Should().Be(VehicleType.Truck);
        lifecycle.OutVehicleType.Should().BeNull();

        var result = lifecycle.GateOut("51D-99999", _deliveryOrderId, _gateOutTime, vehicleType: VehicleType.Barge);

        result.IsSuccess.Should().BeTrue();
        lifecycle.OutVehicleType.Should().Be(VehicleType.Barge);
    }
}

