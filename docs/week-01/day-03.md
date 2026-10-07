# Báo Cáo Kỹ Thuật: Ngày 3 - Làm Phần Bãi (Block, Ô Chứa, Mở Rộng / Thu Hẹp)

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 3**: Mô hình hóa không gian bãi 3 chiều, Aggregate Root `YardBlock`, Entity `YardSlot`, Quy ước Bay lẻ (Odd-Bay Convention) & Thuật toán Expand/Shrink

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

1. **Mô hình hóa hệ tọa độ bãi 3 chiều (Yard Coordinate System)**:
   - Một bãi container rỗng được phân chia thành các chiều không gian:
     - `Block`: Phân khu bãi (ví dụ: Phân khu A, B, C...).
     - `Bay`: Vị trí ô theo chiều dài của block ($X$).
     - `Row`: Dãy ô theo chiều ngang/rộng của block ($Y$).
     - `Tier`: Tầng theo chiều cao xếp chồng ($Z$).
   - Mọi tọa độ Bay, Row, Tier bắt buộc phải là số nguyên dương $> 0$. Tọa độ hiển thị chuẩn hóa của cảng biển có dạng: `Block-Bay-Row-Tier` (ví dụ: `A-01-02-03`).
2. **Quy ước Bay Lẻ (Odd-Bay Convention) & Bài toán lưu trữ ô bãi**:
   - Trong chuẩn logistics quốc tế:
     - **Bay lẻ** ($1, 3, 5, \dots$): Dành cho container tiêu chuẩn 20ft.
     - **Bay chẵn** ($2, 4, 6, \dots$): Dành cho container 40ft (đặt vắt ngang qua 2 ô bay lẻ liền kề).
   - Nếu cơ sở dữ liệu lưu cả slot 20ft và slot 40ft sẽ gây ra sự trùng lặp dữ liệu và xung đột không gian nghiêm trọng. Cần thiết kế: **Chỉ sinh và lưu các ô vật lý ở Bay lẻ**. Một container 40ft khi đặt vào bay chẵn $k$ sẽ chiếm dụng đồng thời 2 ô bay lẻ $k-1$ và $k+1$.
3. **Bài toán mở rộng quy mô bãi (`Expand`)**:
   - Khi depot mở rộng mặt bằng hoặc nâng cấp cẩu nâng để xếp cao thêm tầng, hệ thống phải cho phép tăng `MaxBays`, `MaxRows`, `MaxTiers` và tự động sinh thêm các ô bãi mới lũy tiến mà không làm ảnh hưởng đến các ô bãi cũ đang có container.
4. **Bài toán thu hẹp quy mô bãi & Phát hiện xung đột nguyên tử (`Shrink`)**:
   - Khi depot cần giải tỏa một phần bãi để làm đường nội bộ hoặc trả mặt bằng, kích thước block bị giảm xuống.
   - **Rủi ro lớn nhất**: Vô tình xóa bỏ những ô bãi đang chứa container, dẫn đến mất dấu vị trí tài sản thực tế.
   - **Yêu cầu**: Thuật toán `Shrink` phải kiểm tra phát hiện xung đột (**Conflict Detection**). Nếu trong vùng không gian bị cắt giảm tồn tại dù chỉ một ô đang có container (`IsOccupied == true`), toàn bộ thao tác thu hẹp bị từ chối ngay lập tức (Atomic Failure).
5. **Mô hình hóa Block Ảo (`Virtual Block`)**:
   - Trong bãi rỗng có các khu vực dịch vụ kỹ thuật không có tọa độ bay-row-tier cố định: khu sửa chữa vỏ rách (M&R), khu vệ sinh xịt rửa container, khu cắm điện chạy thử PTI cho container lạnh. Cần cơ chế tạo block ảo với dung lượng linh hoạt.

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Value Object `YardPosition`**:
  - Đóng gói tọa độ 3 chiều thành đối tượng bất biến. Tự kiểm tra các giá trị âm hoặc bằng 0 ngay tại thời điểm khởi tạo.
- **Aggregate Root Pattern (`YardBlock`)**:
  - `YardBlock` đóng vai trò là Aggregate Root quản lý toàn bộ danh sách thực thể con `YardSlot`. Mọi tương tác chiếm ô (`OccupySlot`), giải phóng ô (`ReleaseSlot`), thay đổi kích thước (`Expand`, `Shrink`) bắt buộc phải thông qua `YardBlock`.
  - Không cho phép truy cập và sửa đổi trực tiếp danh sách `YardSlot` từ bên ngoài Aggregate.
- **Encapsulated Collection (Đóng gói tập hợp)**:
  - Khai báo danh sách nội bộ `private readonly List<YardSlot> _slots` và chỉ để lộ ra ngoài thuộc tính chỉ đọc `IReadOnlyCollection<YardSlot> Slots => _slots.AsReadOnly()`.
- **Thuật Toán Sinh Ô Bay Lẻ Tự Động (`InitializeSlots`)**:
  - Duyệt bước nhảy `bay += 2` để chỉ sinh ô tại các số lẻ:
    $$\text{Tổng số ô} = \left\lceil \frac{\text{MaxBays}}{2} \right\rceil \times \text{MaxRows} \times \text{MaxTiers}$$
- **Thuật Toán Conflict Detection trong `Shrink`**:
  - Lọc toàn bộ các ô nằm ngoài phạm vi giới hạn mới:
    $$\text{Slot ngoài vùng} = \{ s \in \text{\_slots} \mid s.\text{Bay} > \text{newBays} \lor s.\text{Row} > \text{newRows} \lor s.\text{Tier} > \text{newTiers} \}$$
  - Nếu bất kỳ ô nào có `IsOccupied == true` $\rightarrow$ Trả về lỗi `YardBlock.ShrinkConflictWithOccupiedSlots`. Ngược lại, an toàn gỡ bỏ các ô này bằng `_slots.RemoveAll(...)`.

### 2.2. Kiến Thức Áp Dụng
- **Domain-Driven Design (DDD)**: Aggregate Root bảo vệ Transactional Boundary và Invariants của cụm thực thể bãi.
- **OOP (Encapsulation triệt để)**: Toàn bộ kích thước và danh sách slot đều dùng `private set` và `private readonly List`.
- **SOLID (Single Responsibility & Open/Closed)**:
  - `YardSlot` chỉ quản lý tọa độ và container chiếm chỗ tại 1 ô.
  - `YardBlock` quản lý ma trận không gian bãi và thuật toán sinh slot.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

```text
Domain/
├── Enums/
│   └── YardEnums.cs                 # Enum YardBlockType (Standard, Virtual)
├── ValueObjects/
│   └── YardPosition.cs              # Value Object tọa độ 3 chiều (Block, Bay, Row, Tier)
└── Entities/
    └── Yard/
        ├── YardBlock.cs             # Aggregate Root quản lý khối bãi, sinh ô, Expand & Shrink
        └── YardSlot.cs              # Thực thể ô bãi 20ft ở bay lẻ, lưu trữ ContainerId
tests/
└── Domain.UnitTests/
    └── Yard/                        # Unit test cho YardPosition, YardBlock, Expand, Shrink
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng thống kê các phương thức cốt lõi được xây dựng trong ngày:

| Tên Hàm (Signature) | Tham Số Đầu Vào | Đầu Ra (Return) | Chức Năng & Vai Trò Trong Luồng |
|---|---|---|---|
| `YardPosition.Create(...)` | Mã block, bay, row, tier | `Result<YardPosition>` | Factory method kiểm tra tọa độ $> 0$, sinh đối tượng tọa độ bất biến kèm định dạng `Block-Bay-Row-Tier`. |
| `YardBlock.CreateStandard(...)` | Code, Name, bays, rows, tiers | `YardBlock` | Khởi tạo block bãi tiêu chuẩn và tự động gọi `InitializeSlots()` để sinh ma trận ô bay lẻ. |
| `YardBlock.CreateVirtual(...)` | Code, Name, tenantId | `YardBlock` | Khởi tạo block ảo phục vụ khu sửa chữa M&R, vệ sinh hoặc PTI container lạnh (kích thước = 0). |
| `YardBlock.InitializeSlots()` | Không (gọi nội bộ) | `void` | Thuật toán sinh toàn bộ các ô bãi 20ft với bước nhảy `bay += 2` (Odd-bay convention). |
| `YardBlock.Expand(newBays, newRows, newTiers)` | Kích thước mới | `Result` | Mở rộng kích thước bãi; nếu kích thước mới lớn hơn, tự động lặp và sinh thêm các ô bãi còn thiếu. |
| `YardBlock.Shrink(newBays, newRows, newTiers)` | Kích thước mới nhỏ hơn | `Result` | Thu hẹp bãi với thuật toán Conflict Detection: từ chối nguyên tử nếu vùng giải tỏa có ô đang chứa cont. |
| `YardBlock.GetSlot(bay, row, tier)` | Tọa độ ô | `YardSlot?` | Tìm kiếm ô bãi theo tọa độ 3 chiều trong tập hợp `_slots`. |
| `YardBlock.OccupySlot(bay, row, tier, containerId)` | Tọa độ ô, Id container | `Result` | Tìm ô tương ứng và chuyển trạng thái ô sang bị chiếm dụng bởi container chỉ định. |
| `YardBlock.ReleaseSlot(bay, row, tier)` | Tọa độ ô | `Result` | Tìm ô tương ứng và giải phóng vị trí khi container được bốc đi nơi khác. |
| `YardSlot.Occupy(Guid containerId)` | Id container | `Result` | Gán `ContainerId`, bật cờ `IsOccupied = true`. Trả về lỗi nếu ô đã có cont chiếm trước đó. |
| `YardSlot.Release()` | Không | `Result` | Xóa `ContainerId`, đặt cờ `IsOccupied = false`. Trả về lỗi nếu ô vốn dĩ đang trống. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Kỹ Thuật Đóng Gói Bộ Sưu Tập (Encapsulated Collection)
```csharp
private readonly List<YardSlot> _slots = new();
public IReadOnlyCollection<YardSlot> Slots => _slots.AsReadOnly();
```
- **Tại sao không dùng `public List<YardSlot> Slots { get; set; }`?**:
  Nếu để `List<YardSlot>` công khai, code bên ngoài có thể gọi `block.Slots.Add(new YardSlot(...))` hoặc `block.Slots.Clear()`. Khi đó, `YardBlock` mất hoàn toàn quyền kiểm soát tính toàn vẹn của ô bãi, phá vỡ nguyên tắc của Aggregate Root.
- **Giải pháp**:
  - `_slots` là trường nội bộ `private readonly` (chỉ Aggregate mới có thể thêm/xóa ô).
  - Thuộc tính công khai `Slots` có kiểu `IReadOnlyCollection<YardSlot>`, tạo một lớp vỏ bảo vệ thông qua `.AsReadOnly()`. Bất kỳ nỗ lực ép kiểu hoặc sửa đổi danh sách từ bên ngoài đều bị chặn lại tại thời điểm biên dịch hoặc ném ngoại lệ lúc chạy.

### 5.2. Từ Khóa `readonly` Cho Trường Dữ Liệu
```csharp
private readonly List<YardSlot> _slots = new();
```
- `readonly` đảm bảo rằng biến tham chiếu `_slots` chỉ được khởi tạo một lần duy nhất tại nơi khai báo hoặc trong constructor.
- Ngăn chặn nguy cơ ai đó vô tình gán `_slots = null` hoặc trỏ sang một danh sách khác trong suốt vòng đời của đối tượng.

### 5.3. Toán Tử `Math.Max` Trong Thiết Lập Biên Dữ Liệu
```csharp
MaxBays = Math.Max(0, bays);
MaxRows = Math.Max(0, rows);
MaxTiers = Math.Max(0, tiers);
```
- Đảm bảo tính phòng vệ (Defensive Programming): ngay cả khi đầu vào truyền vào số âm, kích thước của block vẫn được giữ ở mức an toàn tối thiểu là $0$, không làm sập vòng lặp `for`.

### 5.4. Phương Thức LINQ `RemoveAll` Có Điều Kiện
```csharp
_slots.RemoveAll(s => s.Bay > newBays || s.Row > newRows || s.Tier > newTiers);
```
- Xóa bỏ hàng loạt phần tử thỏa mãn biểu thức vị từ (predicate) trong một lần duyệt với độ phức tạp $O(n)$, tối ưu hiệu năng bộ nhớ hơn so với việc dùng vòng lặp `foreach` kết hợp xóa từng phần tử.

---

## 6. Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)

### 6.1. Mục Tiêu & Cấu Trúc Kiểm Thử Ngày 3
Bộ Unit Tests của Ngày 3 gồm **14 tests** tập trung kiểm thử:
- Thuật toán sinh ô bay lẻ (`Odd-Bay Convention`).
- Mở rộng (`Expand`) và thu hẹp (`Shrink`) không gian bãi.
- Thuật toán phát hiện xung đột (`Conflict Detection`) chặn đứng việc thu hẹp khi có container.

### 6.2. Mã Nguồn Bài Test Minh Họa & Các Kịch Bản

#### Kịch bản 1: Kiểm thử sinh ô bãi chuẩn quy ước Bay lẻ (Odd-Bay Convention)
```csharp
[Fact]
public void CreateStandard_ShouldGenerateSlotsOnlyForOddBays()
{
    // Arrange & Act: Block có 5 bays, 4 rows, 3 tiers
    // Bay lẻ: 1, 3, 5 (3 bays) => Số slot = 3 * 4 * 3 = 36 slots
    var block = YardBlock.CreateStandard("BLOCK-A", "Standard Block A", bays: 5, rows: 4, tiers: 3);

    // Assert: Xác nhận toàn bộ slot sinh ra chỉ nằm ở bay lẻ
    block.Slots.Should().HaveCount(36);
    block.Slots.All(s => s.Bay % 2 != 0).Should().BeTrue("YardSlot chỉ được phép tồn tại ở bay lẻ");
}
```
* **Giải thích**: Kiểm tra thuật toán `InitializeSlots` nhảy bước `bay += 2`, không bao giờ sinh ra ô tại các bay chẵn $2, 4$.

#### Kịch bản 2: Kiểm thử mở rộng quy mô bãi (`Expand`) sinh thêm slot lũy tiến
```csharp
[Fact]
public void Expand_ShouldIncreaseDimensionsAndGenerateNewSlots()
{
    // Arrange: Khởi tạo block ban đầu 3 bays, 2 rows, 2 tiers => 2 bay lẻ (1, 3) * 2 * 2 = 8 slots
    var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 3, rows: 2, tiers: 2);
    block.Slots.Should().HaveCount(8);

    // Act: Mở rộng lên 5 bays
    var result = block.Expand(newMaxBays: 5, newMaxRows: 2, newMaxTiers: 2);

    // Assert: Kích thước mới là 5 bays => 3 bay lẻ (1, 3, 5) * 2 * 2 = 12 slots
    result.IsSuccess.Should().BeTrue();
    block.MaxBays.Should().Be(5);
    block.Slots.Should().HaveCount(12);
    block.Slots.Should().Contain(s => s.Bay == 5 && s.Row == 1 && s.Tier == 1);
}
```
* **Giải thích**: Xác minh khi tăng chiều dài bãi từ 3 lên 5, hệ thống giữ nguyên 8 ô cũ và sinh thêm chính xác 4 ô mới tại bay 5.

#### Kịch bản 3: Kiểm thử Conflict Detection chặn thu hẹp bãi khi có container (Negative Path)
```csharp
[Fact]
public void Shrink_WhenSlotOccupied_ShouldReturnConflict()
{
    // Arrange: Block có 5 bays. Chiếm 1 ô tại bay 5
    var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 5, rows: 2, tiers: 2);
    block.OccupySlot(bay: 5, row: 1, tier: 1, Guid.NewGuid()).IsSuccess.Should().BeTrue();

    // Act: Cố gắng thu hẹp block về 3 bays (loại bỏ bay 5)
    var result = block.Shrink(newMaxBays: 3, newMaxRows: 2, newMaxTiers: 2);

    // Assert: Thao tác bị từ chối, block vẫn giữ nguyên 5 bays và 12 slots
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("YardBlock.ShrinkConflictWithOccupiedSlots");
    block.MaxBays.Should().Be(5);
    block.Slots.Should().HaveCount(12);
}
```
* **Giải thích**: Bảo đảm an toàn tuyệt đối: nếu trong vùng giải tỏa có container đang nằm, hệ thống kiên quyết từ chối thu hẹp bãi để không làm mất dấu vị trí vỏ container trên thực tế.
