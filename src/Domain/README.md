# SnpDepotManagement.Domain

Tầng **Domain** là trái tim của hệ sinh thái phần mềm quản lý Depot Container vỏ rỗng (Empty Container Depot) của SNP. Tầng này được thiết kế theo các nguyên tắc cốt lõi của **Clean Architecture** và **Domain-Driven Design (DDD)**.

---

## 1. Nhiệm Vụ Của Tầng Domain

Trong kiến trúc Clean Architecture:
- **Độc lập tuyệt đối**: Lớp Domain **không** phụ thuộc vào bất kỳ tầng nào khác (`Application`, `Infrastructure`, `Api`) và **không** sử dụng bất kỳ thư viện ngoài (Third-party NuGet Package) nào.
- **Trung tâm nghiệp vụ (Single Source of Truth)**: Định nghĩa toàn bộ thực thể nghiệp vụ (Entities), đối tượng giá trị (Value Objects), quy tắc bất biến (Invariants) và các quy tắc kiểm soát lưu trữ, cấp phát container.
- **Bảo vệ tính toàn vẹn dữ liệu**: Mọi trạng thái hợp lệ của hệ thống phải được đảm bảo ngay trong các hàm khởi tạo (Factory Methods) và phương thức nghiệp vụ của Entity, ngăn chặn hoàn toàn trạng thái dữ liệu rác trước khi đẩy xuống cơ sở dữ liệu.

---

## 2. Cấu Trúc Thành Phần Trong Thư Mục Domain

```text
src/Domain/
├── Common/                  # Các thành phần dùng chung cốt lõi
│   ├── BaseEntity.cs        # Entity cơ sở định danh Guid Id
│   ├── AuditableEntity.cs   # Lịch sử CreatedAt, CreatedBy, LastModifiedAt
│   ├── ITenantEntity.cs     # Hỗ trợ đa tenant (TenantId) theo depot
│   ├── Error.cs             # Đối tượng lỗi chuẩn hóa (Code, Description)
│   ├── Result.cs            # Kết quả thực thi kiểu đường ray (Railway Oriented)
│   ├── PagedResult.cs       # Model kết quả phân trang
│   └── Rules/               # Cơ chế kiểm soát Business Rules
│       ├── IBusinessRule.cs
│       ├── BusinessRuleValidator.cs
│       └── BusinessRuleException.cs
├── Enums/                   # Các tập giá trị hằng số nghiệp vụ
│   ├── ContainerEnums.cs    # ContainerSize (20/40), ContainerType (7 loại), Classification (A/B/C), Condition
│   ├── YardEnums.cs         # YardBlockType (Standard, Virtual), VehicleType
│   └── OperationEnums.cs    # LifecycleStatus (InYard, GatedOut)
├── ValueObjects/            # Đối tượng giá trị bất biến (Immutable)
│   ├── ContainerNumber.cs   # Chuẩn ISO 6346 (Owner, Category, Serial, Check Digit Modulo 11)
│   └── YardPosition.cs      # Tọa độ vị trí bãi (Block, Bay, Row, Tier)
├── Entities/                # Các thực thể nghiệp vụ chính
│   ├── Containers/          # Quản lý thuộc tính vỏ container (Tare, MaxGross, Condition)
│   ├── MasterData/          # Dữ liệu danh mục: LineOperator, Customer (MST 10/13), Voyage, Vehicle
│   ├── Identity/            # ApplicationUser trung lập trong Domain (RefreshToken băm)
│   ├── Yard/                # Quản lý không gian bãi: YardBlock (Aggregate), YardSlot
│   └── Operations/          # Quản lý tác nghiệp: DeliveryOrder, DeliveryOrderItem, ContainerLifecycle
└── Rules/                   # Bộ quy tắc nghiệp vụ độc lập (TDD)
    ├── ValidBayContainerSizeRule.cs
    ├── TierWithinBlockLimitRule.cs
    ├── SlotBelowMustBeOccupiedRule.cs
    ├── SlotIsFreeRule.cs
    ├── DeliveryOrderNotExpiredRule.cs
    ├── DeliveryOrderHasRemainingQuantityRule.cs
    ├── ContainerBelongsToLineOperatorRule.cs
    └── ContainerIsInYardRule.cs
```

---

## 3. Luồng Hoạt Động Nghiệp Vụ (Code Flow)

Hệ thống điều phối luồng quản lý depot qua 4 mắt xích chính:

```mermaid
flowchart TD
    subgraph GateIn [1. Tiếp nhận Vỏ - Gate-In]
        A[Xe chở vỏ đến cổng] --> B[Validate ContainerNumber ISO 6346]
        B --> C[Kiểm tra Tare / Max Gross & Condition]
        C --> D[Ghi nhận ContainerLifecycle: InYard]
    end

    subgraph Stacking [2. Quy Hoạch Xếp Bãi - Yard Stacking]
        D --> E{Kiểm tra 4 Yard Rules}
        E -- 1. ValidBayRule --> E1[20ft: Bay lẻ | 40ft: Bay chẵn chiếm 2 slot lẻ]
        E -- 2. TierLimitRule --> E2[Tier <= MaxTiers của Block]
        E -- 3. SlotBelowRule --> E3[Tier n đòi hỏi Tier n-1 đã có cont đỡ]
        E -- 4. SlotIsFreeRule --> E4[Slot mục tiêu phải còn trống]
        E1 & E2 & E3 & E4 --> F[Occupy Slot trong YardBlock]
    end

    subgraph GateOut [3. Cấp Vỏ Xuất Bãi - Gate-Out]
        G[Tài xế trình Lệnh Giao DeliveryOrder] --> H{Kiểm tra 4 Dispatch Rules}
        H -- NotExpiredRule --> H1[Ngày giao <= ExpiryDate của lệnh]
        H -- RemainingQtyRule --> H2[Lệnh còn số lượng đúng Size/Type]
        H -- LineOperatorRule --> H3[Container thuộc đúng hãng tàu của lệnh]
        H -- InYardRule --> H4[Container đang ở trong bãi & chưa ra]
        H1 & H2 & H3 & H4 --> I[DeliveryOrderItem.Allocate - giảm số còn lại]
        I --> J[ContainerLifecycle.GateOut - giải phóng Slot]
    end
```

### Chi tiết các luồng:

1. **Chuẩn hóa & Xác thực Container Number (ISO 6346):**
   - Người dùng nhập chuỗi (chấp nhận chữ thường, khoảng trắng).
   - Tự động chuẩn hóa về chữ hoa và loại bỏ khoảng trắng.
   - Kiểm tra định dạng 11 ký tự: 3 ký tự hãng tàu (`A-Z`), 1 ký tự nhóm thiết bị (`U/J/Z`), 6 số sê-ri (`0-9`), 1 chữ số kiểm tra.
   - Tính toán thuật toán **Modulo 11** với trọng số $2^i$ ($i$ từ 0 đến 9), bỏ qua các giá trị là bội số của 11. Trả về `Result<ContainerNumber>` an toàn kiểu dữ liệu.

2. **Quy ước Bãi Container (Yard Stacking Convention):**
   - **YardSlot**: Chỉ tồn tại vật lý ở các **bay lẻ** (tương ứng kích thước tiêu chuẩn 1 TEU - 20ft).
   - **Container 40ft**: Được định danh tại **bay chẵn** ở giữa và chiếm giữ đồng thời 2 slot lẻ liền kề (`bay - 1` và `bay + 1`) ở cùng một Row và Tier.
   - **Xếp tầng an toàn**: Container ở tầng $n > 1$ bắt buộc các slot đỡ bên dưới ở tầng $n - 1$ phải đang có container (`SlotBelowMustBeOccupiedRule`). Với cont 40ft, cả 2 slot đỡ bên dưới đều phải có container.
   - **Thu hẹp bãi (`Shrink`)**: Bị chặn ngay lập tức nếu bất kỳ slot nào trong vùng bị cắt đang có container lưu trữ.

3. **Quy trình Cấp vỏ Rỗng (`Gate-Out`):**
   - Khách hàng xuất trình lệnh giao `DeliveryOrder`.
   - Hệ thống kiểm tra: Lệnh còn hạn (`DeliveryOrderNotExpiredRule`), còn số lượng vỏ cần cấp (`DeliveryOrderHasRemainingQuantityRule`), container bốc ra thuộc đúng hãng tàu (`ContainerBelongsToLineOperatorRule`), và vỏ container đang có mặt tại bãi (`ContainerIsInYardRule`).
   - Cấp phát thành công: `DeliveryOrderItem.Allocate()` giảm số lượng còn lại, `ContainerLifecycle.GateOut()` ghi nhận thông tin xe nhận, thời gian ra và giải phóng slot bãi. Chặn hoàn toàn việc Gate-Out hai lần.

---

## 4. Hướng Dẫn Chạy Test

Toàn bộ nghiệp vụ trong Domain được kiểm thử tự động với framework **xUnit.v3** và thư viện assertion **FluentAssertions**.

### Chạy toàn bộ Unit Tests của Domain
Từ thư mục gốc dự án (`d:\SnpDepotManagement`):
```powershell
dotnet test tests/Domain.UnitTests/SnpDepotManagement.Domain.UnitTests.csproj
```

### Chạy kiểm tra kiến trúc (Architecture Tests)
Kiểm tra tự động để đảm bảo tầng Domain không bị ai vô tình tham chiếu ngược hoặc phụ thuộc vào bên ngoài:
```powershell
dotnet test tests/Architecture.Tests/SnpDepotManagement.Architecture.Tests.csproj
```

### Chạy toàn bộ test solution kèm báo cáo chi tiết
```powershell
dotnet test SnpDepotManagement.sln --logger "console;verbosity=normal"
```

### Chạy riêng một nhóm Test cụ thể
Ví dụ chỉ chạy kiểm thử thuật toán số container ISO 6346:
```powershell
dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~ContainerNumberTests"
```
Ví dụ chỉ chạy kiểm thử các quy tắc bãi (Yard Rules):
```powershell
dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~Rules"
```
