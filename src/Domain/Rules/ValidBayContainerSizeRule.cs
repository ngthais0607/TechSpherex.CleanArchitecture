using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

public sealed class ValidBayContainerSizeRule : IBusinessRule
{
    private readonly int _bay;
    private readonly ContainerSize _size;

    public ValidBayContainerSizeRule(int bay, ContainerSize size)
    {
        _bay = bay;
        _size = size;
    }

    public Error Error => Error.Validation("Yard.InvalidBay", $"The container size '{_size}' is not valid for bay '{_bay}'.");
    public string Message => Error.Description;
    public bool IsBroken()
    {
        bool isEvenBay = _bay % 2 == 0;
        return (_size == ContainerSize.Ft20 && isEvenBay) || (_size == ContainerSize.Ft40 && !isEvenBay);
    }
}
