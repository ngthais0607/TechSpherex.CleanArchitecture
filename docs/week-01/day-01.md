# Tuần 1 - Ngày 1: Khởi Tạo Domain

## 1. Việc Đã Thực Hiện

- Khởi tạo solution Clean Architecture và các project `Domain`, `Application`, `Infrastructure`, `Api` cùng ba project test.
- Thiết lập Central Package Management và target framework .NET 10.
- Xây các kiểu nền tảng `BaseEntity`, `AuditableEntity`, `ITenantEntity`, `Error`, `Result` và hợp đồng business rule.
- Thiết lập hướng phụ thuộc để Domain không tham chiếu các tầng bên ngoài.

## 2. Yêu Cầu Cần Giải Quyết

- Tạo nền tảng chung cho entity và lỗi nghiệp vụ mà không gắn Domain với HTTP hoặc database.
- Chuẩn hóa cách biểu diễn thành công/thất bại cho thao tác nghiệp vụ.
- Chuẩn bị tenant và audit mà không đưa chi tiết hạ tầng vào Domain.

## 3. Phương Pháp Và Kết Quả

- Dùng `Result`/`Result<T>` và `Error` cho lỗi nghiệp vụ dự kiến.
- Dùng `ITenantEntity` làm hợp đồng; tenant isolation thực tế được để lại cho Infrastructure.
- Dùng architecture test để bảo vệ hướng phụ thuộc.
- Kết thúc ngày với bộ khung có thể mở rộng cho entity và rule.

Chi tiết xem tại [tài liệu tham chiếu Tuần 1](codebase-structure-and-files.md).
