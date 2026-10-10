# Tuần 1 - Ngày 5: Delivery Order Và Lifecycle

## 1. Việc Đã Thực Hiện

- Xây `DeliveryOrder`, `DeliveryOrderItem` và quota giao container.
- Xây `ContainerLifecycle` cho Gate-In/Gate-Out.
- Triển khai bốn rule xuất cổng.
- Viết test cho phân bổ quota, hạn lệnh, hãng tàu, trạng thái trong bãi và chặn Gate-Out hai lần.

## 2. Yêu Cầu Cần Giải Quyết

- Không giao vượt quota theo kích thước/loại.
- Chỉ xuất container đang ở bãi, đúng hãng tàu và trong hạn lệnh.
- Ghi nhận thời điểm và phương tiện vào/ra một cách tất định.
- Ngăn cùng lifecycle bị xuất hai lần.

## 3. Phương Pháp Và Kết Quả

- `DeliveryOrder` quản lý items qua backing field và `Allocate`.
- Lifecycle chuyển từ `InYard` sang `GatedOut`; thời gian được truyền từ Application.
- Bốn dispatch rules tách riêng để có thể kiểm thử độc lập.
- Các test tại cuối ngày xác nhận quota và state transition chính.
