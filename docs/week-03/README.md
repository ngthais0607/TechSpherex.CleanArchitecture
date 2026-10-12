# Tuần 3: Infrastructure Layer & Minimal API

Tuần 3 xây dựng toàn bộ tầng hạ tầng (Infrastructure Layer), liên kết dữ liệu thực tế với PostgreSQL 17 và Redis 7, bảo đảm cô lập đa người thuê (Multi-Tenancy), kiểm soát đồng thời lạc quan (Optimistic Concurrency bằng PostgreSQL `xmin`), kiểm thử tích hợp thực tế với Testcontainers và phơi bày Minimal API endpoints với tài liệu Scalar UI.

## Nhật Ký Từng Ngày

| Ngày | Nội dung | Tài liệu |
|---|---|---|
| 1 | Docker Environment, Multi-Tenancy và AppDbContext Baseline | [day-01.md](day-01.md) |
| 2 | Entity Configurations, Indexes và Optimistic Concurrency Tokens | [day-02.md](day-02.md) |
| 3 | Migrations, Seeder, Dịch vụ Identity và HybridCache | [day-03.md](day-03.md) |
| 4 | Integration Tests với Testcontainers (PostgreSQL 17 thật) | [day-04.md](day-04.md) |
| 5 | Minimal API Pipeline, Authentication, Scalar UI và Endpoints | [day-05.md](day-05.md) |
| 6 | Code Review, Kiểm Tra Toàn Diện và Tổng Kết Tuần 3 | [day-06.md](day-06.md) |

## Kết Quả Đạt Được

- **Toàn bộ 13 Entity Configurations** với naming convention `snake_case`, kiểu `timestamptz` (UTC), index và shadow property `xmin` token concurrency.
- **Migration đầu tiên `InitialCreate`** được sinh hoàn chỉnh và áp dụng tự động.
- **Dịch vụ cốt lõi:**
  - `PasswordHasher`: PBKDF2 (100,000 iterations, SHA-256).
  - `TokenService`: JWT Access Token + SHA-256 Refresh Token.
  - `HybridCacheService`: Kết hợp HybridCache (.NET 9) với Redis SCAN, phân cấp tags và prefix `tenant:{tenantId}:{key}`.
  - `AppDbSeeder`: Khởi tạo 4 roles (`Admin`, `YardPlanner`, `OrderClerk`, `GateClerk`) và tài khoản Admin mặc định.
- **Integration Test Suite thực tế:** 7 bài test chuyên sâu chạy trên container Docker PostgreSQL 17 thật (xác nhận Slot Concurrency, Quota Concurrency, Tenant Isolation, Foreign Key Constraints & Rollback).
- **Minimal API Endpoints:** Hỗ trợ đăng nhập, refresh token, xem danh sách và đăng ký container tích hợp Scalar UI.
- **Tổng số tests toàn solution:** **294 tests pass 100%** (125 Domain, 131 Application, 24 Infrastructure Unit, 7 Infrastructure Integration, 7 Architecture).
