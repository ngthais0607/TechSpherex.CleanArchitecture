# Báo Cáo Kỹ Thuật: Ngày 5 - Làm Lệnh Giao Và Vòng Đời Container (Vào/Ra Bãi)

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 5**: Lệnh giao vỏ (`DeliveryOrder`), Phân bổ hạn mức (`DeliveryOrderItem`), Máy trạng thái vòng đời (`ContainerLifecycle`), Bộ 4 quy tắc xuất vỏ & Architecture Test

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

1. **Đặc thù nghiệp vụ cấp vỏ rỗng (Empty Container Dispatching)**:
   - Hãng tàu (Line Operator) phát hành Lệnh Giao Vỏ (**Delivery Order - DO**) cho chủ hàng. Chủ hàng điều động xe kéo đến bãi để nhận container rỗng đạt chuẩn đóng hàng xuất khẩu.
   - Một lệnh DO có thể yêu cầu nhiều nhóm vỏ khác nhau (ví dụ: 10 vỏ 20ft Dry và 5 vỏ 40ft Reefer). Hệ thống phải quản lý chi tiết từng dòng hàng (`DeliveryOrderItem`).
2. **Kiểm soát hạn mức cấp phát nguyên tử (`Allocate`)**:
   - Mỗi item có hạn mức được duyệt (`RequiredQuantity`) và số lượng thực tế đã bốc (`DeliveredQuantity`).
   - Biến tính toán bất biến: $\text{RemainingQuantity} = \text{RequiredQuantity} - \text{DeliveredQuantity}$.
   - Chặn đứng hoàn toàn việc cấp phát vượt hạn ngạch cho phép (`quantity > RemainingQuantity`). Khi toàn bộ các dòng hàng đã cấp đủ, lệnh DO tự động chuyển sang trạng thái hoàn tất (`IsCompleted = true`) và khóa lại để bảo toàn dữ liệu lịch sử.
3. **Mô hình máy trạng thái hữu hạn (FSM) vòng đời container (`ContainerLifecycle`)**:
   - Vỏ rỗng lưu bãi trải qua các trạng thái: Tiếp nhận vào bãi (**Gate-In** $\rightarrow$ `InYard`) và Xuất bãi bàn giao cho khách (**Gate-Out** $\rightarrow$ `GatedOut`).
   - **Chặn đứng gian lận và lỗi thao tác**: Nghiêm cấm xuất 2 lần trên cùng một vòng đời. Nếu container đã ở trạng thái `GatedOut`, mọi thao tác quét thẻ xuất tiếp theo đều bị chặn lại.
4. **Bộ 4 quy tắc chốt chặn xuất container ra cổng (Dispatch Rules)**:
   - `DeliveryOrderNotExpiredRule`: Ngày xe đến nhận vỏ phải nằm trong hạn hiệu lực của lệnh giao ($\text{dispatchDate} \le \text{ExpiryDate}$).
   - `DeliveryOrderHasRemainingQuantityRule`: Lệnh giao phải còn số lượng vỏ chưa lấy đúng với kích cỡ (Size) và loại vỏ (Type) yêu cầu.
   - `ContainerBelongsToLineOperatorRule`: Container được chỉ định phải thuộc quyền sở hữu của chính hãng tàu phát hành lệnh.
   - `ContainerIsInYardRule`: Container phải đang thực sự nằm trong bãi (`Status == InYard`).
5. **Bảo vệ ranh giới kiến trúc bằng Architecture Test**:
   - Tự động hóa việc kiểm tra mã nguồn để đảm bảo tầng `Domain` không bị bất kỳ lập trình viên nào vô tình tham chiếu ngược sang tầng ngoài (`Application`, `Infrastructure`, `Api`).

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Aggregate Root Pattern (`DeliveryOrder`)**:
  - `DeliveryOrder` là Aggregate Root bảo vệ toàn vẹn dữ liệu cho danh sách thực thể con `DeliveryOrderItem`. Việc thêm dòng hàng (`AddItem`) hoặc phân bổ số lượng (`ValidateForDispatch`) bắt buộc phải thông qua Aggregate Root.
- **Finite State Machine (FSM)**:
  - Triển khai trong `ContainerLifecycle`:
    $$\text{Create} \xrightarrow{\text{GateIn}} \text{InYard} \xrightarrow{\text{GateOut}} \text{GatedOut}$$
  - Đóng gói logic chuyển trạng thái an toàn, ghi nhận chính xác dấu vết thời gian, vị trí bãi và phương tiện vận chuyển.
- **Specification / Business Rule Pipeline**:
  - 4 quy tắc xuất vỏ được thực thi tuần tự thông qua `BusinessRuleValidator.Validate(...)` trước khi thủ tục Gate-Out được phê duyệt.
- **Architecture as Code (`NetArchTest.Rules`)**:
  - Viết Unit Test đặc biệt để quét metadata của binary assembly tầng Domain, tự động báo lỗi nếu phát hiện vi phạm nguyên tắc phụ thuộc một chiều của Clean Architecture.

### 2.2. Kiến Thức Áp Dụng
- **Clean Architecture**: Xác lập nguyên tắc $0\text{ external dependencies}$ cho Domain.
- **Domain-Driven Design (DDD)**: Aggregate Boundary và Invariants bảo vệ tính nhất quán của hạn ngạch cấp vỏ.
- **OOP (Encapsulation)**: Private collections và private setters bảo vệ toàn vẹn trạng thái vòng đời.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

```text
Domain/
├── Enums/
│   └── OperationEnums.cs                  # Enum LifecycleStatus (InYard, GatedOut)
├── Entities/
│   └── Operations/
│       ├── DeliveryOrder.cs               # Aggregate Root lệnh giao vỏ (DO)
│       ├── DeliveryOrderItem.cs           # Thực thể dòng hàng quota (Required/Delivered)
│       └── ContainerLifecycle.cs          # Thực thể máy trạng thái vòng đời vỏ rỗng
└── Rules/
    ├── DeliveryOrderNotExpiredRule.cs     # Kiểm tra ngày bốc vỏ không quá hạn lệnh
    ├── DeliveryOrderHasRemainingQuantityRule.cs # Kiểm tra còn quota theo Size/Type
    ├── ContainerBelongsToLineOperatorRule.cs # Kiểm tra cont thuộc đúng hãng tàu trên lệnh
    └── ContainerIsInYardRule.cs           # Kiểm tra cont đang thực sự ở trong bãi
tests/
├── Domain.UnitTests/
│   ├── Operations/                        # Unit tests cho DeliveryOrder và ContainerLifecycle
│   └── Rules/                             # Unit tests cho 4 Dispatch Rules
└── Architecture.Tests/
    └── DomainArchitectureTests.cs         # Test kiểm tra tính độc lập tuyệt đối của Domain
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng thống kê các phương thức cốt lõi được xây dựng trong ngày:

| Tên Hàm (Signature) | Tham Số Đầu Vào | Đầu Ra (Return) | Chức Năng & Vai Trò Trong Luồng |
|---|---|---|---|
| `DeliveryOrder.Create(...)` | Số lệnh, hạn lệnh, mã hãng tàu, mã KH, mã tàu | `Result<DeliveryOrder>` | Khởi tạo lệnh giao vỏ rỗng, kiểm tra số lệnh không rỗng và hạn lệnh hợp lệ. |
| `DeliveryOrder.AddItem(...)` | Size, Type, số lượng cần lấy | `Result` | Thêm dòng yêu cầu cấp vỏ vào lệnh; khóa không cho thêm nếu lệnh đã hoàn tất. |
| `DeliveryOrder.ValidateForDispatch(...)` | Size, Type, ngày xuất, hãng tàu | `Result` | Thẩm định tổng thể trước khi bốc vỏ: kiểm tra hạn lệnh, hãng tàu và phân bổ trừ quota. |
| `DeliveryOrderItem.Create(...)` | Size, Type, requiredQuantity | `Result<DeliveryOrderItem>` | Khởi tạo dòng hàng với số lượng yêu cầu $> 0$. |
| `DeliveryOrderItem.Allocate(quantity)` | Số lượng cần bốc (mặc định 1) | `Result` | Trừ quota nguyên tử: tăng `DeliveredQuantity`, từ chối nếu vượt quá `RemainingQuantity`. |
| `ContainerLifecycle.GateIn(...)` | Id container, tọa độ bãi, thời gian vào | `Result<ContainerLifecycle>` | Factory method ghi nhận container vào bãi, chuyển trạng thái sang `InYard`. |
| `ContainerLifecycle.GateOut(...)` | Thời gian ra, biển số xe, số lệnh DO | `Result` | Chuyển trạng thái sang `GatedOut`, xóa tọa độ bãi; từ chối nếu cont đã xuất trước đó. |
| `DeliveryOrderNotExpiredRule.IsBroken()` | Không | `bool` | So sánh ngày xuất với hạn lệnh: trả về `true` nếu ngày xuất $> \text{ExpiryDate}$. |
| `DeliveryOrderHasRemainingQuantityRule.IsBroken()`| Không | `bool` | Kiểm tra còn quota: trả về `true` nếu không tìm thấy item khớp Size/Type hoặc `RemainingQuantity <= 0`. |
| `ContainerBelongsToLineOperatorRule.IsBroken()` | Không | `bool` | Kiểm tra khớp hãng tàu: trả về `true` nếu mã hãng tàu của cont khác với mã trên lệnh. |
| `ContainerIsInYardRule.IsBroken()` | Không | `bool` | Kiểm tra vị trí: trả về `true` nếu container không ở trạng thái `InYard`. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Thuộc Tính Tính Toán Trong Lớp Con `DeliveryOrderItem`
```csharp
public int RemainingQuantity => RequiredQuantity - DeliveredQuantity;
```
- Sử dụng cú pháp rút gọn `=>`. Không lưu trữ một trường riêng trong cơ sở dữ liệu để tránh nguy cơ bất đồng bộ dữ liệu giữa `Required`, `Delivered` và `Remaining`. Giá trị luôn được tính toán tức thì theo thời gian thực.

### 5.2. So Sánh Thời Gian Bỏ Qua Giờ Phút Giây (`Date` Component)
```csharp
if (dispatchDate.Date > _expiryDate.Date)
```
- Khi so sánh hạn lệnh giao vỏ, việc so sánh trực tiếp hai biến `DateTime` có thể gây sai sót do thành phần giờ:phút:giây (ví dụ: lệnh hết hạn lúc `00:00:00` sáng nhưng xe đến lấy hàng lúc `08:00:00` cùng ngày).
- Sử dụng thuộc tính `.Date` sẽ đưa cả hai thời điểm về mốc `00:00:00` của ngày đó, đảm bảo khách hàng có quyền lấy vỏ đến hết ngày cuối cùng của hạn lệnh.

### 5.3. Architecture Testing Với `NetArchTest.Rules`
```csharp
var result = Types.InAssembly(domainAssembly)
    .ShouldNot()
    .HaveDependencyOnAny("SnpDepotManagement.Application", "SnpDepotManagement.Infrastructure")
    .GetResult();

result.IsSuccessful.Should().BeTrue();
```
- **Ý nghĩa cú pháp**: Sử dụng Reflection và phân tích mã IL (Intermediate Language) để thẩm định kiến trúc.
- Nếu một lập trình viên vô tình thêm `using SnpDepotManagement.Infrastructure;` vào bất kỳ file nào trong Domain, bài test này sẽ lập tức báo `Failed` ngay trong quá trình CI/CD build, bảo vệ tuyệt đối tính toàn vẹn của Clean Architecture.

---

## 6. Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)

### 6.1. Mục Tiêu & Cấu Trúc Kiểm Thử Ngày 5
Bộ test Ngày 5 bao gồm:
- **Unit Tests Tác Nghiệp Cổng Bãi**: Kiểm tra việc trừ quota hạn mức trong `DeliveryOrder` và FSM chuyển trạng thái trong `ContainerLifecycle`.
- **Unit Tests 4 Quy Tắc Gate-Out**: Kiểm tra chốt chặn an toàn khi xuất vỏ ra khỏi depot.
- **Architecture Test**: Kiểm tra tự động tính độc lập kiến trúc của Domain.

### 6.2. Mã Nguồn Bài Test Minh Họa & Các Kịch Bản

#### Kịch bản 1: Kiểm thử trừ quota nguyên tử trong `DeliveryOrderItem`
```csharp
[Fact]
public void Allocate_ShouldDecreaseRemainingQuantity()
{
    // Arrange: Khởi tạo lệnh DO có dòng hàng 5 cont 20ft Dry
    var order = DeliveryOrder.Create("DO-01", "MSC", "0123456789", "CTY A", DateTime.UtcNow.AddDays(5), "VESSEL-01").Value!;
    order.AddItem(ContainerSize.Ft20, ContainerType.Dry, quantity: 5);
    var item = order.Items.First();

    // Act: Cấp phát 2 container
    var result = item.Allocate(2);

    // Assert: Đã giao 2, quota còn lại chính xác là 3
    result.IsSuccess.Should().BeTrue();
    item.DeliveredQuantity.Should().Be(2);
    item.RemainingQuantity.Should().Be(3);
}
```
* **Giải thích**: Xác minh khi cấp phát hợp lệ, số lượng đã giao tăng lên và số lượng còn lại giảm tương ứng.

#### Kịch bản 2: Kiểm thử chặn cấp phát vượt quota cho phép (Error Handling)
```csharp
[Fact]
public void Allocate_WhenExceedingRemainingQuantity_ShouldReturnError()
{
    // Arrange: Lệnh còn lại 5 container
    var order = DeliveryOrder.Create("DO-01", "MSC", "0123456789", "CTY A", DateTime.UtcNow.AddDays(5), "VESSEL-01").Value!;
    order.AddItem(ContainerSize.Ft20, ContainerType.Dry, quantity: 5);
    var item = order.Items.First();

    // Act: Cố tình yêu cầu cấp 6 container
    var result = item.Allocate(6);

    // Assert: Thao tác bị chặn đứng nguyên tử
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("DeliveryOrderItem.QuantityExceeded");
    item.DeliveredQuantity.Should().Be(0);
    item.RemainingQuantity.Should().Be(5);
}
```
* **Giải thích**: Bảo đảm không một chủ hàng nào có thể lấy vượt số lượng container mà hãng tàu đã phê duyệt.

#### Kịch bản 3: Kiểm thử chặn quét xuất 2 lần trên một vòng đời (FSM Gate-Out)
```csharp
[Fact]
public void GateOut_WhenAlreadyGatedOut_ShouldReturnConflict()
{
    // Arrange: Vỏ container vào bãi rồi đã xuất bãi thành công lần 1
    var lifecycle = ContainerLifecycle.GateIn(Guid.NewGuid(), "A-01-01-01", DateTime.UtcNow.AddHours(-2)).Value!;
    lifecycle.GateOut(DateTime.UtcNow.AddHours(-1), "51C-12345", "DO-01").IsSuccess.Should().BeTrue();

    // Act: Cố tình gọi GateOut lần thứ 2 trên cùng vòng đời này
    var result = lifecycle.GateOut(DateTime.UtcNow, "51C-99999", "DO-02");

    // Assert: Bị từ chối ngay lập tức vì trạng thái đã là GatedOut
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("ContainerLifecycle.AlreadyGatedOut");
}
```
* **Giải thích**: Ngăn chặn triệt để lỗ hổng gian lận quét thẻ xuất hai lần đối với cùng một container vỏ rỗng.

#### Kịch bản 4: Kiểm thử bảo vệ kiến trúc Clean Architecture (`Architecture Test`)
```csharp
[Fact]
public void Domain_ShouldNotHaveDependenciesOnOtherProjects()
{
    // Arrange: Quét assembly của dự án SnpDepotManagement.Domain
    var domainAssembly = typeof(BaseEntity).Assembly;

    // Act: Thẩm định xem Domain có phụ thuộc vào Application hay Infrastructure không
    var result = Types.InAssembly(domainAssembly)
        .ShouldNot()
        .HaveDependencyOnAny("SnpDepotManagement.Application", "SnpDepotManagement.Infrastructure")
        .GetResult();

    // Assert: Bắt buộc phải thỏa mãn 100%
    result.IsSuccessful.Should().BeTrue();
}
```
* **Giải thích**: Bảo đảm tính toàn vẹn của mô hình Onion Architecture. Nếu ai đó thêm thư viện hạ tầng vào Domain, bài test này sẽ fail ngay lập tức.
