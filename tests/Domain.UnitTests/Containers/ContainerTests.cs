using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.UnitTests.Containers;

public sealed class ContainerTests
{
    private readonly ContainerNumber _validNumber;

    public ContainerTests()
    {
        var numberResult = ContainerNumber.Create("CSQU3054383");
        _validNumber = numberResult.Value!;
    }

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        var result = Container.Create(
            _validNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            tareWeight: 2200m,
            maxGrossWeight: 30480m,
            dateOfManufacture: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ownerCode: "CSQ",
            tenantId: "DEPOT-01");

        result.IsSuccess.Should().BeTrue();
        var container = result.Value!;
        container.ContainerNumber.Should().Be(_validNumber);
        container.IsoCode.Should().Be("22G1");
        container.Size.Should().Be(ContainerSize.Ft20);
        container.Type.Should().Be(ContainerType.Dry);
        container.TareWeight.Should().Be(2200m);
        container.MaxGrossWeight.Should().Be(30480m);
        container.OwnerCode.Should().Be("CSQ");
        container.TenantId.Should().Be("DEPOT-01");
        container.Condition.Should().Be(ContainerCondition.Normal);
    }

    [Fact]
    public void Create_WithGradeAndCondition_ShouldDeriveOwnerCode()
    {
        var result = Container.Create(
            _validNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            tareWeight: 2200m,
            maxGrossWeight: 30480m,
            dateOfManufacture: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            grade: Classification.C,
            condition: ContainerCondition.Warped,
            tenantId: "DEPOT-01");

        result.IsSuccess.Should().BeTrue();
        var container = result.Value!;
        container.OwnerCode.Should().Be("CSQ");
        container.Classification.Should().Be(Classification.C);
        container.Condition.Should().Be(ContainerCondition.Warped);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Create_WhenTareWeightInvalid_ShouldReturnError(decimal tareWeight)
    {
        var result = Container.Create(
            _validNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            tareWeight: tareWeight,
            maxGrossWeight: 30480m,
            dateOfManufacture: null,
            ownerCode: "CSQ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidTareWeight");
    }

    [Theory]
    [InlineData(2200, 2200)]
    [InlineData(2200, 2000)]
    public void Create_WhenMaxGrossWeightLessOrEqualTare_ShouldReturnError(decimal tareWeight, decimal maxGross)
    {
        var result = Container.Create(
            _validNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            tareWeight: tareWeight,
            maxGrossWeight: maxGross,
            dateOfManufacture: null,
            ownerCode: "CSQ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidMaxGrossWeight");
    }

    [Theory]
    [InlineData(ContainerCondition.Damaged)]
    [InlineData(ContainerCondition.Dented)]
    [InlineData(ContainerCondition.Bent)]
    public void MarkDamaged_WithDamageCondition_ShouldSucceed(ContainerCondition condition)
    {
        var container = Container.Create(
            _validNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            tareWeight: 2200m,
            maxGrossWeight: 30480m,
            dateOfManufacture: null,
            ownerCode: "CSQ").Value!;

        var result = container.MarkDamaged(condition);

        result.IsSuccess.Should().BeTrue();
        container.Condition.Should().Be(condition);
    }

    [Fact]
    public void MarkDamaged_WithNormalCondition_ShouldFail()
    {
        var container = Container.Create(
            _validNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            tareWeight: 2200m,
            maxGrossWeight: 30480m,
            dateOfManufacture: null,
            ownerCode: "CSQ").Value!;

        var result = container.MarkDamaged(ContainerCondition.Normal);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidCondition");
    }
}
