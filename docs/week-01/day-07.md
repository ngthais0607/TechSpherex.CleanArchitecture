# Báo Cáo Kỹ Thuật: Ngày 7 - Làm Code Test & Chuẩn Bị Sang Tuần 2 Application (Xử Lý Nghiệp Vụ)

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 7**: Hoàn thiện toàn diện bộ Unit Test & Architecture Test (121 tests passed), Chuẩn hóa quy trình kiểm thử TDD/AAA & Sẵn sàng chuyển giao sang Tuần 2 (Application Layer)

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

1. **Bảo đảm chất lượng phần mềm bằng tự động hóa kiểm thử (Automated Testing)**:
   - Một tầng Domain phức tạp với hàng loạt thuật toán (ISO 6346 Modulo 11, Odd-Bay convention, Dual support physics, FSM Gate-In/Gate-Out) nếu không có hệ thống kiểm thử tự động toàn diện sẽ trở thành rủi ro lớn khi bước vào Tuần 2 xây dựng Use Cases.
   - Yêu cầu: Xây dựng bộ test phủ trọn vẹn 3 kịch bản:
     - *Happy Path*: Dữ liệu hợp lệ, hành vi hoạt động trơn tru.
     - *Boundary / Edge Cases*: Trường hợp biên (mép bãi, tầng trệt, số lượng vừa đúng hạn ngạch, container 40ft tại các vị trí tiếp giáp).
     - *Negative Path*: Dữ liệu sai, vi phạm quy tắc nghiệp vụ, trả về đúng mã lỗi mong đợi.
2. **Khử rủi ro phụ thuộc bằng Architecture Test (Architecture Fitness Functions)**:
   - Đảm bảo tính độc lập tuyệt đối của tầng Domain bằng bài test tự động phân tích binary assembly, ngăn chặn việc ai đó vô tình thêm reference trái phép.
3. **Chuẩn bị hạ tầng kỹ thuật sẵn sàng cho Tuần 2 (Application Layer)**:
   - Cấu hình dự án `SnpDepotManagement.Application` và `Application.UnitTests`.
   - Chuẩn bị các gói CPM cần thiết: `MediatR`, `FluentValidation` để ngay đầu Tuần 2 có thể bắt tay ngay vào việc lập trình các luồng nghiệp vụ (Commands/Queries/Validators).

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Phương Pháp AAA Pattern (Arrange - Act - Assert)**:
  - Mọi bài test đều tuân thủ cấu trúc 3 phần rõ ràng:
    - *Arrange*: Chuẩn bị dữ liệu đầu vào và trạng thái đối tượng.
    - *Act*: Thực thi phương thức cần kiểm thử.
    - *Assert*: Thẩm định kết quả đầu ra có khớp với kỳ vọng.
- **Tham Số Hóa Kiểm Thử (Parameterized Testing Với Theory/InlineData)**:
  - Sử dụng `[Theory]` và `[InlineData]` trong xUnit để kiểm thử một phương thức với hàng chục bộ dữ liệu khác nhau (ví dụ: các mã hãng tàu hợp lệ/không hợp lệ, các giá trị bay lẻ/chẵn) mà không cần viết lặp lại nhiều hàm test.
- **FluentAssertions Library**:
  - Dùng thư viện FluentAssertions để viết các mệnh đề khẳng định tự nhiên, biểu cảm như văn nói tiếng Anh (`result.IsSuccess.Should().BeTrue()`), giúp thông báo lỗi khi test fail trở nên rõ ràng và dễ chẩn đoán nguyên nhân.
- **NetArchTest.Rules**:
  - Tự động hóa kiểm thử kiến trúc (Architecture as Code).

### 2.2. Kiến Thức Áp Dụng
- **Test-Driven Development (TDD)**: Viết test trước để định hình thiết kế API và đảm bảo chất lượng.
- **Clean Architecture Testing**: Phân tách rõ ràng giữa Domain Unit Tests (chỉ test logic nghiệp vụ thuần túy trong RAM) và Integration Tests (tương tác database).
- **Central Package Management (CPM)**: Quản lý phiên bản các công cụ test đồng nhất.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

```text
src/
├── Domain/                                 # Nghiệp vụ lõi đã hoàn thành 100%
└── Application/                            # Dự án tầng Use Cases (sẵn sàng cho Tuần 2)
tests/
├── Domain.UnitTests/
│   ├── Common/                             # Tests cho Result, Error, BusinessRuleValidator
│   ├── Containers/                         # Tests cho Container, ContainerNumber (ISO 6346)
│   ├── MasterData/                         # Tests cho Customer, Vehicle, Voyage
│   ├── Yard/                               # Tests cho YardPosition, YardBlock, Expand, Shrink
│   ├── Operations/                         # Tests cho DeliveryOrder, ContainerLifecycle
│   └── Rules/                              # Tests cho 8 Business Rules (Yard & Dispatch)
├── Architecture.Tests/
│   └── DomainArchitectureTests.cs          # Test ranh giới độc lập của tầng Domain
└── Application.UnitTests/                  # Sẵn sàng tiếp nhận test CQRS trong Tuần 2
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng thống kê các phương thức test tiêu biểu đại diện cho toàn bộ suite test (121 tests):

| Tên Hàm (Signature) | Tham Số / Test Type | Đối Tượng Kiểm Thử | Chức Năng & Kỳ Vọng Trong Luồng Test |
|---|---|---|---|
| `Create_WithValidIsoNumber_ShouldReturnSuccess()` | `[Theory] [InlineData]` | `ContainerNumber.Create` | Kiểm tra các chuỗi số cont chuẩn quốc tế; kỳ vọng trả về `IsSuccess = true`. |
| `Create_WithInvalidCheckDigit_ShouldReturnMismatch()`| `[Fact]` | `ContainerNumber.Create` | Truyền chuỗi có số kiểm tra sai; kỳ vọng trả về lỗi `CheckDigitMismatch`. |
| `Shrink_WhenSlotOccupied_ShouldReturnConflict()` | `[Fact]` | `YardBlock.Shrink` | Chiếm 1 ô trong vùng giải tỏa rồi gọi `Shrink`; kỳ vọng trả về lỗi `Conflict` và không xóa ô. |
| `IsBroken_When40ftAtBlockBoundary_ShouldReturnTrue()` | `[Theory] [InlineData]` | `ValidBayContainerSizeRule` | Đặt cont 40ft ở mép ngoài cùng của block; kỳ vọng `IsBroken() == true` chặn mép bãi. |
| `Allocate_WhenExceedingQuantity_ShouldReturnFailure()`| `[Fact]` | `DeliveryOrderItem.Allocate` | Yêu cầu cấp số lượng lớn hơn hạn ngạch còn lại; kỳ vọng trả về lỗi `QuantityExceeded`. |
| `GateOut_WhenAlreadyGatedOut_ShouldReturnConflict()` | `[Fact]` | `ContainerLifecycle.GateOut` | Gọi `GateOut` lần thứ 2 trên cùng một vòng đời; kỳ vọng từ chối xuất 2 lần. |
| `Domain_ShouldNotHaveDependenciesOnOtherProjects()` | `[Fact]` | `Types.InAssembly` | Quét toàn bộ assembly Domain; kỳ vọng không có bất kỳ phụ thuộc nào vào Application/Infra. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Thuộc Tính (Attributes) `[Fact]`, `[Theory]`, `[InlineData]` Trong xUnit
```csharp
[Theory]
[InlineData("CSQU3054383", true)]
[InlineData("CSQU3054389", false)] // Sai check digit
public void ValidateContainerNumber(string raw, bool expected)
{
    var result = ContainerNumber.Create(raw);
    result.IsSuccess.Should().Be(expected);
}
```
- `[Fact]`: Đánh dấu phương thức là một Unit Test đơn lẻ, chạy với một kịch bản dữ liệu cố định không có tham số.
- `[Theory]`: Đánh dấu phương thức kiểm thử tham số hóa (Parameterized Test). xUnit sẽ gọi hàm test nhiều lần, mỗi lần nạp một bộ tham số từ các attribute dữ liệu bên dưới.
- `[InlineData(...)]`: Cung cấp giá trị cụ thể cho các tham số của hàm test.

### 5.2. Phương Thức Mở Rộng (Extension Methods) Của FluentAssertions
```csharp
result.IsSuccess.Should().BeTrue();
result.Error.Code.Should().Be("ContainerNumber.InvalidLength");
```
- `.Should()`: Là một Extension Method gắn thêm vào mọi đối tượng trong C# nhờ thư viện `FluentAssertions`.
- Cho phép viết các câu lệnh kiểm tra dạng chuỗi phương thức (Method Chaining), biến mã nguồn test thành tài liệu kỹ thuật sống (Living Documentation).

### 5.3. Kiểm Thử Ngoại Lệ Nghiệp Vụ Với `Action` Delegate
```csharp
Action act = () => BusinessRuleValidator.Check(brokenRule);
act.Should().Throw<BusinessRuleException>()
   .Which.Rule.Should().Be(brokenRule);
```
- `Action act = () => ...`: Đóng gói hành vi thực thi vào một delegate (hàm ẩn danh) mà không chạy ngay.
- `act.Should().Throw<...>()`: Thực thi delegate bên trong khối `try-catch` ngầm định của thư viện test để bắt ngoại lệ và kiểm tra đúng kiểu lỗi cũng như thuộc tính bên trong của ngoại lệ.

---

## 6. Thiết Kế Bài Test Thực Tế & Cách Vận Hành Test Suite

### 6.1. Giải Phẫu Chi Tiết Một Bài Test Hoàn Chỉnh (Anatomy of a Unit Test)

Mỗi bài Unit Test trong dự án là một hàm C# độc lập được thực thi bởi Test Runner (xUnit). Cấu trúc điển hình:

```csharp
[Fact]
public void GateOut_WhenAlreadyGatedOut_ShouldReturnConflict()
{
    // 1. ARRANGE (Chuẩn bị trạng thái):
    // Mô phỏng container đã được Gate-In vào bãi lúc 2 tiếng trước,
    // và vừa Gate-Out ra cổng lúc 1 tiếng trước.
    var containerId = Guid.NewGuid();
    var lifecycle = ContainerLifecycle.GateIn(containerId, "A-01-01-01", DateTime.UtcNow.AddHours(-2)).Value!;
    var firstGateOutResult = lifecycle.GateOut(DateTime.UtcNow.AddHours(-1), "51C-12345", "DO-001");
    firstGateOutResult.IsSuccess.Should().BeTrue();

    // 2. ACT (Hành động thực thi):
    // Tài xế khác hoặc cùng xe quét thẻ xuất lần 2
    var secondGateOutResult = lifecycle.GateOut(DateTime.UtcNow, "51C-99999", "DO-002");

    // 3. ASSERT (Khẳng định kết quả):
    // Kết quả lần 2 bắt buộc phải thất bại (IsFailure == true)
    secondGateOutResult.IsFailure.Should().BeTrue();
    // Mã lỗi nghiệp vụ trả về phải chính xác là AlreadyGatedOut
    secondGateOutResult.Error.Code.Should().Be("ContainerLifecycle.AlreadyGatedOut");
}
```

### 6.2. Cơ Chế Chạy Test & Báo Cáo Kết Quả Trong Terminal

Khi chạy lệnh sau tại thư mục gốc:
```powershell
dotnet test SnpDepotManagement.sln
```

1. **Khám phá bài test (Test Discovery)**:
   - xUnit quét toàn bộ assembly `.dll` đã build, tìm các class `public` chứa các phương thức có gắn thuộc tính `[Fact]` hoặc `[Theory]`.
2. **Khởi tạo và Thực thi độc lập (Isolation)**:
   - Mỗi test method được thực thi trên một instance class test mới hoàn toàn. Điều này đảm bảo **Zero Shared State** (các bài test không chia sẻ dữ liệu bộ nhớ, không gây ảnh hưởng chéo).
3. **Đánh giá kết quả (Evaluation & Reporting)**:
   - Nếu toàn bộ các câu lệnh `Should()` đều thỏa mãn, test được đánh dấu `Passed` (màu xanh).
   - Nếu có bất kỳ câu lệnh `Should()` nào không khớp (ví dụ kỳ vọng `true` nhưng thực tế là `false`), một ngoại lệ `AssertionFailedException` được ném ra và in chi tiết sự sai lệch lên màn hình console.

### 6.3. Bảng Tổng Kết Kết Quả Chạy Toàn Bộ Test Suite

```text
Passed!  - Failed: 0, Passed: 120, Skipped: 0, Total: 120, Duration: 452 ms - SnpDepotManagement.Domain.UnitTests.dll (net10.0)
Passed!  - Failed: 0, Passed:   1, Skipped: 0, Total:   1, Duration:   1 s  - SnpDepotManagement.Architecture.Tests.dll (net10.0)
-------------------------------------------------------------------------------------------------------------------------
TỔNG CỘNG: 121 / 121 Tests PASSED (100% Tỷ lệ thành công) | Thời gian chạy: ~1.5 giây
```

> **Kết luận**: Tầng Domain đã hoàn tất với bộ test bao phủ toàn diện 100% các yêu cầu nghiệp vụ và kỹ thuật, sẵn sàng chuyển tiếp sang **Tuần 2: Application Layer (Xử lý nghiệp vụ Use Cases & CQRS)**.
