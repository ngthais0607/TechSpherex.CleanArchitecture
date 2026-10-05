using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.UnitTests.Operations;

public sealed class ContainerLifecycleTests
{
    private readonly Guid _containerId = Guid.NewGuid();
    private readonly Guid _slotId = Guid.NewGuid();
    private readonly Guid _deliveryOrderId = Guid.NewGuid();

    [Fact]
    public void GateIn_ShouldInitializeLifecycleInYard()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            containerId: _containerId,
            lineOperator: "MSC",
            classification: Classification.A,
            condition: ContainerCondition.Normal,
            inVehicleNumber: "51C-12345",
            slotId: _slotId);

        lifecycle.ContainerId.Should().Be(_containerId);
        lifecycle.LineOperator.Should().Be("MSC");
        lifecycle.Classification.Should().Be(Classification.A);
        lifecycle.Condition.Should().Be(ContainerCondition.Normal);
        lifecycle.InVehicleNumber.Should().Be("51C-12345");
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
            slotId: _slotId);

        var result = lifecycle.GateOut("51D-99999", _deliveryOrderId);

        result.IsSuccess.Should().BeTrue();
        lifecycle.Status.Should().Be(LifecycleStatus.GatedOut);
        lifecycle.OutVehicleNumber.Should().Be("51D-99999");
        lifecycle.DeliveryOrderId.Should().Be(_deliveryOrderId);
        lifecycle.OutDate.Should().NotBeNull();
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
            slotId: _slotId);

        // First gate-out succeeds
        lifecycle.GateOut("51D-99999", _deliveryOrderId).IsSuccess.Should().BeTrue();

        // Second gate-out must fail
        var secondResult = lifecycle.GateOut("51D-88888", _deliveryOrderId);

        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("Lifecycle.AlreadyGatedOut");
    }
}
