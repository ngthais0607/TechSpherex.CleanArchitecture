# Tuần 3 - Ngày 4: Integration Tests Với PostgreSQL Thật (Testcontainers)

## 1. Việc Đã Thực Hiện

- Thiết lập dự án `tests/Infrastructure.IntegrationTests` với `Testcontainers.PostgreSql`.
- Tạo `PostgreSqlFixture` (`IAsyncLifetime`) tự động khởi chạy PostgreSQL 17 container và áp dụng migrations khi test chạy.
- Viết 4 bộ integration tests chuyên sâu:
  1. `YardSlotConcurrencyTests`: Hai giao dịch đồng thời đặt cùng 1 slot -> Đúng 1 thành công, giao dịch sau nhận `DbUpdateConcurrencyException`.
  2. `GateOutQuotaConcurrencyTests`: Hai giao dịch đồng thời xuất rỗng container khi quota chỉ còn 1 -> 1 thành công, giao dịch sau nhận `DbUpdateConcurrencyException`.
  3. `TenantIsolationTests`: Dữ liệu phân tách tuyệt đối giữa các tenant; cùng số container vẫn tồn tại được ở hai depot khác nhau nhờ chỉ mục composite `(tenant_id, container_number)`.
  4. `IntegrityConstraintsAndRollbackTests`: Kiểm thử ràng buộc khóa ngoại `Restrict` (không xóa container khi còn lifecycle), `Cascade` khi xóa đơn hàng, và rollback giao dịch khi xảy ra lỗi.

## 2. Yêu Cầu Cần Giải Quyết

- Xác minh hành vi thực tế của cơ sở dữ liệu quan hệ PostgreSQL (đặc biệt là cơ chế optimistic concurrency bằng `xmin`) mà `InMemoryDatabase` không thể mô phỏng được.
- Đảm bảo tính toàn vẹn của ACID và khả năng rollback hoàn toàn.

## 3. Phương Pháp Và Kết Quả

- Sử dụng `postgres:17-alpine` trong Testcontainers.
- Tạo helper `ContainerTestHelper` tuân thủ thuật toán tính check digit Modulo 11 của ISO 6346.
- Toàn bộ 7 integration tests chạy trên PostgreSQL thật xanh 100%.
