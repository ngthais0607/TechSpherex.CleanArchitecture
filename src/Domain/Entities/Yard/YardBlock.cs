using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.Entities.Yard;

public sealed class YardBlock: AuditableEntity 
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsVirtual { get; private set; }

    // Sức chứa giới hạn theo 3 chiều không gian[cite: 2]
    public int MaxBays { get; private set; }
    public int MaxRows { get; private set; }
    public int MaxTiers { get; private set; }

    private readonly List<YardSlot> _slots = new();
    public IReadOnlyCollection<YardSlot> Slots => _slots.AsReadOnly();
    private YardBlock() { }

    public static YardBlock CreateStandard(string code, string name, int bays, int rows, int tiers)
    {
        var block = new YardBlock
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name,
            IsVirtual = false,
            MaxBays = bays,
            MaxRows = rows,
            MaxTiers = tiers
        };

        // Tự động sinh ma trận tọa độ slot rỗng cho bãi[cite: 2]
        block.InitializeSlots();
        return block;
    }

    /// 
    /// Factory Method: Khởi tạo Block ảo dùng cho nghiệp vụ đặc thù[cite: 2].
    /// 
    public static YardBlock CreateVirtual(string code, string name)
    {
        return new YardBlock
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name,
            IsVirtual = true,
            MaxBays = 0,
            MaxRows = 0,
            MaxTiers = 0
        };
    }

    /// 
    /// Sinh toàn bộ danh mục vị trí theo 3 vòng lặp không gian[cite: 2].
    /// 
    private void InitializeSlots()
    {
        for (int bay = 1; bay <= MaxBays; bay++)
        {
            for (int row = 1; row <= MaxRows; row++)
            {
                for (int tier = 1; tier <= MaxTiers; tier++)
                {
                    _slots.Add(new YardSlot(Id, bay, row, tier));
                }
            }
        }
    }

    /// 
    /// Nghiệp vụ xếp container vào slot chỉ định trong Block[cite: 2].
    /// Đóng gói kiểm tra quy tắc nghiệp vụ ngay bên trong Entity[cite: 2].
    /// 
    public Result AssignSlot(int bay, int row, int tier, ContainerSize containerSize)
    {
        // 1. Block ảo không hỗ trợ tọa độ slot[cite: 2]
        if (IsVirtual)
            return Result.Failure(Error.Conflict("Yard.VirtualBlock", "Block ảo không hỗ trợ phân vị trí theo Bay, Row, Tier."));

        // 2. Kích hoạt Business Rule kiểm tra tính tương thích Bay lẻ / Bay chẵn[cite: 2]
        var rule = new ValidBayContainerSizeRule(bay, containerSize);
        if (rule.IsBroken())
            return Result.Failure(Error.Validation("Yard.InvalidBay", rule.Message));

        // 3. Tìm slot theo tọa độ
        var slot = _slots.FirstOrDefault(s => s.Bay == bay && s.Row == row && s.Tier == tier);
        if (slot == null)
            return Result.Failure(Error.NotFound("Yard.SlotNotFound", "Vị trí không tồn tại trong bãi."));

        // 4. Kiểm tra slot đã có container khác chiếm chỗ chưa
        if (slot.IsOccupied)
            return Result.Failure(Error.Conflict("Yard.SlotOccupied", "Vị trí này đã có container chiếm chỗ."));

        // Đánh dấu chiếm chỗ thành công
        slot.Occupy();
        return Result.Success();
    }

    /// 
    /// Nghiệp vụ giải phóng vị trí khi cont được bốc dỡ ra khỏi bãi.
    /// 
    public void ReleaseSlot(int bay, int row, int tier)
    {
        var slot = _slots.FirstOrDefault(s => s.Bay == bay && s.Row == row && s.Tier == tier);
        slot?.Release();
    }
}

