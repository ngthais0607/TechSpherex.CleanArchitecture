# Audit tuần 4

Một reviewer độc lập đã đọc phạm vi `e81f90e..5e54dd5` và các thay đổi tuần 4 đang chuẩn bị; không sửa code hoặc tự chạy test. Người triển khai xử lý findings trong một lượt sửa và tự kiểm chứng bằng PostgreSQL thật. UAT của người dùng vẫn **Awaiting**.

## Findings và cách xử lý

| Finding | Sửa / bằng chứng regression |
|---|---|
| Place tầng trên và Gate-Out chân đỡ cùng ghi thành công | RED: cả hai 204. YardBlock dùng xmin; mọi thay đổi slot chạm parent trong cùng transaction. Gate-Out đọc parent/occupancy trước khi xét chân đỡ. GREEN: một 204/một 409, không còn container mất đỡ, chỉ một movement. |
| Hai Expand trên các chiều khác nhau ghi đè kích thước | RED: cả hai 204. Token parent chặn lost update; GREEN: một 204/một 409, slot count và tọa độ khớp kích thước. |
| Xuất quota cuối ở hai DO item khác nhau không hoàn tất DO | RED: cả hai 204 từ snapshot cũ. DO dùng xmin và parent update khi item thay đổi. GREEN: một 204/một 409; reload/retry loser thành công, quota đều 0 và IsCompleted=true. |
| Legacy version0 xuất sau rollout bị đếm Gate-Out hai lần | RED: totalCount=3 thay vì 2. SQL bỏ fallback gate event nếu đã có movement cùng lifecycle/type. GREEN: chỉ một Gate-Out nguồn Movement, vẫn giữ Gate-In legacy. |
| Inventory dùng condition đăng ký thay vì lượt nhập | RED: Normal thay vì Damaged. SQL dùng lifecycle.Condition. GREEN: nhóm/filter theo tình trạng lượt lưu bãi. |
| Tax code14 ký tự qua validator rồi lỗi DB500 | Regrade Important: vi phạm hợp đồng validation400. Giới hạn13 phù hợp varchar13. RED500 → GREEN400 ở14 ký tự;13 vẫn201. |
| Register trả Location tới GET route không có | Regression độc lập RED404 khi theo Location. Trả201 JSON với userId, bỏ Location không có resource route; GREEN. |

Bộ regression audit, history cũ và endpoint metadata: **14/14 passed**. Toàn solution và image sau sửa được ghi ở [final-verification.md](final-verification.md), không suy từ kết quả test trước sửa.

## Quyết định triển khai

Dùng optimistic concurrency ở aggregate thay vì mở transaction serializable cho toàn HTTP request. Update parent, slot, lifecycle, quota và movement dùng cùng SaveChanges transaction; xung đột rollback toàn bộ và trả409. Đổi lại, hai thao tác độc lập trong cùng block/DO cũng có thể xung đột; client cần reload rồi retry, không tự lặp request vô điều kiện.

Migration `ProtectAggregateConcurrency` bổ sung mapping xmin cho YardBlock/DeliveryOrder; PostgreSQL cung cấp system column này. Snapshot/migration được cập nhật cùng model. Upgrade drill so khớp danh sách migration thật với source, thay số5 hardcode cũ.

## Minor còn lại

- Bốn POST master data có Location tới `/{id}` chưa có GET tương ứng. Có thể dùng GET danh sách để xem dữ liệu tạo; không coi URL đó là API đọc hiện hữu. Đề xuất bỏ Location hoặc thiết kế route đọc trong đợt sau.
- `Jwt__RefreshTokenExpirationDays` chưa được Login/Refresh handler sử dụng; hiện hạn refresh cố định7 ngày. Guide đã sửa để không hướng dẫn cấu hình không có tác dụng; chưa thay đổi hành vi expiry.

Known tooling risks: SSH.NET2024.1.0 transitive từ Testcontainers có NU1903, chỉ ở test tooling; Newman6.2.3 chỉ dùng collection v2.1 cục bộ và không còn maintained theo tài liệu Postman. Không đóng gói hai công cụ này vào API runtime.
