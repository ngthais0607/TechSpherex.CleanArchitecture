# SnpDepotManagement

Hệ thống quản lý depot container vỏ rỗng được xây dựng theo Clean Architecture trên .NET 10.

## Thiết Kế & Giao Diện

- [Giao diện & Đặc tả thiết kế](docs/design): Bao gồm file thiết kế `App.approved.tsx`, CSS giao diện và các tài liệu đặc tả nghiệp vụ từ UI.

## Trạng Thái Kiểm Thử

- Final gate sau audit: **415 passed, 0 failed, 0 skipped** (Domain: 140, Application: 142, Architecture: 8, Infrastructure Unit: 33, Infrastructure Integration: 17, API Integration: 75); build 0 lỗi.
- Postman: **85 requests, 135 assertions, 0 failed** trên môi trường Docker.

Application unit tests dùng EF Core InMemory; HTTP/transaction/tenant/concurrency tests dùng PostgreSQL Testcontainers thật. Benchmark tạo 20.000 container trên DB riêng. Startup, upgrade mapping và backup/restore đã chạy trên project Docker dùng một lần. Docker Desktop phải chạy Linux engine khi test; không skip integration tests để có kết quả xanh.
