# Báo Cáo Kỹ Thuật: Ngày 2 - Container (Số Chuẩn ISO), Dữ Liệu Danh Mục & Tài Khoản Người Dùng

* **Dự án**: Hệ Thống Quản Lý Bãi Container Rỗng (SNP Depot Management)
* **Giai đoạn**: Tuần 1 - Tầng Nghiệp Vụ Cốt Lõi (Domain Layer)
* **Trọng tâm Ngày 2**: Value Object `ContainerNumber` (ISO 6346 Modulo 11), Thực thể `Container`, Master Data (Hãng tàu, Khách hàng, Chuyến tàu, Phương tiện) & `ApplicationUser`

---

## 1. Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý

1. **Chuẩn hóa và thẩm định số Container theo chuẩn quốc tế ISO 6346**:
   - Mã số container là định danh duy nhất toàn cầu gồm 11 ký tự: 3 chữ cái mã chủ sở hữu (Owner Code), 1 chữ cái phân loại thiết bị (Category Identifier: `U` cho container, `J` cho thiết bị gắn kèm, `Z` cho rơ-moóc), 6 chữ số seri (Serial Number), và 1 chữ số kiểm tra (Check Digit).
   - Xử lý các lỗi thực tế từ tác nghiệp cổng bãi: tài xế hoặc điều độ viên gõ nhầm khoảng trắng xen giữa, gõ chữ thường, gõ thiếu ký tự, hoặc số kiểm tra không khớp với thuật toán Modulo 11. Cần hệ thống 7 mã lỗi chuyên biệt để phản hồi chính xác vị trí sai sót.
2. **Bất biến về trọng lượng và phân cấp chất lượng vỏ rỗng**:
   - Trọng lượng vỏ ($\text{TareWeight}$) phải $> 0$.
   - Trọng lượng toàn tải tối đa ($\text{MaxGrossWeight}$) phải $> \text{TareWeight}$.
   - Khả năng tải hàng hóa: $\text{Payload} = \text{MaxGrossWeight} - \text{TareWeight}$.
   - Vỏ container rỗng cần phân cấp chất lượng: `Grade A` (đạt chuẩn đóng hàng thực phẩm, điện tử), `Grade B` (hàng bách hóa tiêu chuẩn), `Grade C` (vỏ cũ/kém chất lượng).
   - Khi phát hiện rách thủng, móp méo (`MarkDamaged`), hệ thống phải tự động chuyển trạng thái `Condition = Damaged` và hạ cấp ngay về `Grade C` nhằm ngăn chặn việc cấp nhầm vỏ hỏng cho chủ hàng.
3. **Thẩm định dữ liệu danh mục khách hàng (Customer Tax Code)**:
   - Mã số thuế doanh nghiệp tại Việt Nam bắt buộc tuân theo quy chuẩn: 10 chữ số (cho doanh nghiệp độc lập/trụ sở chính) hoặc 13 chữ số (cho chi nhánh/văn phòng đại diện), không được chứa chữ cái hay ký tự đặc biệt.
4. **Mô hình hóa tài khoản người dùng độc lập trong Domain**:
   - Tránh việc phụ thuộc Domain vào thư viện `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Cần thiết kế thực thể POCO `ApplicationUser` độc lập, lưu giữ thông tin cốt lõi (UserName, Email, RefreshTokenHash) và hỗ trợ cơ chế cấp phát / thu hồi token.

---

## 2. Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng

### 2.1. Giải Pháp Thiết Kế
- **Value Object Pattern (`ContainerNumber`)**:
  - Không có định danh `Id`, tính bất biến tuyệt đối (Immutable), tự chịu trách nhiệm xác thực tính hợp lệ. Hai số container giống nhau về chuỗi giá trị là một.
  - Tự động làm sạch dữ liệu: loại bỏ toàn bộ khoảng trắng thừa và chuyển sang chữ hoa (`ToUpperInvariant()`) trước khi xác thực.
- **Thuật Toán ISO 6346 Modulo 11**:
  - Bảng mã hóa chữ cái: Gán giá trị $10 \dots 38$ cho $A \dots Z$, loại trừ các số là bội số của 11 ($11, 22, 33$).
  - Trọng số lũy thừa của 2: Từng ký tự tại vị trí $i \in [0, 9]$ được nhân với $2^i$, tính toán siêu tốc thông qua toán tử dịch bit `1 << i`.
  - Công thức số kiểm tra: $d = \left(\sum_{i=0}^9 \text{Value}_i \times 2^i \pmod{11}\right) \pmod{10}$.
- **Static Factory Method Pattern**:
  - Sử dụng phương thức tĩnh `Create(...)` trả về `Result<T>` thay vì constructor công khai. Ngăn ngừa việc tạo ra các thực thể ở trạng thái không hợp lệ.
- **Tách biệt kiến trúc (Clean Architecture Domain Independence)**:
  - `ApplicationUser` được thiết kế thuần túy C# (POCO), chỉ chứa các trường phục vụ nghiệp vụ xác thực của kho bãi mà không mang theo bất kỳ thư viện Identity nào của ASP.NET Core.

### 2.2. Kiến Thức Áp Dụng
- **Domain-Driven Design (DDD)**:
  - Phân biệt rõ rệt **Value Object** (`ContainerNumber`) và **Entity** (`Container`, `Customer`, `LineOperator`, `Vehicle`, `Voyage`).
- **OOP (Đóng Gói & Bất Biến)**:
  - Private constructor ép buộc mọi tương tác phải đi qua Factory Method.
  - Trạng thái bên trong của `Container` được bảo vệ bằng `private set`.
- **SOLID (Single Responsibility Principle)**:
  - `ContainerNumber` chỉ làm đúng nhiệm vụ kiểm tra và lưu số container.
  - `Customer` chịu trách nhiệm kiểm tra định dạng MST của chính nó.

---

## 3. Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)

```text
Domain/
├── Enums/
│   └── ContainerEnums.cs            # Enum ContainerSize, ContainerType, Classification, ContainerCondition
├── ValueObjects/
│   └── ContainerNumber.cs           # Value Object số container chuẩn ISO 6346 Modulo 11
└── Entities/
    ├── Containers/
    │   └── Container.cs             # Entity container vỏ rỗng (trọng lượng, tình trạng, phân cấp)
    ├── MasterData/
    │   ├── LineOperator.cs          # Danh mục hãng tàu (Code, Name)
    │   ├── Customer.cs              # Danh mục khách hàng & xác thực MST Việt Nam
    │   ├── Voyage.cs                # Danh mục chuyến tàu (VesselName, VoyageNo, ETD)
    │   └── Vehicle.cs               # Danh mục phương tiện vận tải (PlateNumber, VehicleType)
    └── Identity/
        └── ApplicationUser.cs       # Thực thể người dùng trung lập (POCO) trong Domain
tests/
└── Domain.UnitTests/
    ├── Containers/                  # Unit test cho ContainerNumber và Container
    ├── MasterData/                  # Unit test cho Customer, Vehicle, Voyage
    └── Identity/                    # Unit test cho ApplicationUser
```

---

## 4. Flow Code: Danh Sách Hàm & Chức Năng

Bảng thống kê các phương thức cốt lõi được xây dựng trong ngày:

| Tên Hàm (Signature) | Tham Số Đầu Vào | Đầu Ra (Return) | Chức Năng & Vai Trò Trong Luồng |
|---|---|---|---|
| `ContainerNumber.Create(string? rawInput)` | Chuỗi số cont thô | `Result<ContainerNumber>` | Chuẩn hóa chuỗi (xóa khoảng trắng, in hoa), chạy 7 bước kiểm tra ISO 6346 và thẩm định số kiểm tra Modulo 11. |
| `CalculateCheckDigit(string first10)` | 10 ký tự đầu của cont | `int` | Hàm nội bộ tính tổng có trọng số với toán tử `1 << i`, chia lấy dư cho 11 và 10 để ra số kiểm tra lý thuyết. |
| `LetterToValue(char c)` | Ký tự chữ cái | `int` | Bảng tra cứu chuyển đổi ký tự $A \dots Z$ thành giá trị số, bỏ qua các bội số của 11 ($11, 22, 33$). |
| `Container.Create(...)` | Số cont, mã ISO, size, type, trọng lượng, ngày SX | `Result<Container>` | Factory method khởi tạo container vỏ rỗng, bắt buộc $\text{TareWeight} > 0$ và $\text{MaxGross} > \text{TareWeight}$. |
| `Container.MarkDamaged(string reason)` | Lý do hư hỏng | `void` | Chuyển trạng thái sang `Damaged` và tự động hạ cấp chất lượng về `Classification.C`. |
| `Container.UpdateClassification(Classification c)` | Hạng chất lượng mới | `void` | Cập nhật phân cấp vỏ rỗng sau khi giám định hoặc sửa chữa vệ sinh xong. |
| `Customer.Create(...)` | Mã KH, tên, mã số thuế | `Result<Customer>` | Khởi tạo đối tượng khách hàng, thẩm định độ dài và định dạng hợp lệ của MST Việt Nam. |
| `Customer.ValidateTaxCode(string taxCode)` | Chuỗi mã số thuế | `bool` | Kiểm tra chuỗi có đúng 10 hoặc 13 ký tự số nguyên hay không. |
| `Vehicle.Create(...)` | Biển số xe, loại xe | `Result<Vehicle>` | Chuẩn hóa biển số xe (in hoa, xóa khoảng trắng thừa) và tạo phương tiện vận tải. |
| `ApplicationUser.Create(...)` | Username, email | `Result<ApplicationUser>` | Khởi tạo tài khoản người dùng nghiệp vụ với email hợp lệ. |
| `ApplicationUser.SetRefreshTokenHash(...)` | Chuỗi hash, hạn dùng | `void` | Lưu trữ refresh token đã băm an toàn phục vụ duy trì phiên đăng nhập. |
| `ApplicationUser.RevokeRefreshToken()` | Không | `void` | Xóa bỏ refresh token hiện tại, lập tức thu hồi phiên làm việc của người dùng. |

---

## 5. Giải Thích Cú Pháp C# (Syntax Deep-Dive)

### 5.1. Cú Pháp `public sealed record ContainerNumber`
- `sealed`: Khóa class, không cho kế thừa.
- `record`: Khai báo kiểu tham chiếu bất biến với tính năng **Value-based equality** (hai bản ghi bằng nhau nếu toàn bộ giá trị các trường giống nhau). Điều này hoàn toàn khớp với định nghĩa của **Value Object** trong Domain-Driven Design: hai số container `"CSQU3054383"` dù nằm ở hai vùng nhớ khác nhau vẫn được toán tử `==` so sánh bằng nhau.

### 5.2. Private Constructor & Static Factory Method
```csharp
private ContainerNumber(string value) => Value = value;
public static Result<ContainerNumber> Create(string? rawInput) { ... }
```
- **Tại sao constructor là `private`?**: Ngăn chặn lập trình viên bên ngoài khởi tạo tùy tiện bằng lệnh `new ContainerNumber(...)`. Bắt buộc mọi luồng tạo đối tượng phải đi qua `Create(...)`, đảm bảo đối tượng luôn luôn hợp lệ ngay từ lúc sinh ra.

### 5.3. Toán Tử Dịch Bit (Bitwise Shift) `1 << i`
```csharp
int weight = 1 << i; // Tương đương Math.Pow(2, i) nhưng nhanh hơn rất nhiều
```
- `1 << 0` = $1 = 2^0$
- `1 << 1` = $2 = 2^1$
- `1 << 2` = $4 = 2^2$
- **Ưu điểm**: Dịch bit trên thanh ghi CPU chỉ tốn $1$ chu kỳ máy (clock cycle), nhanh hơn hàng chục lần so với việc gọi hàm số thực `Math.Pow(2, i)`.

### 5.4. Cơ Chế `private set` Và Null-Forgiving Operator `null!`
```csharp
public ContainerNumber ContainerNumber { get; private set; } = null!;
```
- `private set`: Thuộc tính chỉ có thể được gán giá trị bên trong các phương thức của chính class `Container`. Các tầng ngoài (Application, API) chỉ có quyền đọc (`get`), ngăn chặn việc sửa đổi dữ liệu tùy tiện (Bảo toàn Encapsulation).
- `= null!`: Toán tử bỏ qua cảnh báo null của trình biên dịch C# nullable context. Báo hiệu cho compiler biết thuộc tính này sẽ được gán giá trị hợp lệ trong Factory Method trước khi đối tượng được sử dụng thực tế.

### 5.5. Biểu Thức Lambda Thuộc Tính (Expression-Bodied Members)
```csharp
public string OwnerCode => Value.Substring(0, 3);
public char CategoryIdentifier => Value[3];
public string SerialNumber => Value.Substring(4, 6);
public int CheckDigit => Value[10] - '0';
```
- Dấu `=>` giúp định nghĩa các thuộc tính tính toán (Computed Properties) ngắn gọn, giá trị được tính động trực tiếp từ chuỗi `Value` mà không tốn thêm bộ nhớ lưu trữ riêng cho từng biến.

---

## 6. Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)

### 6.1. Mục Tiêu & Cấu Trúc Kiểm Thử Ngày 2
Bộ Unit Tests của Ngày 2 gồm **55 tests** tập trung kiểm thử:
- Thuật toán Modulo 11 của số container chuẩn ISO 6346 (phủ trọn 7 mã lỗi).
- Bất biến trọng lượng và logic tự động hạ cấp chất lượng khi hư hại của `Container`.
- Xác thực định dạng Mã Số Thuế chuẩn Việt Nam trong `Customer`.

### 6.2. Mã Nguồn Bài Test Minh Họa & Các Kịch Bản

#### Kịch bản 1: Kiểm thử số Container chuẩn quốc tế hợp lệ (Happy Path)
```csharp
[Fact]
public void Create_WithValidNumber_ShouldSucceed()
{
    // Arrange: Mã cont chuẩn quốc tế CSQU3054383
    var validNumber = "CSQU3054383";

    // Act
    var result = ContainerNumber.Create(validNumber);

    // Assert: Xác minh bóc tách chính xác từng thành phần
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value!.Value.Should().Be(validNumber);
    result.Value.OwnerCode.Should().Be("CSQ");
    result.Value.CategoryIdentifier.Should().Be('U');
    result.Value.SerialNumber.Should().Be("305438");
    result.Value.CheckDigit.Should().Be(3);
}
```
* **Giải thích**: Khi truyền số cont chuẩn, Value Object phải khởi tạo thành công và bóc tách chính xác OwnerCode (`CSQ`), Category (`U`), Serial (`305438`), CheckDigit (`3`).

#### Kịch bản 2: Kiểm thử tự động chuẩn hóa chữ thường và khoảng trắng (Edge Case)
```csharp
[Theory]
[InlineData("csqu3054383")]
[InlineData(" CSQU 305438 3 ")]
[InlineData("csqu 305438 3")]
public void Create_WithLowerCaseOrWhitespace_ShouldNormalizeSuccessfully(string input)
{
    // Act
    var result = ContainerNumber.Create(input);

    // Assert: Tự động loại bỏ khoảng trắng và in hoa thành "CSQU3054383"
    result.IsSuccess.Should().BeTrue();
    result.Value!.Value.Should().Be("CSQU3054383");
}
```
* **Giải thích**: Sử dụng `[Theory]` với `[InlineData]` để chạy 3 trường hợp tài xế gõ sai format. Hệ thống tự động xử lý thành công mà không báo lỗi giả.

#### Kịch bản 3: Kiểm thử số kiểm tra sai lệch (Check Digit Mismatch Error)
```csharp
[Fact]
public void Create_WhenCheckDigitMismatch_ShouldReturnCheckDigitMismatchError()
{
    // Arrange: Số cont bị gõ sai check digit (số đúng là 3 nhưng gõ thành 9)
    var input = "CSQU3054389";

    // Act
    var result = ContainerNumber.Create(input);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("ContainerNumber.CheckDigitMismatch");
}
```
* **Giải thích**: Thuật toán Modulo 11 tính ra kết quả lý thuyết là $3$, nhưng số thực tế là $9$, hệ thống lập tức từ chối và trả về đúng mã lỗi `ContainerNumber.CheckDigitMismatch`.

#### Kịch bản 4: Kiểm thử tự động hạ cấp chất lượng khi vỏ bị hỏng (`MarkDamaged`)
```csharp
[Fact]
public void MarkDamaged_ShouldSetConditionToDamaged_AndDowngradeToGradeC()
{
    // Arrange: Khởi tạo container vỏ tốt loại A
    var containerNumber = ContainerNumber.Create("CSQU3054383").Value!;
    var container = Container.Create(containerNumber, "22G1", ContainerSize.Ft20, ContainerType.Dry, 2200, 30480, null, "MSC").Value!;
    container.Classification.Should().Be(Classification.A);

    // Act: Báo hỏng thủng sàn
    container.MarkDamaged("Thủng sàn container");

    // Assert: Trạng thái chuyển Damaged và cấp chất lượng tự tụt về C
    container.Condition.Should().Be(ContainerCondition.Damaged);
    container.Classification.Should().Be(Classification.C);
}
```
* **Giải thích**: Đảm bảo nghiệp vụ an toàn cốt lõi: vỏ bị hỏng không bao giờ còn giữ hạng A, tránh cấp nhầm cho khách đóng hàng cao cấp.
