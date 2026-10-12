# Tuần 3 - Ngày 2: Entity Configurations, Indexes Và Optimistic Concurrency Tokens

## 1. Việc Đã Thực Hiện

- Viết các lớp `IEntityTypeConfiguration<T>` cho toàn bộ 13 thực thể:
  - Identity: `ApplicationUser`, `Role`, `UserRole`.
  - MasterData: `Customer`, `LineOperator`, `Voyage`, `Vehicle`.
  - Yard: `YardBlock`, `YardSlot` (`Position` Owned Entity).
  - Containers: `Container` (ValueConverter cho `ContainerNumber`).
  - Operations: `DeliveryOrder`, `DeliveryOrderItem`, `ContainerLifecycle`.
- Cấu hình chỉ mục hiệu năng và chỉ mục duy nhất (bao gồm TenantId).
- Cấu hình optimistic concurrency token dựa trên system column `xmin` của PostgreSQL (`xid`) cho `YardSlot`, `DeliveryOrderItem` và `ContainerLifecycle`.
- Viết `ModelValidationTests` để kiểm tra tính toàn vẹn của mô hình EF Core.

## 2. Yêu Cầu Cần Giải Quyết

- Tuân thủ quy tắc Fluent API trong `Persistence/Configurations`, không dùng Data Annotations trong Domain.
- Ngăn chặn triệt để xung đột đặt chỗ (slot) và quota xuất rỗng khi có nhiều giao dịch thực thi đồng thời.
- Quản lý mapping của Value Object `ContainerNumber` và Owned Entity `YardPosition`.

## 3. Phương Pháp Và Kết Quả

- Sử dụng shadow property `xmin` kiểu `xid` với `IsConcurrencyToken()` và `ValueGeneratedOnAddOrUpdate()`.
- Tạo `ModelValidationTests` xác nhận 13 entity types, owned navigations và các unique indexes hợp lệ.
