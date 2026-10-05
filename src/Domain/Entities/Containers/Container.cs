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
        string ownerCode,
        string tenantId = "")
    {
        if (containerNumber == null)
        {
            return Result.Failure<Container>(
                Error.Validation("Container.EmptyNumber", "Container number is required."));
        }

        if (tareWeight <= 0)
        {
            return Result.Failure<Container>(
                Error.Validation("Container.InvalidTareWeight", "Tare weight must be greater than zero."));
        }

        if (maxGrossWeight <= tareWeight)
        {
            return Result.Failure<Container>(
                Error.Validation("Container.InvalidMaxGrossWeight", "Max gross weight must be greater than tare weight."));
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
            OwnerCode = string.IsNullOrWhiteSpace(ownerCode) ? containerNumber.OwnerCode : ownerCode.Trim().ToUpperInvariant(),
            TenantId = tenantId,
            Condition = ContainerCondition.Normal,
            Classification = Classification.A
        });
    }

    public Result MarkDamaged(ContainerCondition condition)
    {
        if (condition == ContainerCondition.Normal)
        {
            return Result.Failure(
                Error.Validation("Container.InvalidCondition", "Damaged condition cannot be Normal."));
        }

        Condition = condition;
        return Result.Success();
    }
}