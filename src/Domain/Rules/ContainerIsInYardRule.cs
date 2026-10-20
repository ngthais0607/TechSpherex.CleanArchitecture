using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

public sealed class ContainerIsInYardRule : IBusinessRule
{
    private readonly ContainerLifecycle _lifecycle;

    public ContainerIsInYardRule(ContainerLifecycle lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public Error Error => Error.Conflict(
        "LifecycleRule.ContainerNotInYard",
        "Container hiện không có trong bãi.");

    public string Message => Error.Description;

    public bool IsBroken()
    {
        return _lifecycle.Status != LifecycleStatus.InYard || _lifecycle.OutDate != null;
    }
}
