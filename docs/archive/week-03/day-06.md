# Tuần 3 - Ngày 6: Code Review, Kiểm Tra Toàn Diện Và Tổng Kết Tuần 3

## 1. Việc Đã Thực Hiện

- Rà soát toàn diện mã nguồn lớp `Infrastructure` và `Api`:
  - **Kiểm tra rò rỉ Tenant:** Toàn bộ thực thể thuộc depot đều kế thừa `ITenantEntity`. Bộ lọc `HasQueryFilter` được kích hoạt mặc định và chỉ cho phép bypass có chủ đích trong các tác vụ nội bộ. Trong `SaveChangesAsync`, `TenantId` luôn được kiểm tra và tự động gán nếu chưa có.
  - **Kiểm tra Concurrency Token:** Các điểm nóng xung đột dữ liệu cao (`YardSlot`, `DeliveryOrderItem`, `ContainerLifecycle`) đều được bảo vệ bằng shadow property `xmin` (PostgreSQL `xid`).
  - **Kiểm tra Kiến trúc (Clean Architecture):** `Infrastructure` chỉ tham chiếu `Application` (và gián tiếp `Domain`). `Api` làm composition root, tham chiếu cả `Application` và `Infrastructure`.
- Chạy toàn bộ test suite trên toàn solution: Xác nhận 294 tests chạy xanh 100%.

## 2. Các Bài Học Và Điểm Lưu Ý Kỹ Thuật

- **EF Core Owned Entity Navigation trong HasIndex:** Trong EF Core, `HasIndex` không chấp nhận biểu thức lambda duyệt qua navigation thuộc tính con (`s => s.Position.Bay`). Do đó, chỉ mục composite bao gồm cả cột thuộc sở hữu của thực thể con được khai báo trực tiếp ở migration level hoặc database level để bảo đảm toàn vẹn dữ liệu tuyệt đối.
- **ISO 6346 Modulo 11:** Khởi tạo số container hợp lệ trong kiểm thử phải tính toán check digit Modulo 11 chính xác để tránh lỗi `NullReferenceException` khi truy cập `.Value` của `Result.Failure`.

## 3. Tổng Kết Chất Lượng

- 294 tests tự động xanh 100%:
  - Domain.UnitTests: 125 tests
  - Application.UnitTests: 131 tests
  - Infrastructure.UnitTests: 24 tests
  - Infrastructure.IntegrationTests: 7 tests (chạy với PostgreSQL 17 Testcontainers)
  - Architecture.Tests: 7 tests
- Toàn bộ cam kết của Tuần 3 đã được hoàn thành trọn vẹn theo đúng kế hoạch.
