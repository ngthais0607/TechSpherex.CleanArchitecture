# Tuần 3 - Ngày 5: Minimal API Pipeline, Authentication, Scalar UI Và Endpoints

## 1. Việc Đã Thực Hiện

- Cấu hình file cấu hình `appsettings.json`: Thiết lập chuỗi kết nối PostgreSQL, Redis, cấu hình JWT và thông tin tài khoản Admin mặc định.
- Cấu hình `src/Api/Program.cs`:
  - Đăng ký `AddApplication()` và `AddInfrastructure(builder.Configuration)`.
  - Thiết lập Authorization policies cho các vai trò (`Admin`, `YardPlanner`, `OrderClerk`, `GateClerk`).
  - Tích hợp `IAppDbSeeder` tự động seed vai trò và tài khoản Admin khi API khởi động.
  - Tích hợp OpenAPI và giao diện tài liệu API hiện đại `Scalar.AspNetCore`.
  - Thiết lập pipeline middleware: `TenantMiddleware` (trích xuất header `X-Tenant-Id`), `UseAuthentication()`, `UseAuthorization()`.
- Tạo các nhóm endpoint Minimal API:
  - `Endpoints/IdentityEndpoints.cs`: `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/register`.
  - `Endpoints/ContainerEndpoints.cs`: `GET /api/containers` (phân trang, lọc theo tenant), `POST /api/containers` (đăng ký container mới).

## 2. Yêu Cầu Cần Giải Quyết

- Hoàn tất chu trình chạy End-to-End từ HTTP Request -> Pipeline Middleware -> Authentication/Authorization -> Application CQRS Handlers -> EF Core Persistence.
- Cung cấp giao diện trực quan cho lập trình viên qua Scalar API Reference.

## 3. Phương Pháp Và Kết Quả

- Tận dụng Minimal API endpoints nhóm theo `MapGroup` và metadata fluent.
- Endpoint điều phối trực tiếp qua generic CQRS handler abstractions (`ICommandHandler`, `IQueryHandler`), không coupling với framework thứ ba.
- Xác nhận giải pháp build thành công 0 lỗi.
