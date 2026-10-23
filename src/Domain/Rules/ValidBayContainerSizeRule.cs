using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

public sealed class ValidBayContainerSizeRule : IBusinessRule
{
    private readonly int _bay;
    private readonly ContainerSize _size;
    private readonly int _maxBays;

    public ValidBayContainerSizeRule(int bay, ContainerSize size, int maxBays = int.MaxValue)
    {
        _bay = bay;
        _size = size;
        _maxBays = maxBays;
    }

    public Error Error => Error.Validation(
        "YardRule.InvalidBayForSize",
        $"Vị trí bay '{_bay}' không hợp lệ cho kích thước container '{_size}'.");

    public string Message => Error.Description;

    public bool IsBroken()
    {
        if (_bay < 1 || _bay > _maxBays)
        {
            return true;
        }

        bool isEvenBay = _bay % 2 == 0;

        if (_size == ContainerSize.Ft20)
        {
            return isEvenBay;
        }

        if (_size == ContainerSize.Ft40)
        {
            if (!isEvenBay)
            {
                return true;
            }

            int bayBefore = _bay - 1;
            int bayAfter = _bay + 1;

            return bayBefore < 1 || bayAfter > _maxBays;
        }

        return false;
    }
}
