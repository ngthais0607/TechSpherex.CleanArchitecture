# Tuần 1 - Ngày 3: Cấu Trúc Bãi

## 1. Việc Đã Thực Hiện

- Xây `YardPosition`, `YardBlock` và `YardSlot`.
- Tạo block chuẩn theo Bay/Row/Tier và block ảo không có lưới slot.
- Triển khai mở rộng và thu hẹp block.
- Viết test cho sinh slot, mở rộng, thu hẹp và xung đột slot đang chiếm.

## 2. Yêu Cầu Cần Giải Quyết

- Biểu diễn vị trí bãi nhất quán.
- Chỉ tạo slot vật lý tại Bay lẻ cho đơn vị 20ft.
- Không làm mất container khi thay đổi kích thước block.
- Giữ quyền quản lý slot bên trong aggregate `YardBlock`.

## 3. Phương Pháp Và Kết Quả

- `YardBlock` sở hữu backing field `_slots` và chỉ công khai `IReadOnlyCollection`.
- Constructor `YardSlot` là `internal` để hạn chế nơi tạo slot.
- `Expand` chỉ thêm tọa độ mới; `Shrink` từ chối vùng còn container.
- Kết quả kiểm thử tại cuối ngày xác nhận cấu trúc block chuẩn và block ảo.
