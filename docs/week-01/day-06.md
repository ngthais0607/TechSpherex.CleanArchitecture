# Báo Cáo Kỹ Thuật: Ngày 6 - Check Lại Code, Double-Check Logic & Nghiên Cứu Bước Thiết Kế Tiếp Theo

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 6**: Rà soát chất lượng mã nguồn (Code Review), Thẩm định lại toàn bộ logic bất biến (Invariants Double-Check) & Thiết kế nền tảng cho Tuần 2 (Application Layer)

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

1. **Rà soát toàn diện tính thuần khiết của Domain (Domain Model Purity)**:
   - Đảm bảo toàn bộ 100% mã nguồn trong `SnpDepotManagement.Domain` là mã C# thuần túy (POCO), không bị rò rỉ bất kỳ khái niệm nào của cơ sở dữ liệu (Entity Framework Core, SQL, bảng, khóa ngoại) hay tầng hiển thị (HTTP, Controllers, JSON).
2. **Double-Check các điểm nút logic bất biến (Invariants Checklist)**:
   - *Số container chuẩn ISO 6346*: Xác minh lại toàn bộ bảng mã chữ cái, thuật toán Modulo 11 và khả năng loại bỏ khoảng trắng, ký tự đặc biệt.
   - *Bãi container 3 chiều*: Rà soát lại công thức tính tổng slot bay lẻ, thuật toán mở rộng lũy tiến (`Expand`) và thuật toán phát hiện xung đột nguyên tử (`Shrink`) khi có container đang chiếm chỗ.
   - *An toàn xếp bãi vật lý*: Rà soát lại trường hợp mép block (Boundary Overhang), tầng trệt và hai chân đỡ (Dual Supporting Stack Physics) của container 40ft.
   - *Vòng đời và lệnh giao*: Kiểm tra việc trừ hạn mức (`Allocate`) và ngăn chặn việc quét xuất 2 lần (`GateOut`) trên cùng một vòng đời lưu bãi.
3. **Nghiên cứu & Thiết lập bước chuyển tiếp sang Tuần 2 (Application Layer)**:
   - Sau khi hoàn thành lõi Domain, bài toán tiếp theo là: Làm thế nào để các Use Cases (tác nghiệp bốc dỡ, điều độ bãi, xác thực) tương tác với Domain một cách an toàn, có khả năng mở rộng cao và không làm bẩn Domain?
   - Cần nghiên cứu kiến trúc **CQRS** (Command Query Responsibility Segregation) và mô hình **Mediator** (thông qua MediatR), kèm cơ chế **Domain Events** để phục vụ việc kích hoạt các luồng công việc liên quan (gửi thông báo, ghi log kiểm toán, tích hợp hệ thống ngoài).

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Defensive Programming & Invariant Enforcement**:
  - Mọi Aggregate Root (`YardBlock`, `DeliveryOrder`, `ContainerLifecycle`) đều tự chịu trách nhiệm bảo vệ trạng thái của mình thông qua các phương thức nghiệp vụ tường minh; không có bất kỳ `public setter` nào.
- **Mô Hình CQRS (Command Query Responsibility Segregation)**:
  - Tách rời hoàn toàn luồng ghi (Command: thêm container, mở rộng bãi, cấp vỏ) và luồng đọc (Query: xem sơ đồ bãi, tra cứu thông tin vỏ, kiểm tra hạn lệnh).
- **Mediator Pattern (Chuẩn Bị Cho Tuần 2)**:
  - Sử dụng MediatR để biến mỗi nghiệp vụ thành một cặp `IRequest` (Command/Query) và `IRequestHandler`. Controller/API chỉ việc gửi Request vào Mediator mà không cần biết logic chi tiết nằm ở đâu, triệt tiêu sự phụ thuộc chéo.
- **Mô Hình Sự Kiện Miền (Domain Events Pattern)**:
  - Thiết kế trừu tượng `IDomainEvent`. Khi một nghiệp vụ hoàn tất (ví dụ: `ContainerGatedOut`), Aggregate Root sẽ phát ra sự kiện để các Handler khác xử lý không đồng bộ (như giải phóng ô bãi, thông báo hãng tàu) mà không làm phức tạp hóa nghiệp vụ chính.

### 2.2. Kiến Thức Áp Dụng
- **Clean Architecture & Onion Architecture**: Duy trì ranh giới độc lập tuyệt đối của tầng lõi.
- **Domain-Driven Design (DDD)**: Đảm bảo nguyên tắc "Aggregate Root là cổng giao tiếp duy nhất".
- **SOLID**:
  - `SRP`: Tách biệt rành mạch giữa nghiệp vụ lõi (Domain) và luồng điều phối use-case (Application).
  - `DIP`: Chuẩn bị các interfaces trừu tượng (`IContainerRepository`, `IUnitOfWork`) tại tầng Domain/Application để tầng Infrastructure hiện thực.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

Cấu trúc hiện tại và phần định hướng chuẩn bị cho Tuần 2:

```text
Domain/
├── Common/                          # Result, Error, BaseEntity, AuditableEntity, IBusinessRule
├── Entities/                        # Container, YardBlock, DeliveryOrder, ContainerLifecycle
├── ValueObjects/                    # ContainerNumber, YardPosition
├── Rules/                           # Bộ 8 Business Rules (Yard & Dispatch)
└── Events/ (Chuẩn bị)               # Hợp đồng sự kiện miền IDomainEvent
Application/ (Khung chuẩn bị)
├── Common/                          # Pipeline Behaviors (Validation, Logging)
├── Containers/                      # Commands & Queries quản lý vỏ container
├── Yard/                            # Commands & Queries quản lý bãi và vị trí
└── Operations/                      # Commands & Queries tác nghiệp Gate-In / Gate-Out
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng tổng hợp các hàm cốt lõi đã rà soát và các hợp đồng định hướng cho bước tiếp theo:

| Tên Hàm (Signature) | Tham Số Đầu Vào | Đầu Ra (Return) | Chức Năng & Vai Trò Trong Luồng |
|---|---|---|---|
| `ContainerNumber.Create(rawInput)` | Chuỗi số cont thô | `Result<ContainerNumber>` | Rà soát: Kiểm tra tính toàn vẹn 11 ký tự, Modulo 11 và 7 mã lỗi chuyên biệt. |
| `YardBlock.Shrink(bays, rows, tiers)` | Kích thước mới | `Result` | Rà soát: Thuật toán Conflict Detection chặn đứng nguyên tử việc thu bãi khi có cont. |
| `SlotBelowMustBeOccupiedRule.IsBroken()` | Không | `bool` | Rà soát: Thẩm định vật lý 2 chân đế của cont 40ft tại tầng $> 1$. |
| `DeliveryOrderItem.Allocate(quantity)` | Số lượng cần bốc | `Result` | Rà soát: Trừ quota nguyên tử, ngăn chặn việc lấy vỏ vượt quá hạn ngạch. |
| `ContainerLifecycle.GateOut(...)` | Giờ ra, biển số, số DO | `Result` | Rà soát: Chuyển trạng thái sang `GatedOut`, ngăn chặn việc xuất 2 lần. |
| `IRequestHandler<TRequest, TResponse>.Handle(...)` *(Định hướng T2)* | Request, CancellationToken | `Task<TResponse>` | Hợp đồng xử lý Use Case độc lập cho từng Command/Query trong MediatR. |
| `IPipelineBehavior<TRequest, TResponse>.Handle(...)` *(Định hướng T2)* | Request, NextHandler, CancellationToken | `Task<TResponse>` | Middleware cho luồng Application: tự động validate dữ liệu và log vết trước khi vào Handler. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Ràng Buộc Generic (Generic Constraints) Với Từ Khóa `where`
```csharp
public interface ICommandHandler<in TCommand, TResult> 
    where TCommand : ICommand<TResult>
```
- `where TCommand : ICommand<TResult>`: Ép kiểu tham số Generic phải là một lớp triển khai `ICommand<TResult>`.
- Trình biên dịch C# sẽ ngăn chặn việc truyền vào các kiểu dữ liệu không hợp lệ ngay tại thời điểm biên dịch (Compile-time type safety).

### 5.2. Từ Khóa Phương Chiều `in` Và `out` (Variance in Generics)
- `in TCommand` (Contravariance - Nghịch biến): Cho phép truyền vào kiểu cha hoặc kiểu con một cách linh hoạt ở vị trí tham số đầu vào.
- `out TResult` (Covariance - Đồng biến): Chỉ định kiểu trả về của interface, cho phép gán một đối tượng có kiểu trả về cụ thể hơn cho một biến có kiểu tổng quát hơn.

### 5.3. Biểu Thức Khớp Mẫu (Pattern Matching) Với Biểu Thức `switch`
```csharp
return condition switch
{
    ContainerCondition.Damaged => Classification.C,
    ContainerCondition.UnderRepair => Classification.C,
    _ => Classification.A
};
```
- Thay thế cho cấu trúc `switch-case` truyền thống dài dòng.
- Cú pháp `_ =>` (Discard pattern) đại diện cho nhánh mặc định (`default`). Biểu thức `switch` ngắn gọn, biểu cảm và được tối ưu hóa hiệu năng nhảy nhánh (branch table).

### 5.4. Toán Tử Gán Điều Kiện Null `??=` (Null-Coalescing Assignment)
```csharp
_items ??= new List<DeliveryOrderItem>();
```
- Nếu `_items` đang là `null`, nó sẽ được khởi tạo bằng `new List<...>()`. Nếu `_items` đã có giá trị, dòng lệnh không làm gì cả. Giúp mã nguồn phòng thủ tránh ngoại lệ `NullReferenceException` một cách ngắn gọn.

---

## 6. Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)

### 6.1. Mục Tiêu & Kỹ Thuật Double-Check Test Trong Ngày 6
Mục tiêu kiểm thử của Ngày 6 là sử dụng toàn bộ bộ test hiện có như một **Hàng rào kiểm định hồi quy (Regression Harness)**:
- Đảm bảo việc tái cấu trúc mã nguồn (Refactoring) và bổ sung các hàm phòng vệ không làm phá vỡ bất kỳ quy tắc nghiệp vụ cũ nào.
- Viết thêm các bài test bao phủ các điểm nút biên (Edge Boundary Tests) và kiểm tra khả năng bất biến tuyệt đối của Domain Entities.

### 6.2. Mã Nguồn Bài Test Minh Họa & Các Kịch Bản

#### Kịch bản 1: Kiểm thử bất biến trọng lượng Container (Payload Invariant Test)
```csharp
[Fact]
public void Create_WhenMaxGrossWeightLessThanOrEqualToTareWeight_ShouldReturnError()
{
    // Arrange: Khởi tạo container với MaxGross (2000kg) <= TareWeight (2200kg) -> Vô lý
    var containerNumber = ContainerNumber.Create("CSQU3054383").Value!;

    // Act
    var result = Container.Create(
        containerNumber, "22G1", ContainerSize.Ft20, ContainerType.Dry,
        tareWeight: 2200, maxGrossWeight: 2000, null, "MSC");

    // Assert: Trả về lỗi vì tổng tải không thể nhỏ hơn xác vỏ
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("Container.InvalidMaxGrossWeight");
}
```
* **Giải thích**: Double-check logic bảo vệ trọng lượng: một chiếc container không thể có trọng lượng toàn tải nhỏ hơn trọng lượng bản thân vỏ.

#### Kịch bản 2: Kiểm thử tính bất biến của danh sách Slots trong `YardBlock`
```csharp
[Fact]
public void YardBlock_SlotsCollection_ShouldBeReadOnly_CannotBeModifiedDirectly()
{
    // Arrange: Khởi tạo block chuẩn
    var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 3, rows: 2, tiers: 2);

    // Act & Assert: Thuộc tính Slots phải là kiểu IReadOnlyCollection
    block.Slots.Should().BeAssignableTo<IReadOnlyCollection<YardSlot>>();
    
    // Ép kiểu sang IList để cố tình thêm phần tử từ bên ngoài sẽ thất bại
    Action act = () => ((IList<YardSlot>)block.Slots).Add(null!);
    act.Should().Throw<NotSupportedException>();
}
```
* **Giải thích**: Kiểm tra tính đóng gói: mã nguồn ngoài không thể can thiệp trực tiếp vào danh sách slot của block, bảo đảm Aggregate Root giữ trọn quyền kiểm soát.
