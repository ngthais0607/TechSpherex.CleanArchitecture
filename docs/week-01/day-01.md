# Báo Cáo Kỹ Thuật: Ngày 1 - Dựng Khung Dự Án & Cấu Trúc Thư Mục

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 1**: Khung kiến trúc Clean Architecture, Quản lý gói tập trung (CPM) & Hệ thống nền tảng dùng chung (Domain Common)

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

Trong ngày đầu tiên xây dựng nền móng cho hệ thống quản lý Depot container rỗng, các bài toán logic và kỹ thuật cốt lõi cần giải quyết bao gồm:

1. **Kiểm soát luồng xử lý và loại bỏ Exception-Driven Flow**:
   - Trong môi trường cảng và kho bãi với hàng nghìn lượt xe ra vào mỗi ngày, dữ liệu đầu vào không hợp lệ (nhập sai số cont, sai tải trọng, sai hạn lệnh) là **tình huống nghiệp vụ thông thường**, không phải lỗi hệ thống (unhandled crash).
   - Ném ngoại lệ (`throw Exception`) làm runtime .NET phải sinh Stack Trace, gây lãng phí bộ nhớ và CPU, đồng thời làm phân mảnh luồng code. Cần cơ chế đóng gói trạng thái thực thi an toàn, tường minh.
2. **Chuẩn hóa danh mục lỗi tập trung (Error Catalog)**:
   - Tránh việc trả về chuỗi thông báo lỗi tự do (magic string) rải rác trong code. Cần phân loại lỗi tường minh (`Validation`, `NotFound`, `Conflict`) kèm mã lỗi cố định (`Code`) để hỗ trợ Frontend/Mobile hiển thị đa ngôn ngữ (i18n).
3. **Đóng gói quy tắc nghiệp vụ độc lập (Business Rule Decoupling)**:
   - Các điều kiện thẩm định nghiệp vụ (hạn lệnh, khả năng chịu tải, tương thích kích cỡ) nếu viết bằng hàng loạt câu lệnh `if-else` trong Entity hoặc Controller sẽ vi phạm SRP, khó mở rộng và rất khó viết Unit Test độc lập.
4. **Cô lập dữ liệu đa chi nhánh / đa Depot (Multi-Tenancy Isolation)**:
   - Tổng công ty quản lý nhiều Depot (Cát Lái, Hiệp Phước, Phú Hữu...). Cần cơ chế đánh dấu phân vùng dữ liệu ngay từ tầng Domain mà không vi phạm nguyên tắc Clean Architecture.
5. **Chuẩn hóa định danh và dấu vết kiểm toán (Identity & Auditing)**:
   - Đảm bảo mọi bản ghi đều có mã định danh duy nhất toàn cục (`Guid Id`) sinh độc lập với DB, kèm dấu vết người tạo, ngày tạo, ngày chỉnh sửa để phục vụ đối soát và truy vết khi xảy ra tranh chấp giao nhận.

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Clean Architecture (Onion Architecture)**:
  - Chia hệ thống thành 4 tầng riêng biệt: `Domain` $\rightarrow$ `Application` $\rightarrow$ `Infrastructure` $\rightarrow$ `Api`.
  - Tuân thủ quy tắc phụ thuộc một chiều (**Dependency Rule**): Tầng `Domain` nằm ở trung tâm và tuyệt đối **không phụ thuộc** vào bất kỳ tầng nào hoặc thư viện bên ngoài ($0\text{ external dependencies}$).
- **Central Package Management (CPM)**:
  - Toàn bộ phiên bản thư viện Nuget được quản lý tập trung tại `Directory.Packages.props`, ngăn chặn hoàn toàn rủi ro lệch phiên bản (Version Drift) giữa các project con.
- **Result Pattern & Railway-Oriented Programming**:
  - Dùng lớp `Result` và `Result<T>` để biểu diễn kết quả thực thi gồm 2 đường ray: Thành công (`IsSuccess = true`) hoặc Thất bại (`IsSuccess = false` kèm `Error`).
- **Specification / Business Rule Pattern**:
  - Tách quy tắc nghiệp vụ thành các lớp độc lập triển khai interface `IBusinessRule`, điều phối tập trung qua `BusinessRuleValidator`.
- **Interface Segregation cho Multi-Tenancy**:
  - Cung cấp interface `ITenantEntity` chứa `TenantId`. Chỉ những thực thể bãi cần cách ly dữ liệu mới triển khai interface này.

### 2.2. Kiến Thức Áp Dụng
- **Clean Architecture & Domain-Driven Design (DDD)**: Đặt Domain làm trung tâm bất biến, độc lập với cơ sở dữ liệu và framework UI.
- **OOP (4 Trụ cột)**:
  - *Encapsulation*: Thuộc tính định danh và kiểm toán được bảo vệ bằng `protected set`, chỉ lớp kế thừa mới được gán.
  - *Abstraction*: Sử dụng `abstract class BaseEntity`, `abstract class AuditableEntity`, `interface IBusinessRule`.
  - *Inheritance*: Cây kế thừa phân tầng chuẩn: `BaseEntity` $\rightarrow$ `AuditableEntity` $\rightarrow$ Domain Entities.
  - *Polymorphism*: Bộ điều phối `BusinessRuleValidator` tiếp nhận đa hình `IEnumerable<IBusinessRule>`.
- **SOLID Principles**:
  - `SRP`: Mỗi lớp (`Error`, `Result`, `BaseEntity`) chỉ làm đúng một nhiệm vụ duy nhất.
  - `OCP`: Bổ sung quy tắc nghiệp vụ mới mà không sửa `BusinessRuleValidator`.
  - `ISP`: Không ép buộc `TenantId` vào `BaseEntity`, tách riêng thành `ITenantEntity`.
  - `DIP`: Domain định nghĩa abstraction (`IBusinessRule`), các tầng ngoài phụ thuộc vào Domain.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

Cấu trúc các thư mục nền tảng đã thiết lập trong ngày:

```text
Domain/
├── Common/
│   ├── BaseEntity.cs                 # Lớp cơ sở trừu tượng chứa Id duy nhất (Guid)
│   ├── AuditableEntity.cs            # Lớp cơ sở chứa dấu vết kiểm toán (CreatedAt, CreatedBy...)
│   ├── ITenantEntity.cs              # Giao diện đánh dấu phân vùng chi nhánh depot
│   ├── Error.cs                      # Record định nghĩa lỗi chuẩn (Code, Description)
│   ├── Result.cs                     # Bao đóng kết quả thực thi (Result, Result<T>)
│   ├── PagedResult.cs                # Kết quả phân trang dữ liệu
│   └── Rules/
│       ├── IBusinessRule.cs          # Hợp đồng chung cho mọi quy tắc nghiệp vụ
│       ├── BusinessRuleException.cs  # Ngoại lệ đóng gói khi quy tắc bị vi phạm
│       └── BusinessRuleValidator.cs  # Bộ điều phối thẩm định quy tắc tập trung
tests/
└── Domain.UnitTests/
    └── Common/                       # Test suite cho Result, Error, BusinessRuleValidator
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng thống kê các phương thức cốt lõi được xây dựng trong tầng `Domain/Common`:

| Tên Hàm (Signature) | Tham Số Đầu Vào | Đầu Ra (Return) | Chức Năng & Vai Trò Trong Luồng |
|---|---|---|---|
| `Result.Success()` | Không | `Result` | Tạo đối tượng kết quả biểu thị luồng nghiệp vụ thành công, gán `IsSuccess = true` và `Error = Error.None`. |
| `Result.Failure(Error error)` | `Error error` | `Result` | Tạo đối tượng kết quả biểu thị luồng nghiệp vụ thất bại, gán `IsSuccess = false` và lưu thông tin lỗi. |
| `Result.Success<T>(T value)` | `T value` | `Result<T>` | Tạo kết quả thành công chứa dữ liệu trả về kiểu `T` (Generic). |
| `Result.Failure<T>(Error error)` | `Error error` | `Result<T>` | Tạo kết quả thất bại cho thao tác trả dữ liệu kiểu `T`, `Value` trả về `default`. |
| `Error.Validation(string code, string desc)` | Mã lỗi, mô tả | `Error` | Factory method sinh lỗi phân loại dữ liệu đầu vào không hợp lệ. |
| `Error.NotFound(string code, string desc)` | Mã lỗi, mô tả | `Error` | Factory method sinh lỗi phân loại tài nguyên không tìm thấy. |
| `Error.Conflict(string code, string desc)` | Mã lỗi, mô tả | `Error` | Factory method sinh lỗi xung đột trạng thái dữ liệu nghiệp vụ. |
| `BusinessRuleValidator.Check(IBusinessRule rule)` | Đối tượng quy tắc | `void` | Kiểm tra quy tắc đơn lẻ: nếu `rule.IsBroken()` trả về `true`, ném ngay `BusinessRuleException`. |
| `BusinessRuleValidator.Validate(params IBusinessRule[] rules)` | Mảng các quy tắc | `void` | Thẩm định danh sách quy tắc theo thứ tự: dừng lại và ném ngoại lệ ngay tại quy tắc vi phạm đầu tiên. |
| `BusinessRuleValidator.Validate(IEnumerable<IBusinessRule> rules)` | Tập hợp đa hình quy tắc | `void` | Quá tải (overload) hỗ trợ nhận danh sách động `List<IBusinessRule>` hoặc LINQ query. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Từ Khóa `public`, `private`, `protected`, `internal` (Access Modifiers)
- `public`: Thành phần có thể được truy cập từ bất kỳ đâu (trong cùng assembly hoặc từ các assembly bên ngoài). Dùng cho các phương thức API và property công khai như `Result.Success()`, `Id`.
- `private`: Giới hạn truy cập chỉ bên trong phạm vi của chính class định nghĩa. Giúp bảo vệ dữ liệu không bị sửa đổi từ bên ngoài.
- `protected`: Cho phép class hiện tại và các class kế thừa truy cập. Được dùng trong constructor `protected Result(...)` và property `public Guid Id { get; protected set; }` để lớp con có thể khởi tạo nhưng bên ngoài không được gán đè.
- `internal`: Chỉ cho phép truy cập bên trong cùng một project/assembly (`SnpDepotManagement.Domain`). Dùng trong `protected internal Result(T? value...)` để kiểm soát chặt chẽ việc khởi tạo.

### 5.2. Từ Khóa `abstract` (Lớp Cơ Sở Trừu Tượng)
- Áp dụng tại `public abstract class BaseEntity` và `public abstract class AuditableEntity`.
- **Ý nghĩa**: Ngăn chặn việc khởi tạo trực tiếp instance (`new BaseEntity()` là không hợp lệ). Lớp `abstract` đóng vai trò là bản thiết kế chung để các thực thể nghiệp vụ cụ thể (`Container`, `YardBlock`, `DeliveryOrder`) kế thừa lại.

### 5.3. Từ Khóa `sealed` (Khóa Kế Thừa)
- **Ý nghĩa**: Ngăn không cho bất kỳ class nào khác kế thừa từ class này.
- **Tác dụng**:
  1. *Bảo toàn thiết kế*: Đảm bảo hành vi của lớp không bị override hoặc làm sai lệch bởi lớp con.
  2. *Tối ưu hóa hiệu năng (Devirtualization)*: Trình biên dịch JIT của .NET nhận biết lớp là `sealed` sẽ bỏ qua bảng ảo (vtable) và chuyển cuộc gọi phương thức thành lời gọi trực tiếp (direct call / inline), tăng tốc độ thực thi đáng kể.

### 5.4. Cơ Chế Property: `get; set;` vs `get; protected set;` vs `get;`
- `public string TenantId { get; set; }`: Cả đọc (`get`) và ghi (`set`) đều công khai.
- `public Guid Id { get; protected set; }`: Đọc công khai nhưng ghi chỉ dành riêng cho lớp kế thừa (Encapsulation - đóng gói bảo vệ khóa chính).
- `public bool IsSuccess { get; }`: Thuộc tính chỉ đọc (Read-only property), giá trị chỉ được gán duy nhất 1 lần trong constructor, đảm bảo tính bất biến (Immutability).
- `public bool IsFailure => !IsSuccess;`: Expression-bodied property (cú pháp rút gọn biểu thức), tự động tính toán giá trị mỗi khi được truy cập mà không tốn ô nhớ lưu trữ.

### 5.5. Cấu Trúc `record` Trong C#
- Khai báo: `public record Error(string Code, string Description)`
- **Đặc tính**:
  - Tự động sinh cơ chế so sánh theo giá trị (**Value Equality**): Hai đối tượng `Error` có cùng `Code` và `Description` sẽ được coi là bằng nhau (`==`), rất phù hợp để làm Value Object hoặc đối tượng thông điệp lỗi.
  - Tự động sinh hàm `ToString()`, hàm băm `GetHashCode()`, và hỗ trợ cú pháp `with` để sao chép biến thể.

---

## 6. Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)

### 6.1. Mục Tiêu & Cấu Trúc Kiểm Thử Theo Mô Hình AAA
Mục tiêu kiểm thử của Ngày 1 là bảo đảm các khối gạch nền tảng (`Result`, `Result<T>`, `Error`, `BusinessRuleValidator`) hoạt động chính xác 100%, không bị lỗi logic rẽ nhánh.

Bài test tuân thủ nghiêm ngặt mô hình **AAA**:
1. **Arrange (Chuẩn bị)**: Thiết lập dữ liệu đầu vào và các đối tượng giả lập.
2. **Act (Thực thi)**: Gọi trực tiếp phương thức nghiệp vụ cần test.
3. **Assert (Khẳng định)**: Dùng `FluentAssertions` để kiểm tra kết quả kỳ vọng.

### 6.2. Mã Nguồn Bài Test Minh Họa & Các Kịch Bản

#### Kịch bản 1: Kiểm thử luồng Thành công của `Result` (Happy Path)
```csharp
[Fact]
public void Result_Success_ShouldHaveIsSuccessTrue_AndNoError()
{
    // Arrange & Act
    var result = Result.Success();

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.IsFailure.Should().BeFalse();
    result.Error.Should().Be(Error.None);
}
```
* **Giải thích**: Khi gọi `Result.Success()`, cờ `IsSuccess` phải là `true`, `IsFailure` phải là `false`, và đối tượng lỗi phải là `Error.None` (rỗng).

#### Kịch bản 2: Kiểm thử luồng Thất bại chứa mã lỗi (Error Path)
```csharp
[Fact]
public void Result_Failure_ShouldContainSpecifiedError()
{
    // Arrange
    var expectedError = Error.Validation("Input.Invalid", "Dữ liệu không đúng quy chuẩn.");

    // Act
    var result = Result.Failure(expectedError);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("Input.Invalid");
    result.Error.Description.Should().Be("Dữ liệu không đúng quy chuẩn.");
}
```
* **Giải thích**: Kiểm tra khi luồng thất bại, `Result` phải lưu giữ chính xác `Code` và `Description` của lỗi được truyền vào.

#### Kịch bản 3: Kiểm thử bộ duyệt quy tắc `BusinessRuleValidator` ném ngoại lệ khi có vi phạm
```csharp
[Fact]
public void BusinessRuleValidator_WhenRuleIsBroken_ShouldThrowBusinessRuleException()
{
    // Arrange: Tạo một rule vi phạm (IsBroken = true)
    var brokenRule = new MockBrokenRule();

    // Act: Thực thi trong một Action delegate
    Action act = () => BusinessRuleValidator.Check(brokenRule);

    // Assert: Kỳ vọng ném BusinessRuleException chứa đúng rule đó
    act.Should().Throw<BusinessRuleException>()
       .Which.Rule.Should().Be(brokenRule);
}
```
* **Giải thích**: Khi một quy tắc nghiệp vụ bị vi phạm (`IsBroken() == true`), hàm `Check` hoặc `Validate` phải lập tức ném ra ngoại lệ chuyên biệt `BusinessRuleException` để dừng luồng xử lý và báo lỗi lên tầng trên.
