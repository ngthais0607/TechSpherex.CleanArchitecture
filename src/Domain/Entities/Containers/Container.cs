using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.Entities.Containers;

public sealed class Container : AuditableEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public ContainerNumber ContainerNumber { get; private set; } = null!;
    public string IsoCode { get; private set; } = string.Empty;
    public ContainerSize Size { get; private set; }
    public ContainerType Type { get; private set; }
    public decimal TareWeight { get; private set; }
    public decimal MaxGrossWeight { get; private set; }
    public DateTime? DateOfManufacture { get; private set; }
    public string OwnerCode { get; private set; } = string.Empty;
    public ContainerCondition Condition { get; private set; } = ContainerCondition.Normal;
    public Classification Classification { get; private set; } = Classification.A;

    private Container() { }

    public static Result<Container> Create(
        ContainerNumber containerNumber,
        string isoCode,
        ContainerSize size,
        ContainerType type,
        decimal tareWeight,
        decimal maxGrossWeight,
        DateTime? dateOfManufacture,
        Classification grade = Classification.A,
        ContainerCondition condition = ContainerCondition.Normal,
        string tenantId = "")
    {
        if (containerNumber == null)
        {
            return Result.Failure<Container>(
                Error.Validation("Container.EmptyNumber", "Số container không được để trống."));
        }

        if (tareWeight <= 0)
        {
            return Result.Failure<Container>(
                Error.Validation("Container.InvalidTareWeight", "Trọng lượng vỏ phải lớn hơn 0."));
        }

        if (maxGrossWeight <= tareWeight)
        {
            return Result.Failure<Container>(
                Error.Validation("Container.InvalidMaxGrossWeight", "Trọng lượng toàn tải tối đa phải lớn hơn trọng lượng vỏ."));
        }

        return Result.Success(new Container
        {
            ContainerNumber = containerNumber,
            IsoCode = isoCode?.Trim().ToUpperInvariant() ?? string.Empty,
            Size = size,
            Type = type,
            TareWeight = tareWeight,
            MaxGrossWeight = maxGrossWeight,
            DateOfManufacture = dateOfManufacture,
            OwnerCode = containerNumber.OwnerCode,
            TenantId = tenantId,
            Condition = condition,
            Classification = grade
        });
    }

    public static Result<Container> Create(
        ContainerNumber containerNumber,
        string isoCode,
        ContainerSize size,
        ContainerType type,
        decimal tareWeight,
        decimal maxGrossWeight,
        DateTime? dateOfManufacture,
        string ownerCode,
        string tenantId = "")
    {
        var res = Create(containerNumber, isoCode, size, type, tareWeight, maxGrossWeight, dateOfManufacture, Classification.A, ContainerCondition.Normal, tenantId);
        if (res.IsSuccess && !string.IsNullOrWhiteSpace(ownerCode))
        {
            res.Value!.OwnerCode = ownerCode.Trim().ToUpperInvariant();
        }
        return res;
    }

    public Result MarkDamaged(ContainerCondition condition)
    {
        if (condition == ContainerCondition.Normal)
        {
            return Result.Failure(
                Error.Validation("Container.InvalidCondition", "Tình trạng hư hỏng không thể là Normal."));
        }

        Condition = condition;
        return Result.Success();
    }

    public void UpdateConditionAndClassification(ContainerCondition? condition, Classification? grade)
    {
        if (condition.HasValue) Condition = condition.Value;
        if (grade.HasValue) Classification = grade.Value;
    }
}