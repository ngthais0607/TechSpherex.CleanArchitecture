# Tuần 2 - Ngày 3: Container Và Cấu Hình Bãi

## 1. Việc Đã Thực Hiện

- Triển khai đăng ký, ghi nhận hư hỏng, tra cứu và phân trang container.
- Triển khai tạo, mở rộng, thu hẹp block và truy vấn layout.
- Bổ sung validator, DTO và unit test cho các use case.
- Kết nối ISO 6346 và invariant Domain vào handler.

## 2. Yêu Cầu Cần Giải Quyết

- Chống đăng ký trùng số container.
- Ghi nhận tình trạng hư hỏng nhưng không tự đổi phân loại.
- Không thu hẹp block nếu vùng bị xóa còn container.
- Trả layout và danh sách container dưới dạng DTO/phân trang.

## 3. Phương Pháp Và Kết Quả

- Handler truy vấn trùng lặp trước khi gọi factory Domain.
- `MarkContainerDamaged` cập nhật `Condition`; `Classification` độc lập.
- Yard handler ủy quyền sinh và thay đổi slot cho `YardBlock`.
- Các test tại cuối ngày xác nhận use case container và cấu hình bãi.
