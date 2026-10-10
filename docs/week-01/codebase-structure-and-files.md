# Tuần 1: Cấu Trúc Domain Và Giải Thích Kỹ Thuật

## 1. Phạm Vi

Tuần 1 xây dựng tầng `Domain`, nơi chứa mô hình và quy tắc nghiệp vụ độc lập với API, cơ sở dữ liệu và giao diện. Tài liệu này là nguồn tham chiếu cho cấu trúc code, cú pháp C# đáng chú ý, mô hình dữ liệu đề xuất và phạm vi kiểm thử của tuần.

```text
src/Domain/
├── Common/                 # Kiểu nền tảng: Entity, Result, Error, phân trang, business rule
├── Entities/
│   ├── Containers/         # Container vỏ rỗng
│   ├── Identity/           # Tài khoản nghiệp vụ
│   ├── MasterData/         # Hãng tàu, khách hàng, phương tiện, chuyến tàu
│   ├── Operations/         # Delivery Order và vòng đời Gate-In/Gate-Out
│   └── Yard/               # Block và slot bãi
├── Enums/                  # Tập giá trị hữu hạn của nghiệp vụ
├── Rules/                  # Tám quy tắc xếp bãi và xuất cổng
└── ValueObjects/           # ContainerNumber và YardPosition
```

Tầng này không tham chiếu `Application`, `Infrastructure` hoặc `Api`. Những chi tiết như EF Core mapping, transaction, JWT và HTTP thuộc các tầng bên ngoài.

## 2. Nhiệm Vụ Của Từng Nhóm File

### 2.1. Common

| Thành phần | Nhiệm vụ |
|---|---|
| `BaseEntity` | Cung cấp định danh `Guid Id` cho entity. |
| `AuditableEntity` | Bổ sung thời điểm/người tạo và cập nhật. Giá trị audit cuối cùng sẽ do Infrastructure quản lý khi persistence được triển khai đầy đủ. |
| `ITenantEntity` | Đánh dấu entity có dữ liệu phân vùng theo depot/tenant. Interface chưa tự tạo tenant isolation. |
| `Error` | Mang mã lỗi và mô tả lỗi nghiệp vụ. |
| `Result`, `Result<T>` | Biểu diễn thành công/thất bại mà không dùng exception cho nhánh nghiệp vụ dự kiến. |
| `PagedResult<T>` | Gói danh sách phân trang cùng tổng số bản ghi và trạng thái trang trước/sau. |
| `IBusinessRule` | Hợp đồng cho một quy tắc có thể đánh giá độc lập. |
| `BusinessRuleValidator` | Chạy rule và chuyển vi phạm thành kết quả phù hợp ở Domain. |

`Result` không có nghĩa chương trình không bao giờ phát sinh exception. Exception vẫn phù hợp cho lỗi hạ tầng hoặc trạng thái lập trình không dự kiến; lỗi nghiệp vụ thông thường được trả về bằng `Result`.

### 2.2. Entities

| Entity | Trách nhiệm chính |
|---|---|
| `Container` | Lưu số container, mã ISO, kích thước, loại, trọng lượng, hãng sở hữu, tình trạng kỹ thuật và phân loại. `Condition` và `Classification` độc lập. |
| `ApplicationUser` | Quản lý thông tin tài khoản, password hash, role và vòng đời refresh token hash. |
| `Customer` | Chuẩn hóa tên và kiểm tra mã số thuế gồm đúng 10 hoặc 13 chữ số. |
| `LineOperator` | Quản lý mã và tên hãng tàu; mã được chuẩn hóa chữ hoa. |
| `Vehicle` | Quản lý biển số và loại phương tiện. |
| `Voyage` | Quản lý tàu, số chuyến và thời điểm dự kiến rời cảng. |
| `YardBlock` | Aggregate root quản lý kích thước block và tập slot; hỗ trợ block chuẩn, block ảo, mở rộng và thu hẹp. |
| `YardSlot` | Một vị trí vật lý theo Bay/Row/Tier; chỉ `YardBlock` tạo slot qua constructor `internal`. |
| `DeliveryOrder` | Aggregate root quản lý lệnh giao vỏ, hạn lệnh và các dòng quota `DeliveryOrderItem`. |
| `ContainerLifecycle` | Ghi nhận Gate-In/Gate-Out, xe vào/ra, slot hiện tại và Delivery Order dùng khi xuất. |

`DeliveryOrderItem` được đặt cùng file với `DeliveryOrder` vì vòng đời của nó thuộc aggregate này và không được quản lý độc lập.

### 2.3. Value Objects

`ContainerNumber` là `public sealed record` bất biến. Factory `Create` chuẩn hóa khoảng trắng/chữ hoa và kiểm tra định dạng ISO 6346: ba chữ owner, category `U/J/Z`, sáu số serial và check digit Modulo 11.

`YardPosition` biểu diễn Block/Bay/Row/Tier và cung cấp biểu diễn chuỗi như `A-01-02-03`. Việc xếp container thực tế dùng `YardSlot`; value object này phù hợp cho truyền/hiển thị tọa độ.

### 2.4. Enums

`enum` giới hạn một thuộc tính vào tập giá trị đã biết, giúp tránh chuỗi tự do và điều kiện không hợp lệ.

| Enum | Giá trị hiện tại |
|---|---|
| `ContainerSize` | `Ft20 = 20`, `Ft40 = 40` |
| `ContainerType` | `Dry`, `Reefer`, `OpenTop`, `FlatRack`, `Bunker`, `Ventilated`, `Specialized` |
| `Classification` | `A = 1`, `B = 2`, `C = 3` |
| `ContainerCondition` | `Normal`, `Damaged`, `Dented`, `Bent` |
| `LifecycleStatus` | `InYard`, `GatedOut` |
| `YardBlockType` | `Standard`, `Virtual` |
| `VehicleType` | `Truck`, `Barge`, `Train`, `Other` |

`ContainerClassification` hiện là enum tương thích có thêm alias `GradeA/GradeB/GradeC`; nghiệp vụ chính đang sử dụng `Classification`. Khi mapping database cần lưu đúng giá trị số thực tế của enum thay vì giả định bắt đầu từ `0`.

## 3. Tám Business Rules

### Quy tắc xếp bãi

| Rule | Điều kiện được bảo vệ |
|---|---|
| `ValidBayContainerSizeRule` | Container 20ft dùng Bay lẻ; 40ft dùng Bay chẵn và không vượt mép block. |
| `TierWithinBlockLimitRule` | Tier mục tiêu không vượt chiều cao block. |
| `SlotIsFreeRule` | Slot phải trống; với 40ft kiểm tra hai slot đỡ ở Bay liền kề. |
| `SlotBelowMustBeOccupiedRule` | Container đặt trên Tier lớn hơn 1 phải có đủ điểm đỡ bên dưới. |

### Quy tắc xuất cổng

| Rule | Điều kiện được bảo vệ |
|---|---|
| `ContainerIsInYardRule` | Lifecycle phải đang `InYard` và chưa có thời điểm ra. |
| `ContainerBelongsToLineOperatorRule` | Hãng tàu của container phải khớp lệnh giao. |
| `DeliveryOrderNotExpiredRule` | Ngày xử lý không vượt ngày hết hạn của lệnh. |
| `DeliveryOrderHasRemainingQuantityRule` | Lệnh còn quota đúng kích thước và loại container. |

Rule chỉ đánh giá điều kiện. Việc tải dữ liệu, phối hợp nhiều rule và lưu thay đổi thuộc tầng `Application`.

## 4. Cấu Trúc C# Đáng Chú Ý

### `public`, `private`, `internal`

- `public`: thành phần được truy cập từ project khác.
- `private`: chỉ dùng bên trong chính class; phù hợp để khóa constructor hoặc setter.
- `internal`: dùng trong cùng assembly. `YardSlot` dùng constructor `internal` để `YardBlock` tạo slot nhưng không mở quyền tạo tùy ý cho bên ngoài Domain.

### `sealed class` và `sealed record`

`sealed` ngăn kế thừa. Entity và value object của dự án được đóng để bảo vệ invariant và tránh lớp con thay đổi hành vi ngoài kiểm soát.

`record` cung cấp so sánh theo giá trị. `ContainerNumber` và `YardPosition` là `sealed record`, phù hợp với value object. Record không mặc nhiên bất biến; tính bất biến ở đây đến từ thuộc tính chỉ đọc và constructor `private`.

### `private set`

```csharp
public ContainerCondition Condition { get; private set; }
```

Code bên ngoài đọc được trạng thái nhưng chỉ entity mới thay đổi được. Thay đổi phải đi qua phương thức nghiệp vụ như `MarkDamaged`, `Allocate`, `Occupy` hoặc `GateOut`.

### Factory Method

Factory như `Container.Create(...)` hoặc `ContainerNumber.Create(...)` tập trung chuẩn hóa và validation tại một điểm. Constructor `private` ngăn tạo object bỏ qua invariant. Factory có thể trả `Result<T>` khi đầu vào có thể thất bại; `ContainerLifecycle.GateIn(...)` trả entity trực tiếp vì dữ liệu đầu vào đã được Application kiểm tra.

### Backing Field và `IReadOnlyCollection<T>`

```csharp
private readonly List<DeliveryOrderItem> _items = new();
public IReadOnlyCollection<DeliveryOrderItem> Items => _items.AsReadOnly();
```

Danh sách thật được giữ riêng; bên ngoài chỉ đọc. Aggregate root cung cấp phương thức thay đổi để duy trì invariant. EF Core cần mapping backing field tương ứng ở Infrastructure.

### Generic `Result<T>` và `PagedResult<T>`

`T` là tham số kiểu. Một cấu trúc dùng được cho nhiều kiểu giá trị mà vẫn giữ type safety, ví dụ `Result<Container>` hoặc `PagedResult<ContainerDto>`.

### Biểu thức `=>`, nullable và pattern matching

- `=>` phù hợp cho thuộc tính tính toán ngắn như `IsInDepot` hoặc `RemainingQuantity`.
- Dấu `?` như `Guid?` hoặc `DateTime?` cho biết giá trị có thể chưa tồn tại.
- Pattern matching/switch expression hữu ích khi ánh xạ enum để hiển thị; không nên tự phát sinh quyết định nghiệp vụ không có trong entity.

## 5. Luồng Nghiệp Vụ Chính

### Container

1. `ContainerNumber.Create` chuẩn hóa và kiểm tra ISO 6346.
2. `Container.Create` kiểm tra `TareWeight > 0` và `MaxGrossWeight > TareWeight`.
3. Container mới có `Condition.Normal` và `Classification.A`.
4. `MarkDamaged(condition)` chỉ cập nhật tình trạng khác `Normal`; không tự đổi phân loại A/B/C.

### Yard

1. `YardBlock.CreateStandard` sinh slot Bay lẻ theo Rows và Tiers.
2. Bay chẵn là vị trí logic cho container 40ft, chiếm hai slot Bay lẻ liền kề.
3. `Expand` chỉ thêm slot mới; `Shrink` từ chối nếu vùng bị cắt còn container.
4. Block ảo không tạo lưới slot vật lý.

### Delivery Order và Lifecycle

1. Delivery Order chứa quota theo `ContainerSize` và `ContainerType`.
2. `Allocate` không cho giao vượt số lượng còn lại.
3. `GateIn` ghi thời gian được truyền từ Application và gắn `CurrentSlotId`.
4. `GateOut` chặn xuất hai lần, ghi xe ra/DO/thời gian và xóa liên kết slot hiện tại.

## 6. Mô Hình Dữ Liệu Đề Xuất

Phần này là **thiết kế đề xuất cho Infrastructure**, không phải schema đã được migration hoặc integration test xác nhận.

| Bảng | Cột nghiệp vụ chính |
|---|---|
| `containers` | `container_number`, `iso_code`, `size`, `type`, `tare_weight`, `max_gross_weight`, `date_of_manufacture`, `owner_code`, `condition`, `classification`, `tenant_id` |
| `yard_blocks` | `code`, `name`, `is_virtual`, `block_type`, `max_bays`, `max_rows`, `max_tiers`, `tenant_id` |
| `yard_slots` | `block_id`, `bay`, `row`, `tier`, `container_id`, `is_occupied` |
| `delivery_orders` | `order_number`, `line_operator`, `customer_tax_code`, `customer_name`, `expiry_date`, `outbound_vessel`, `is_completed`, `tenant_id` |
| `delivery_order_items` | `delivery_order_id`, `container_size`, `container_type`, `required_quantity`, `delivered_quantity` |
| `container_lifecycles` | `container_id`, `line_operator`, `classification`, `condition`, `status`, `in_vehicle_number`, `in_date`, `out_vehicle_number`, `out_date`, `delivery_order_id`, `current_slot_id`, `tenant_id` |
| `customers` | `tax_code`, `name` |
| `line_operators` | `code`, `name` |
| `vehicles` | `plate_number`, `type` |
| `voyages` | `vessel_name`, `voyage_number`, `etd` |
| `application_users` | `user_name`, `email`, `first_name`, `last_name`, `password_hash`, `role`, `refresh_token_hash`, `refresh_token_expiry_time`, `is_active` |

Các ràng buộc unique, foreign key, conversion enum/value object, global tenant filter và delete behavior phải được chốt trong EF Core mapping. Không giả định `DeliveryOrder` đang giữ FK tới `Customer`/`LineOperator`: entity hiện lưu snapshot chuỗi trực tiếp.

## 7. Kiểm Thử Tuần 1

`tests/Domain.UnitTests` hiện có **120 tests** bao phủ:

- ISO 6346 và value objects.
- Invariant của container, master data và tài khoản.
- Sinh/mở rộng/thu hẹp block và slot.
- Delivery Order, quota và lifecycle.
- Tám business rules.

Architecture tests kiểm tra hướng phụ thuộc của Domain nhưng không chứng minh hành vi database. Unit tests chạy trong bộ nhớ cũng không chứng minh transaction, foreign key, unique index, query filter hoặc optimistic concurrency của relational database.

## 8. Quan Hệ Với Tuần 2

Domain cung cấp entity, value object, enum, rule và `Result`. Tuần 2 sử dụng các thành phần này để xây CQRS handlers, validation, persistence abstraction và workflow ứng dụng. Xem [cấu trúc Tuần 2](../week-02/codebase-structure-and-files.md) sau khi hoàn tất tầng Application.
