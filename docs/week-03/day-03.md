# Tuần 3 - Ngày 3: Migrations, Seeder, Dịch Vụ Identity Và HybridCache

## 1. Việc Đã Thực Hiện

- Cài đặt `dotnet-ef` local tool và tạo `AppDbContextFactory` (`IDesignTimeDbContextFactory<AppDbContext>`).
- Sinh thành công migration ban đầu `InitialCreate` với bảng, khóa ngoại, kiểu dữ liệu snake_case và chỉ mục.
- Bổ sung composite unique index `(tenant_id, block_id, position_bay, position_row, position_tier)` cho `yard_slots`.
- Triển khai `PasswordHasher` dùng PBKDF2 (100,000 iterations, SHA-256).
- Triển khai `TokenService` phát hành JWT Access Token và Refresh Token 64-byte.
- Triển khai `HybridCacheService` kết hợp `HybridCache` (.NET 9) và Redis SCAN với định dạng khóa `tenant:{tenantId}:{key}` và phân cấp tag.
- Triển khai `AppDbSeeder` seed 4 vai trò chuẩn (`Admin`, `YardPlanner`, `OrderClerk`, `GateClerk`) và tài khoản Admin mặc định.
- Viết unit tests cho `PasswordHasher`, `TokenService`, `HybridCacheService` và `AppDbSeeder`.

## 2. Yêu Cầu Cần Giải Quyết

- Bảo mật thông tin đăng nhập, bảo đảm khóa bí mật JWT >= 32 ký tự.
- Caching đa tầng tốc độ cao, hỗ trợ xóa cache phân vùng theo tiền tố và tenant.
- Khởi tạo dữ liệu nền tảng tự động, có tính idempotent khi chạy nhiều lần.

## 3. Phương Pháp Và Kết Quả

- Sử dụng `Rfc2898DeriveBytes.Pbkdf2` với `CryptographicOperations.FixedTimeEquals` chống tấn công timing.
- Sử dụng tag-based cache invalidation của `HybridCache` kết hợp Redis multiplexer.
- 24/24 Infrastructure unit tests chạy xanh 100%.
