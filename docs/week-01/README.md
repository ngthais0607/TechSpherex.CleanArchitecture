# Nhật Ký Tuần 1: Lớp Domain (SnpDepotManagement)

Thư mục này lưu trữ chi tiết toàn bộ quá trình thiết kế, triển khai, rà soát và kiểm thử của **Tuần 1 (Lớp Domain)** cho hệ thống quản lý Depot Container vỏ rỗng (**SnpDepotManagement**), bám sát 100% theo lộ trình kỹ thuật 7 ngày.

---

## 1. Mục Lục Báo Cáo Theo Lộ Trình 7 Ngày

| Ngày / Chặng | Nội Dung Trọng Tâm Theo Lộ Trình | Tài Liệu Chi Tiết |
|---|---|---|
| **Ngày 1** | **Dựng khung dự án và cấu trúc thư mục**: Solution Clean Architecture, Central Package Management (CPM), BaseEntity, AuditableEntity, ITenantEntity, Error, Result, BusinessRuleValidator. | 📄 [day-01.md](file:///d:/SnpDepotManagement/docs/week-01/day-01.md) |
| **Ngày 2** | **Làm Container (số chuẩn ISO), dữ liệu danh mục (hãng tàu, khách hàng, chuyến tàu, phương tiện) và tài khoản người dùng**: Thuật toán ISO 6346 Modulo 11 (bit-shift `1 << i`, 7 mã lỗi), Container (Tare/MaxGross, hạ cấp hư hại), Master Data (MST 10/13 số), ApplicationUser. | 📄 [day-02.md](file:///d:/SnpDepotManagement/docs/week-01/day-02.md) |
| **Ngày 3** | **Làm phần bãi (block, ô chứa, mở rộng/thu hẹp)**: Tọa độ 3 chiều `YardPosition`, Aggregate Root `YardBlock`, Entity `YardSlot`, Quy ước Bay lẻ (Odd-Bay Convention), Thuật toán `Expand` và `Shrink` (Conflict Detection nguyên tử), Block Ảo. | 📄 [day-03.md](file:///d:/SnpDepotManagement/docs/week-01/day-03.md) |
| **Ngày 4** | **Viết các quy tắc xếp container trong bãi**: Bộ 4 quy tắc an toàn bãi (TDD): Tương thích kích cỡ & chặn mép block (`ValidBayContainerSizeRule`), Tĩnh không (`TierWithinBlockLimitRule`), Chống lơ lửng & 2 chân đỡ (`SlotBelowMustBeOccupiedRule`), Ô trống (`SlotIsFreeRule`). | 📄 [day-04.md](file:///d:/SnpDepotManagement/docs/week-01/day-04.md) |
| **Ngày 5** | **Làm lệnh giao và vòng đời container (vào/ra bãi)**: Lệnh giao vỏ `DeliveryOrder` & `DeliveryOrderItem` (quota trừ dần nguyên tử), Vòng đời FSM `ContainerLifecycle` (Gate-In, Gate-Out chặn xuất 2 lần), Bộ 4 quy tắc xuất vỏ (Dispatch Rules) & Architecture Test. | 📄 [day-05.md](file:///d:/SnpDepotManagement/docs/week-01/day-05.md) |
| **Ngày 6** | **Check lại Code và Double check logic và tiếp tục tìm hiểu về bước thiết kế tiếp theo**: Rà soát tính toàn vẹn (Invariants), bảo vệ tính đóng gói, rà soát trường hợp biên, nghiên cứu bước thiết kế tiếp theo cho Tuần 2 (CQRS, MediatR, Repository, Domain Events). | 📄 [day-06.md](file:///d:/SnpDepotManagement/docs/week-01/day-06.md) |
| **Ngày 7** | **Làm code test và chuẩn bị sang tuần 2 Application (xử lý nghiệp vụ)**: Hoàn thiện suite kiểm thử tự động (121 tests Passed), chuẩn hóa quy trình TDD/AAA, kiểm thử kiến trúc `NetArchTest.Rules` và sẵn sàng cấu hình cho tầng Application. | 📄 [day-07.md](file:///d:/SnpDepotManagement/docs/week-01/day-07.md) |
| **Cơ Sở Dữ Liệu** | **Thiết kế Database tổng quát & Giải thích**: Sơ đồ ERD (11 bảng), Khóa chính (UUID), Khóa ngoại (FK), Ràng buộc xóa (Cascade/Restrict/Set Null), Kịch bản truy vấn và Script SQL DDL (PostgreSQL). | 🗄️ [database-design.md](file:///d:/SnpDepotManagement/docs/week-01/database-design.md) |
| **Giải Thích Test** | **Phân tích toàn diện 121 Unit Tests**: Giải thích chi tiết mã nguồn test từng ngày theo chuẩn AAA, ý nghĩa nghiệp vụ của từng assert, và bảng phủ mã lỗi Domain. | 🧪 [tests-explanation.md](file:///d:/SnpDepotManagement/docs/week-01/tests-explanation.md) |
| **Tổng Hợp** | **Ma trận logic nghiệp vụ & Giải pháp kiến trúc chuyên sâu**: Bảng đối chiếu toàn diện các bài toán nghiệp vụ bãi rỗng và giải pháp xử lý. | 🌟 [week-01-logic-and-solutions.md](file:///d:/SnpDepotManagement/docs/week-01/week-01-logic-and-solutions.md) |

---

## 2. Chuẩn Cấu Trúc Tài Liệu Cho Mỗi Ngày (6 Phần Đồng Nhất)

Mỗi tài liệu từ [day-01.md](file:///d:/SnpDepotManagement/docs/week-01/day-01.md) đến [day-07.md](file:///d:/SnpDepotManagement/docs/week-01/day-07.md) được xây dựng theo chuẩn 6 phần đồng nhất:

1. **Logic Nghiệp Vụ & Kỹ Thuật Phải Xử Lý**: Nêu rõ bài toán, tình huống thực tế, rủi ro và các trường hợp biên (edge cases).
2. **Giải Pháp Kỹ Thuật & Kiến Thức Áp Dụng**: Trình bày kiến trúc Clean Architecture, DDD, OOP, SOLID, Design Patterns và thuật toán áp dụng.
3. **Cấu Trúc Thư Mục Đã Tạo (Rút Gọn)**: Tóm tắt ngắn gọn các thư mục thành phần dạng `Domain/Common/...`, `Domain/Entities/...`, `tests/...`.
4. **Flow Code: Danh Sách Hàm & Chức Năng**: Bảng liệt kê chi tiết tên hàm (`MethodName(...)`), tham số đầu vào, đầu ra và nhiệm vụ trong luồng xử lý.
5. **Giải Thích Cú Pháp C# (Syntax Deep-Dive)**: Giải thích cặn kẽ bản chất và lý do sử dụng các cú pháp C# (`public`, `private`, `internal`, `protected`, `sealed`, `abstract`, `get; set;`, `get; private set;`, `record`, `static`, factory methods...).
6. **Thiết Kế Bài Test Thực Tế (Unit Test Implementation & Scenarios)**: Phân tích cấu trúc AAA (Arrange - Act - Assert), các kịch bản kiểm thử Happy/Edge/Error, code test C# thực tế từ project và giải thích luồng chạy kiểm thử.

---

## 3. Bảng Tổng Hợp Áp Dụng OOP, SOLID & Design Patterns

### 3.1. Bốn Trụ Cột Lập Trình Hướng Đối Tượng (OOP)

| Trụ Cột OOP | Vị Trí Hiện Thực Tiêu Biểu Trong Dự Án |
|---|---|
| **Encapsulation (Đóng gói)** | - Mọi Entity (`Container`, `YardBlock`, `DeliveryOrder`, `ContainerLifecycle`) đều dùng `private set` cho properties và private/internal constructors. Trạng thái chỉ được thay đổi qua các phương thức nghiệp vụ tường minh (`MarkDamaged`, `Expand`, `Shrink`, `Allocate`, `GateOut`).<br>- `YardBlock` bảo vệ danh sách nội bộ `_slots` thông qua `IReadOnlyCollection<YardSlot>`. |
| **Abstraction (Trừu tượng hóa)** | - `BaseEntity` và `AuditableEntity` trừu tượng hóa định danh `Id` và dấu vết kiểm toán `CreatedAt`, `LastModifiedAt`.<br>- `IBusinessRule` trừu tượng hóa mọi điều kiện kiểm tra nghiệp vụ phức tạp thành hợp đồng `bool IsBroken()`. |
| **Inheritance (Kế thừa)** | - Cấu trúc kế thừa phân cấp sạch: `Container`, `YardBlock`, `DeliveryOrder`, `ContainerLifecycle`, `ApplicationUser` đều kế thừa từ `AuditableEntity` $\rightarrow$ `BaseEntity`. |
| **Polymorphism (Đa hình)** | - 8 Business Rules đều hiện thực `IBusinessRule`. Bộ điều phối `BusinessRuleValidator` tiếp nhận danh sách đa hình `IEnumerable<IBusinessRule>` để kiểm tra tự động. |

---

### 3.2. Nguyên Lý SOLID

| Nguyên Lý | Cách Áp Dụng Cụ Thể Trong Domain |
|---|---|
| **S - Single Responsibility** | - `ContainerNumber` chỉ làm nhiệm vụ xác thực số container chuẩn ISO 6346.<br>- `YardSlot` chỉ quản lý tọa độ và trạng thái chiếm chỗ của 1 ô đơn lẻ.<br>- Mỗi Rule trong `Rules/` chỉ chịu trách nhiệm kiểm tra một quy chuẩn duy nhất (hạn lệnh, chiều cao tầng, ô trống...). |
| **O - Open/Closed** | - Thiết kế `IBusinessRule` cho phép hệ thống bổ sung thêm hàng loạt quy tắc bãi hoặc xuất vỏ mới mà không cần sửa đổi bất kỳ dòng code nào trong `YardBlock` hay `DeliveryOrder`. |
| **L - Liskov Substitution** | - Mọi class rule đều có thể thay thế hoàn toàn cho interface `IBusinessRule` khi truyền vào `BusinessRuleValidator.Validate(...)`. |
| **I - Interface Segregation** | - Tách riêng biệt `ITenantEntity` và `IBusinessRule`. Chỉ những thực thể gắn liền với tài sản tại bãi mới triển khai `ITenantEntity`. |
| **D - Dependency Inversion** | - Tầng Domain nằm ở tâm kiến trúc Clean Architecture, không phụ thuộc vào bất kỳ thư viện ngoài hay tầng nào khác ($0\text{ external dependencies}$). |

---

## 4. Thống Kê Toàn Bộ Test Suite

```powershell
# Lệnh thực thi toàn bộ kiểm thử giải pháp:
dotnet test SnpDepotManagement.sln
```

* **Kết quả thực tế**:
  - `tests/Domain.UnitTests`: **120 Passed**, 0 Failed, 0 Skipped
  - `tests/Architecture.Tests`: **1 Passed**, 0 Failed, 0 Skipped
  - **Tổng cộng: 121 / 121 Tests Passed (100% tỷ lệ thành công)**
  - **Thời gian thực thi:** $\approx 1.5$ giây
