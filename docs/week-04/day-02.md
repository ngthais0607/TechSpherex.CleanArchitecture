# Ngày 2 — API nghiệp vụ và lịch sử nguyên tử

Trạng thái: daily gate xanh ngày 2026-10-16, commit theo mốc ngày 2.

Minimal APIs cho block, vị trí, lệnh giao hàng, Gate-In/Gate-Out và container gọi trực tiếp interface handler Application. Ma trận quyền và danh sách route ở [endpoint-role-matrix.md](endpoint-role-matrix.md). Endpoint không truy cập DbContext.

Mỗi thao tác thành công ghi một `ContainerMovement` cùng transaction với lifecycle/vị trí/quota. Snapshot vị trí lưu mã block và tọa độ tại thời điểm phát sinh, actor từ principal HTTP, UTC từ TimeProvider. Movement và snapshot đã lưu không được sửa/xóa qua AppDbContext. Container 40 feet chiếm hai slot nhưng chỉ sinh một event, bay logic là bay chẵn; `CurrentSlotId` trỏ slot vật lý thấp hơn. Remove đưa liên kết về null, Relocate cập nhật liên kết và Gate-Out giải phóng vị trí.

Migration `AddContainerMovementHistory` giữ lượt cũ ở version 0. Gate-In mới tạo version 1. Không dựng lịch sử chuyển/gỡ không có dữ liệu; báo cáo ngày 3 phải kết hợp mốc vào/ra thực của lượt cũ với các movement mới mà không đếm đôi.

Các lỗi đã có test PostgreSQL chứng minh trước khi sửa:

- Gate-In 40 feet chỉ chiếm một slot; Place thiếu lượt active; Remove/Relocate giữ liên kết cũ.
- Theo xác nhận của người dùng: không ghép hai container 20 feet thành một container 40 feet khi thao tác bay chẵn; không xuất container khi còn container phía trên. Thất bại giữ nguyên quota/lịch sử.
- HTTP enum sai bên trong Items của DO từng trả 201; bổ sung validator enum con, trả 400.
- Expand từng trả concurrency 409 dù không có cạnh tranh vì slot mới mang Guid có sẵn bị EF nhận là update; handler đăng ký insert các slot mới rõ ràng.

FindFreeSlots lọc, đếm, sắp xếp và phân trang tại database; phép ghép slot 40 feet đã chạy qua PostgreSQL. Layout giữ snapshot một block. Danh sách container/DO thêm Id để sắp xếp ổn định khi các khóa chính bằng nhau.

Hai race HTTP dùng JWT thật, hai request scope và PostgreSQL xmin: tranh một vị trí Place; tranh quota cuối Gate-Out. Barrier giữ hai SaveChanges trước commit, kiểm tra một 204/một 409 và dữ liệu sau rollback chỉ có một movement. Fixture không mock handler hoặc xác thực.

Daily gate: build toàn solution 0 lỗi, 215 cảnh báo; **376 passed, 0 failed, 0 skipped** (Domain 139, Application 142, Architecture 7, Infrastructure Unit 29, Infrastructure Integration 17, API Integration 42). Log cục bộ: `day2-build.log`, `day2-all-tests.log`. Cảnh báo SSH.NET gián tiếp và analyzer test vẫn được báo.
