using System;
using System.Collections.Generic;
using System.Text;

namespace SnpDepotManagement.Domain.Enums;

public enum ContainerSize 
{
    Ft20= 20,
    Ft40 = 40,
    Ft45 = 45
}

public enum ContainerType
{
    Dry = 'U',         // Container hàng khô thông thường[cite: 4, 6]
    Reefer = 'R',      // Container lạnh, có hệ thống điều chỉnh nhiệt độ[cite: 4, 6]
    OpenTop = 'S',     // Container mở nóc, dùng chở hàng quá khổ[cite: 4, 6]
    FlatRack = 'F',    // Container mặt bằng, không có vách bên/mái[cite: 4, 6]
    Bunker = 'B',      // Container bồn chứa chất lỏng/hóa chất[cite: 6]
    Ventilated = 'V',  // Container có lỗ thông khí[cite: 6]
    Specialized = 'Z'  // Container chuyên dụng khác[cite: 6]

}

public enum ContainerClassification
{
    GradeA, // Vỏ đạt chuẩn đóng hàng giá trị cao / thực phẩm
    GradeB, // Vỏ đạt chuẩn đóng hàng tiêu chuẩn
    GradeC  // Vỏ xuống cấp, chỉ đóng hàng thô hoặc cần sửa chữa

}

public enum ContainerCondition
{
    Normal,  // Vỏ lành lặn, bình thường[cite: 3]
    Damaged, // Hư hỏng thủng, rách[cite: 3]
    Dented,  // Bị móp méo cục bộ[cite: 3]
    Bent     // Khung chịu lực bị cong vênh[cite: 3]
}