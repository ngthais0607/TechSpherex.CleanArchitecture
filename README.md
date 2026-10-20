# SnpDepotManagement

Hệ thống quản lý depot container vỏ rỗng được xây dựng theo Clean Architecture trên .NET 10.

## Thiết Kế & Giao Diện

- [Giao diện & Đặc tả thiết kế](docs/design): Bao gồm file thiết kế `App.approved.tsx`, CSS giao diện và các tài liệu đặc tả nghiệp vụ từ UI.

## Trạng Thái Kiểm Thử

- Final gate sau khi hoàn thành toàn bộ T0–T8: **503 passed, 0 failed, 0 skipped** (Domain: 148, Application: 178, Architecture: 8, Infrastructure Unit: 35, Infrastructure Integration: 17, API Integration: 117 bao gồm 14 kịch bản nghiệm thu Acceptance Tests); build 0 lỗi.
- Postman: **88 requests** với per-role tokens và không có hardcoded secrets.

## Sẵn Sàng Cho Angular (Milestone)
Toàn bộ backend từ T0 đến T8 đã sẵn sàng 100% để đội frontend phát triển Angular UI:
- Đã cấu hình và hoàn thiện đầy đủ Domain validation, quy tắc xếp dỡ bãi (Y1–Y6), cổng (G1–G9), đặt chỗ (D1–D8).
- Hỗ trợ đầy đủ Lookups, Dashboard, Báo cáo, Quản lý hãng khai thác/Khách hàng (IsActive/Deactivate), Block ảo, Container display status.
- Dữ liệu mẫu (DemoDataSeeder) và kiểm tra ISO 6346 Modulo 11 check digit.

Application unit tests dùng EF Core InMemory; HTTP/transaction/tenant/concurrency tests dùng PostgreSQL Testcontainers thật. Benchmark tạo 20.000 container trên DB riêng. Startup, upgrade mapping và backup/restore đã chạy trên project Docker dùng một lần. Docker Desktop phải chạy Linux engine khi test; không skip integration tests để có kết quả xanh.
