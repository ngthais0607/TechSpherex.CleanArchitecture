# Báo Cáo Kỹ Thuật: Ngày 4 - Quy Tắc Xếp Bãi (Yard Business Rules - TDD)

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 4**: Bộ 4 quy tắc an toàn xếp bãi (Specification Pattern), Xử lý trường hợp biên vật lý (Dual Support Physics, Boundary Overhang) & Phương pháp TDD

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

1. **Đặc thù an toàn vật lý của bãi vỏ container rỗng**:
   - Container rỗng có trọng lượng nhẹ (Tare Weight chỉ từ $2.2$ đến $4$ tấn) nhưng được xếp chồng rất cao (5 đến 7 tầng) nhằm tận dụng tối đa diện tích bãi.
   - Khối container cao tầng chịu tác động lớn từ áp lực gió bão và sự mất thăng bằng nền móng. Do đó, các quy tắc xếp bãi là "chốt chặn an toàn sống còn" của hệ thống để ngăn chặn sập đổ hàng loạt.
2. **Quy tắc tương thích hình học và trường hợp biên mép bãi (`ValidBayContainerSizeRule`)**:
   - Container 20ft bắt buộc phải xếp vào **Bay lẻ** ($1, 3, 5, \dots$).
   - Container 40ft bắt buộc phải xếp vào **Bay chẵn** ($2, 4, 6, \dots$).
   - **Trường hợp biên mép block (Boundary Overhang Edge Case)**:
     - Nếu một block có `MaxBays = 4`, một lệnh xếp cont 40ft vào `Bay = 4`:
       - Bay trước: $4 - 1 = 3 \le 4$ (hợp lệ).
       - Bay sau: $4 + 1 = 5 > 4$ (vượt ra ngoài phạm vi bãi!).
     - Nếu không có logic chặn mép, nửa sau container 40ft sẽ bị chìa ra ngoài đường nội bộ bãi, gây va quẹt xe nâng hoặc gãy đổ.
3. **Quy tắc giới hạn tĩnh không chiều cao (`TierWithinBlockLimitRule`)**:
   - Ngăn cấm việc xếp container vượt quá tầng cao tối đa (`MaxTiers`) mà nền bãi và thiết bị xe nâng (Reach Stacker) cho phép. Chặn các giá trị tầng $\le 0$.
4. **Quy tắc chống container lơ lửng & Vật lý hai chân đế (`SlotBelowMustBeOccupiedRule`)**:
   - **Tầng trệt ($Tier = 1$)**: Nằm trên mặt đất nên luôn hợp lệ.
   - **Tầng cao ($Tier > 1$)**:
     - *Container 20ft*: Ô ngay bên dưới tại tầng $Tier - 1$ (cùng Bay, cùng Row) bắt buộc phải có container đang chiếm chỗ (`IsOccupied == true`).
     - *Container 40ft (Dual Support Physics)*: Cả **hai ô bay lẻ liền kề bên dưới** ($Bay - 1$ và $Bay + 1$ tại tầng $Tier - 1$) đều bắt buộc phải có container đỡ. Nếu chỉ có một bên đỡ, container 40ft sẽ bị kênh và gãy sập.
5. **Quy tắc chống đè lên container khác (`SlotIsFreeRule`)**:
   - Đảm bảo vị trí dự kiến hạ container chưa bị chiếm chỗ:
     - Với 20ft: Ô tại tọa độ đó phải có `IsOccupied == false`.
     - Với 40ft: Cả hai ô thành phần $Bay - 1$ và $Bay + 1$ tại tầng đó đều phải còn trống.

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Specification / Business Rule Pattern**:
  - Triển khai interface chuẩn `IBusinessRule` gồm hai thành phần:
    - `bool IsBroken()`: Trả về `true` nếu điều kiện an toàn bị vi phạm.
    - `Error Error`: Chứa mã lỗi và thông báo chi tiết khi vi phạm.
  - Tách hoàn toàn logic kiểm tra an toàn ra khỏi Entity `YardBlock`. Khi cần thêm quy tắc mới (ví dụ: cách ly hàng nguy hiểm, giới hạn tải trọng gió), chỉ cần tạo thêm class rule mới mà không sửa mã nguồn cũ (Tuân thủ triệt để OCP).
- **Thuật Toán Kiểm Tra Mép Block Toàn Diện**:
  ```csharp
  int bayBefore = _bay - 1;
  int bayAfter = _bay + 1;
  return bayBefore < 1 || bayAfter > _maxBays;
  ```
- **Thuật Toán Thẩm Định Hai Chân Đỡ (Dual Supporting Stack Physics)**:
  - Container 40ft kiểm tra đồng thời:
    $$\text{IsBroken} = (\text{slotBefore} == \text{null} \lor !\text{slotBefore}.\text{IsOccupied}) \lor (\text{slotAfter} == \text{null} \lor !\text{slotAfter}.\text{IsOccupied})$$
- **Test-Driven Development (TDD)**:
  - Toàn bộ 4 quy tắc đều được viết Unit Tests trước để phủ hết các trường hợp biên: mép ngoài, tầng trệt, ô không tồn tại, thiếu chân đỡ trái/phải, rồi mới tiến hành viết code hiện thực.

### 2.2. Kiến Thức Áp Dụng
- **Domain-Driven Design (DDD)**: Domain Rules thể hiện các quy tắc nghiệp vụ bất biến cốt lõi của không gian bãi.
- **SOLID**:
  - `SRP`: Mỗi class chỉ thẩm định đúng một khía cạnh an toàn bãi.
  - `OCP`: Mở rộng quy tắc xếp cont không giới hạn mà không sửa core.
  - `LSP`: Mọi Rule đều tương thích hoàn hảo với `BusinessRuleValidator.Validate(...)`.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

```text
Domain/
└── Rules/
    ├── ValidBayContainerSizeRule.cs       # Thẩm định kích thước tương thích Bay và chặn mép bãi
    ├── TierWithinBlockLimitRule.cs        # Thẩm định giới hạn tĩnh không chiều cao tầng
    ├── SlotBelowMustBeOccupiedRule.cs     # Thẩm định an toàn chống lơ lửng & vật lý 2 chân đỡ
    └── SlotIsFreeRule.cs                  # Thẩm định ô trống (chống xếp đè lên cont khác)
tests/
└── Domain.UnitTests/
    └── Rules/                             # 29 Unit Tests kiểm thử độc lập 4 quy tắc bãi theo TDD
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng thống kê các phương thức cốt lõi được xây dựng trong ngày:

| Tên Hàm (Signature) | Tham Số Đầu Vào | Đầu Ra (Return) | Chức Năng & Vai Trò Trong Luồng |
|---|---|---|---|
| `ValidBayContainerSizeRule(...)` | Bay, kích thước cont, số bay tối đa | Constructor | Nạp tọa độ và kích cỡ container cần thẩm định vào đối tượng rule. |
| `ValidBayContainerSizeRule.IsBroken()` | Không | `bool` | Kiểm tra vi phạm: trả về `true` nếu 20ft ở bay chẵn, 40ft ở bay lẻ, hoặc 40ft bị thò ra ngoài mép block (`bayAfter > maxBays`). |
| `TierWithinBlockLimitRule(...)` | Tầng chỉ định, tầng tối đa của block | Constructor | Nạp thông số tầng cao của vị trí dự kiến hạ container. |
| `TierWithinBlockLimitRule.IsBroken()` | Không | `bool` | Kiểm tra vi phạm: trả về `true` nếu tầng $\le 0$ hoặc tầng vượt quá `MaxTiers` của block. |
| `SlotBelowMustBeOccupiedRule(...)` | Block bãi, bay, row, tier, kích cỡ cont | Constructor | Nạp ngữ cảnh ô bãi để kiểm tra sự tồn tại của khối đỡ bên dưới. |
| `SlotBelowMustBeOccupiedRule.IsBroken()` | Không | `bool` | Kiểm tra vi phạm chống lơ lửng: tầng 1 trả về `false` (hợp lệ); tầng $> 1$ yêu cầu ô bên dưới phải có cont (40ft cần cả 2 ô liền kề). |
| `SlotIsFreeRule(...)` | Block bãi, bay, row, tier, kích cỡ cont | Constructor | Nạp ngữ cảnh ô bãi để kiểm tra trạng thái chiếm chỗ hiện tại. |
| `SlotIsFreeRule.IsBroken()` | Không | `bool` | Kiểm tra vi phạm chống xếp đè: trả về `true` nếu ô chỉ định (hoặc 1 trong 2 ô của cont 40ft) đã bị chiếm hoặc không tồn tại. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Triển Khai Giao Diện Với `public sealed class ClassName : IBusinessRule`
```csharp
public sealed class ValidBayContainerSizeRule : IBusinessRule
```
- `sealed`: Đảm bảo quy tắc nghiệp vụ là một lớp cụ thể, hoàn chỉnh, không bị lớp khác kế thừa và ghi đè logic kiểm tra an toàn.
- `: IBusinessRule`: Khai báo thực thi hợp đồng chung. Điều này cho phép áp dụng tính **Đa hình (Polymorphism)**:
  ```csharp
  BusinessRuleValidator.Validate(
      new ValidBayContainerSizeRule(...),
      new TierWithinBlockLimitRule(...),
      new SlotBelowMustBeOccupiedRule(...)
  );
  ```

### 5.2. Thuộc Tính Lỗi Dạng Biểu Thức (Expression-Bodied Property)
```csharp
public Error Error => Error.Validation(
    "YardRule.InvalidBayContainerSize",
    $"Container size {_size} is not compatible with bay {_bay}."
);
```
- Thay vì cấp phát một biến `Error` cố định trong bộ nhớ ngay khi khởi tạo rule, cú pháp `=>` chỉ tính toán và sinh ra đối tượng lỗi khi thuộc tính `Error` thực sự được gọi (thường là khi `IsBroken() == true`). Giúp tiết kiệm tài nguyên bộ nhớ cho các trường hợp kiểm tra hợp lệ.

### 5.3. Trường Bất Biến `private readonly` Trong Rules
```csharp
private readonly int _bay;
private readonly ContainerSize _size;
private readonly int _maxBays;
```
- Việc đặt các biến đầu vào ở trạng thái `private readonly` đảm bảo các tham số thẩm định không thể bị thay đổi sau khi rule đã được khởi tạo, giữ cho quá trình kiểm tra luôn nhất quán và không có hiệu ứng lề (Side Effect).

---

## 6. Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)

### 6.1. Mục Tiêu & Cấu Trúc Kiểm Thử Ngày 4
Bộ Unit Tests của Ngày 4 gồm **29 tests** được phát triển theo phương pháp **TDD (Test-Driven Development)**:
- Kiểm thử tính tương thích kích cỡ container và xử lý trường hợp biên mép bãi.
- Kiểm thử giới hạn tĩnh không chiều cao.
- Kiểm thử cơ chế an toàn chống lơ lửng và vật lý hai chân đế (`Dual Support Physics`).
- Kiểm thử chống xếp đè lên ô đã có container.

### 6.2. Mã Nguồn Bài Test Minh Họa & Các Kịch Bản

#### Kịch bản 1: Kiểm thử chặn container 40ft tại mép ngoài cùng của block (Boundary Overhang)
```csharp
[Fact]
public void IsBroken_When40FtAtEdgeWhereAdjacentBayExceedsBlock_ShouldBeTrue()
{
    // Arrange: Block có MaxBays = 4. Xếp cont 40ft vào bay 4 (bay + 1 = 5 vượt MaxBays)
    var rule = new ValidBayContainerSizeRule(bay: 4, ContainerSize.Ft40, maxBays: 4);

    // Act
    bool isBroken = rule.IsBroken();

    // Assert: Quy tắc bị phá vỡ (vi phạm an toàn mép bãi)
    isBroken.Should().BeTrue();
    rule.Error.Code.Should().Be("YardRule.InvalidBayForSize");
}
```
* **Giải thích**: Test case bắt buộc phải trả về `isBroken == true` để ngăn xe nâng đặt cont 40ft thò đầu ra ngoài đường nội bộ bãi.

#### Kịch bản 2: Kiểm thử vật lý hai chân đỡ của container 40ft (Dual Support Physics)
```csharp
[Fact]
public void IsBroken_When40FtOnTier2_AndOnlyOneSupportingSlotOccupied_ShouldBeTrue()
{
    // Arrange: Block có 2 ô bay lẻ 1 và 3 ở tầng 1. Chỉ chiếm ô bay 1, ô bay 3 để trống
    var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 4, rows: 2, tiers: 3);
    block.OccupySlot(bay: 1, row: 1, tier: 1, Guid.NewGuid()); // Ô trái có cont
    // Ô phải (bay 3, row 1, tier 1) TRỐNG!

    // Act: Cố gắng đặt cont 40ft vào bay 2, tầng 2 (bắc ngang qua bay 1 và bay 3)
    var rule = new SlotBelowMustBeOccupiedRule(block, bay: 2, row: 1, tier: 2, ContainerSize.Ft40);

    // Assert: Thiếu chân đỡ bên phải => Vi phạm quy tắc, không cho hạ cont
    rule.IsBroken().Should().BeTrue();
    rule.Error.Code.Should().Be("YardRule.SlotBelowMustBeOccupied");
}
```
* **Giải thích**: Kiểm tra đúng động lực học cảng: container 40ft xếp tầng 2 trở lên bắt buộc phải có đủ 2 container tầng dưới đỡ ở cả 2 đầu. Thiếu 1 bên là bị từ chối ngay.

#### Kịch bản 3: Kiểm thử tầng trệt luôn luôn hợp lệ (Tầng 1 trên nền đất)
```csharp
[Fact]
public void IsBroken_WhenTier1_ShouldAlwaysBeFalse()
{
    // Arrange: Đặt container ở tầng trệt (Tier = 1)
    var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 4, rows: 2, tiers: 3);
    var rule = new SlotBelowMustBeOccupiedRule(block, bay: 1, row: 1, tier: 1, ContainerSize.Ft20);

    // Act & Assert: Tầng trệt không cần ô đỡ bên dưới => IsBroken luôn là false
    rule.IsBroken().Should().BeFalse();
}
```
* **Giải thích**: Tầng trệt tiếp xúc trực tiếp mặt đất nên quy tắc chống lơ lửng không áp dụng.
