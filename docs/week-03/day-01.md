# Tuần 3 - Ngày 1: Docker Environment, Multi-Tenancy Và AppDbContext Baseline

## 1. Việc Đã Thực Hiện

- Cấu hình môi trường hạ tầng qua `docker-compose.yml`, `.env.example` (PostgreSQL 17, Redis 7).
- Triển khai multi-tenancy qua `TenantProvider`, `TenantMiddleware` (nhận diện qua header `X-Tenant-Id`).
- Triển khai `CurrentUser` lấy context người dùng từ `ClaimsPrincipal`.
- Xây dựng `AppDbContext` kế thừa `DbContext`, triển khai `IAppDbContext`:
  - Tự động áp dụng Global Query Filter cho tất cả thực thể `ITenantEntity`.
  - Tự động điền `TenantId` và audit fields (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`) trong `SaveChangesAsync`.
- Viết unit tests cho `TenantProvider` và `AppDbContextTenantFilterTests`.

## 2. Yêu Cầu Cần Giải Quyết

- Bảo đảm dữ liệu giữa các depot (tenant) được cô lập tuyệt đối ở tầng ORM.
- Tự động hóa việc kiểm tra audit và tenant để các command handler không phải gán thủ công.
- Tách biệt hoàn toàn tầng cơ sở hạ tầng mà vẫn bảo đảm hợp đồng với tầng Application.

## 3. Phương Pháp Và Kết Quả

- Sử dụng `EFCore.NamingConventions` (snake_case) và PostgreSQL Npgsql provider.
- Expression tree dynamic cho `ITenantEntity` query filter trong `OnModelCreating`.
- Toàn bộ unit tests khởi đầu cho Infrastructure chạy xanh 100%.
