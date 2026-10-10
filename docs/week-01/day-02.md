# Tuần 1 - Ngày 2: Container, Master Data Và Identity

## 1. Việc Đã Thực Hiện

- Xây `ContainerNumber` theo ISO 6346 Modulo 11 và entity `Container`.
- Bổ sung `Customer`, `LineOperator`, `Vehicle`, `Voyage` và `ApplicationUser`.
- Định nghĩa enum kích thước, loại, tình trạng và phân loại container.
- Viết unit test cho số container, trọng lượng, mã số thuế, phương tiện và tài khoản.

## 2. Yêu Cầu Cần Giải Quyết

- Chuẩn hóa số container nhập từ cổng và phát hiện check digit sai.
- Bảo vệ invariant `TareWeight > 0` và `MaxGrossWeight > TareWeight`.
- Kiểm tra mã số thuế 10/13 chữ số và chuẩn hóa master data.
- Quản lý tình trạng kỹ thuật độc lập với phân loại A/B/C.

## 3. Phương Pháp Và Kết Quả

- Dùng factory trả `Result<T>` để chuẩn hóa và từ chối dữ liệu không hợp lệ.
- `MarkDamaged` chỉ cập nhật `Condition`; không tự động đổi `Classification`.
- Dùng constructor riêng và `private set` để bảo vệ trạng thái.
- Các test tại thời điểm kết thúc ngày xác nhận các invariant đã triển khai.

Chi tiết enum, factory và value object xem tại [tài liệu tham chiếu Tuần 1](codebase-structure-and-files.md).
