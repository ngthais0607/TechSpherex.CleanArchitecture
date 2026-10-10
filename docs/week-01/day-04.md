# Tuần 1 - Ngày 4: Quy Tắc Xếp Bãi

## 1. Việc Đã Thực Hiện

- Triển khai bốn rule xếp bãi: kích thước/Bay, giới hạn Tier, slot trống và điểm đỡ bên dưới.
- Bổ sung trường hợp 20ft và 40ft.
- Viết unit test cho happy path, mép block, slot bị chiếm và container bị lơ lửng.

## 2. Yêu Cầu Cần Giải Quyết

- Container 20ft phải dùng Bay lẻ.
- Container 40ft dùng Bay chẵn logic và cần hai slot Bay lẻ liền kề.
- Không đặt container quá chiều cao block hoặc thiếu điểm đỡ.
- Rule phải độc lập để Application phối hợp lại theo use case.

## 3. Phương Pháp Và Kết Quả

- Mỗi điều kiện được đóng gói thành một implementation của `IBusinessRule`.
- Rule chỉ đánh giá và trả mã lỗi; không tự truy vấn hoặc lưu dữ liệu.
- Test theo biên Bay/Tier và trạng thái slot giúp khóa hành vi.
- Tại cuối ngày, bốn yard rules đã sẵn sàng cho tầng Application.

Chi tiết tám rule của hệ thống xem tại [tài liệu tham chiếu Tuần 1](codebase-structure-and-files.md).
