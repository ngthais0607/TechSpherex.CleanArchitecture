# Tuần 2 - Ngày 5: Delivery Order Và Cổng Bãi

## 1. Việc Đã Thực Hiện

- Triển khai tạo và truy vấn Delivery Order.
- Triển khai Gate-In, Gate-Out và lịch sử lifecycle.
- Phối hợp quota, slot và lifecycle trong handler.
- Bổ sung unit test cho bốn dispatch rules và các trường hợp lỗi.

## 2. Yêu Cầu Cần Giải Quyết

- Gate-In chỉ dùng slot trống và không tạo hai lifecycle đang hoạt động.
- Gate-Out chỉ cho container đúng hãng tàu, còn quota, còn hạn và đang ở bãi.
- Giải phóng slot, trừ quota và cập nhật lifecycle cùng một ranh giới lưu.
- Thời điểm Gate-In/Gate-Out phải kiểm thử tất định.

## 3. Phương Pháp Và Kết Quả

- Handler dùng `TimeProvider` và truyền UTC xuống Domain.
- Gate-Out gọi một lần `SaveChangesAsync` sau khi mọi rule đạt.
- Đây là một ranh giới lưu thay đổi; ACID/rollback cần relational integration test.
- Test tại cuối ngày xác nhận hành vi Application bằng `TestAppDbContext`.

Chi tiết luồng xem tại [tài liệu tham chiếu Tuần 2](codebase-structure-and-files.md).
