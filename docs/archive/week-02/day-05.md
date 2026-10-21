# Tuần 2 - Ngày 5: Delivery Order Và Cổng Bãi

## 1. Việc Đã Thực Hiện

- Triển khai tạo, xác thực và truy vấn Delivery Order (Lệnh giao container).
- Triển khai Gate-In (Nhập cổng / Hạ bãi), Gate-Out (Xuất cổng / Giao cont rỗng) và lịch sử `ContainerLifecycle`.
- Phối hợp quota lệnh giao, slot bãi và lifecycle trong cùng một transaction nguyên tử (`SaveChangesAsync`).
- Lưu trữ phương tiện vận tải vào/ra cổng (`InVehicleType`, `OutVehicleType`, biển số xe).
- Bổ sung unit test cho bốn quy tắc Gate-Out (dispatch rules), invariants Domain, và các trường hợp lỗi dữ liệu.

## 2. Yêu Cầu Cần Giải Quyết & Nguyên Tắc Nghiệp Vụ Cốt Lõi

### 2.1 Nguyên tắc bất biến về Giao nhận tại Depot
- **Depot tuyệt đối không tự ý giao container ra ngoài nếu không có lệnh của hãng vận chuyển (Line Operator)**.
- Mọi lệnh giao container (Delivery Order - DO) hợp lệ phải bao gồm đầy đủ 5 thành phần nghiệp vụ:
  1. **Khách hàng**: Mã số thuế (`CustomerTaxCode`) và Tên khách hàng (`CustomerName`).
  2. **Số lệnh**: Mã số lệnh giao (`OrderNumber`) do hãng tàu phát hành.
  3. **Hạn lệnh**: Ngày cuối cùng được phép giao container rỗng ra ngoài (`ExpiryDate`).
  4. **Chuyến tàu xuất**: Chuyến tàu xuất container này ra khỏi Việt Nam (`OutboundVessel`).
  5. **Định lượng container**: Số lượng container theo từng kích cỡ và loại (`DeliveryOrderItem`: `Size`, `Type`, `RequiredQuantity`).

### 2.2 Ràng buộc kiểm soát Gate-In và Gate-Out
- **Gate-In (Nhập cổng / Hạ bãi)**:
  - Chỉ cho phép xếp vào slot bãi còn trống và hợp lệ theo quy tắc xếp dỡ (Bay parity theo 20ft/40ft, giới hạn Tier của Block, và luật trọng lực tầng dưới phải có bệ đỡ).
  - Không tạo hai `ContainerLifecycle` cùng ở trạng thái hoạt động (`InYard`) cho cùng một container.
  - Ghi nhận loại phương tiện (`VehicleType`) và biển số phương tiện vào cổng.
- **Gate-Out (Xuất cổng / Giao container rỗng)**:
  - Chỉ xuất khi thoả mãn đồng thời 4 Business Rules:
    1. Container đang hiện diện ở bãi (`ContainerIsInYardRule`).
    2. Container thuộc đúng hãng tàu phát hành lệnh giao (`ContainerBelongsToLineOperatorRule`).
    3. Lệnh giao còn hạn hiệu lực (`DeliveryOrderNotExpiredRule`).
    4. Lệnh giao còn quota chưa cấp phát (`DeliveryOrderHasRemainingQuantityRule`).
  - Giải phóng slot, trừ quota lệnh và cập nhật trạng thái lifecycle sang `GatedOut` trong cùng một ranh giới lưu.
  - Ghi nhận loại phương tiện (`VehicleType`) và biển số phương tiện nhận container.
- **Thời điểm Gate-In / Gate-Out**: Phải kiểm thử tất định thông qua `TimeProvider`.

## 3. Phương Pháp Và Kết Quả

- **Phân định trách nhiệm UX vs Backend**:
  - Giao diện UI: Lọc sẵn toạ độ bãi hợp lệ hoặc hỗ trợ tự động gợi ý vị trí trống (`FindFreeSlotsQuery`), ngăn chặn nhập sai ngay từ bước nhập liệu.
  - Tầng Backend: Là chốt chặn an toàn cuối cùng, từ chối giao dịch nguyên tử và rollback nếu xảy ra xung đột đồng thời hoặc vi phạm luật xếp bãi / hạn lệnh.
- **Tính toàn vẹn dữ liệu**:
  - Domain Entity `DeliveryOrder` và `CreateDeliveryOrderCommandValidator` bảo vệ các trường bắt buộc (`CustomerTaxCode`, `CustomerName`, `OutboundVessel`, `ExpiryDate`).
  - Handler dùng `TimeProvider` và truyền UTC xuống Domain.
  - Gate-Out gọi một lần `SaveChangesAsync` sau khi mọi rule đạt, bảo đảm tính nguyên tử (atomic boundary).
  - Bổ sung Query `GetDeliveryOrderByOrderNumberQuery` hỗ trợ tra cứu DO theo số lệnh phục vụ đối soát tại cổng.
