# Tuần 2 - Ngày 1: Abstractions Và CQRS

## 1. Việc Đã Thực Hiện

- Tạo CQRS in-house với command, query và handler interfaces.
- Tạo các abstraction cho database, identity, cache, rules và tenancy.
- Xây `RuleEngine`, `Roles` và đăng ký DI bằng reflection.
- Tạo `TestAppDbContext` để kiểm thử handler.

## 2. Yêu Cầu Cần Giải Quyết

- Điều phối use case mà không phụ thuộc MediatR.
- Cho Application truy cập persistence qua hợp đồng, không tham chiếu Infrastructure.
- Tự động đăng ký handler/validator để tránh danh sách DI thủ công.
- Giữ validator tách khỏi handler.

## 3. Phương Pháp Và Kết Quả

- Dùng generic interfaces và reflection scan Application assembly.
- Đăng ký `IRuleEngine`, `TimeProvider`, handler và validator tại `AddApplication`.
- `IAppDbContext` làm persistence boundary; không thêm `IUnitOfWork`.
- Kết quả kiểm thử tại cuối ngày xác nhận nền tảng CQRS và architecture ban đầu.

Chi tiết xem tại [tài liệu tham chiếu Tuần 2](codebase-structure-and-files.md).
