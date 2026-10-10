# Tuần 2 - Ngày 4: Vận Hành Bãi

## 1. Việc Đã Thực Hiện

- Triển khai Place, Remove và Relocate container.
- Triển khai truy vấn tìm slot trống.
- Kết nối bốn yard rules qua `IRuleEngine`.
- Bổ sung bảo vệ chống dỡ container đang làm điểm đỡ.

## 2. Yêu Cầu Cần Giải Quyết

- Kiểm tra đồng thời kích thước/Bay, Tier, slot trống và điểm đỡ.
- Xử lý đúng một slot cho 20ft và hai slot đỡ cho 40ft.
- Không để trạng thái nguồn bị mất khi relocate đích không hợp lệ.
- Ưu tiên slot thấp và hợp lệ khi tìm vị trí trống.

## 3. Phương Pháp Và Kết Quả

- Handler tải aggregate rồi tạo các Domain rule cần thiết.
- Relocate kiểm tra đích trước khi hoàn tất thay đổi và khôi phục nguồn khi cần.
- Remove kiểm tra container phía trên trước khi giải phóng slot.
- Unit tests tại cuối ngày bao phủ các luồng đặt, dỡ, chuyển và tìm slot.

Chi tiết rule và orchestration xem tại [tài liệu tham chiếu Tuần 2](codebase-structure-and-files.md).
