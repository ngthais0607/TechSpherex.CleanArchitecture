# Tuần 2 - Ngày 2: Identity Và Master Data

## 1. Việc Đã Thực Hiện

- Triển khai Login, Register và Refresh Token.
- Triển khai command/query cho Customer, LineOperator, Vehicle và Voyage.
- Bổ sung DTO, validator và unit test tương ứng.
- Áp dụng `TimeProvider` cho thời hạn token.

## 2. Yêu Cầu Cần Giải Quyết

- Chỉ Admin được tạo tài khoản và gán role.
- Không lưu refresh token dạng rõ; phải hash và xoay token.
- Chống trùng mã danh mục và kiểm tra dữ liệu đầu vào.
- Không trả trực tiếp Domain entity ra ngoài Application.

## 3. Phương Pháp Và Kết Quả

- Handler dùng `ICurrentUser`, `IPasswordHasher` và `ITokenService`.
- Query ánh xạ entity sang DTO.
- FluentValidation xử lý hình thức request; Domain giữ invariant.
- Kết quả test tại cuối ngày được lưu như mốc lịch sử, không thay thế tổng số hiện tại.

Chi tiết CQRS, record và TimeProvider xem tại [tài liệu tham chiếu Tuần 2](codebase-structure-and-files.md).
