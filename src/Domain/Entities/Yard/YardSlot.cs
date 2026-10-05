using SnpDepotManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace SnpDepotManagement.Domain.Entities;

public sealed class YardSlot : BaseEntity
{
    // Khóa ngoại tham chiếu đến Block bãi chứa slot này
    public Guid BlockId { get; private set; }

    // Tọa độ 3 chiều: Bay (chiều dài), Row (chiều rộng/hàng), Tier (chiều cao/tầng)
    public int Bay { get; private set; }
    public int Row { get; private set; }
    public int Tier { get; private set; }

    // Cờ đánh dấu vị trí này hiện đã có container chiếm dụng hay chưa
    public bool IsOccupied { get; private set; }

    // Constructor internal
    internal YardSlot(Guid blockId, int bay, int row, int tier)
    {
        BlockId = blockId;
        Bay = bay;
        Row = row;
        Tier = tier;
        IsOccupied = false;
    }

    // Các hàm đổi trạng thái nội bộ, bảo vệ tính toàn vẹn dữ liệu
    internal void Occupy() => IsOccupied = true;
    internal void Release() => IsOccupied = false;
}