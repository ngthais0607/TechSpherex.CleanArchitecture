# Kết quả kiểm tra demo Angular

Ngày 20/10/2026. Phạm vi frontend local demo, không thay backend.

## Kết quả cuối

| Lệnh | Kết quả đã quan sát |
| --- | --- |
| `npm.cmd test` | 29 passed, 0 failed; 6 file test |
| `npm.cmd run e2e` | 11 passed, 0 failed; Chromium, 1 worker |
| `npm.cmd run build` | Thành công; initial bundle 347,30 kB, ước tính truyền 97,00 kB |
| `npm.cmd ci --dry-run --ignore-scripts --no-audit --no-fund` | Thành công; chỉ kiểm tra kế hoạch cài dependency, không phải cài sạch thực tế |
| `git diff --check` | Không có lỗi khoảng trắng |

ExcelJS được tách sang lazy chunk, không nằm trong initial bundle. Không chạy lại suite .NET vì không thay mã backend.

## Chức năng đã kiểm tra

- Điều hướng đủ màn Tổng quan, Container, Lệnh giao, Cổng, Sơ đồ bãi, Khách hàng/Hãng, Báo cáo, Tồn bãi; đăng nhập sai, đăng xuất và quay lại route.
- Tạo container hợp lệ → nhập block ảo → tạo lệnh → gán → di chuyển sang block thật → xuất → reload → hủy xuất → khôi phục đúng vị trí và booking → tải Excel.
- Kiểm tra checksum, gán batch nguyên tử, quota đặt chỗ, xếp 20ft/40ft, sức đỡ, thu hẹp block, thời điểm hết hạn UTC+7, mốc lưu 72 giờ và phạm vi ngày báo cáo.
- Excel readback xác nhận số đúng kiểu và chuỗi `=1+1` không bị đổi thành công thức.
- Snapshot có quan hệ sai hoặc schema khác bị từ chối; transaction thất bại không ghi dở; quota lưu trữ có cảnh báo, dữ liệu vẫn còn trong RAM; bản lưu hỏng không bị ghi đè trước khi người dùng xác nhận khôi phục.
- Form giữ focus, Esc và trả focus, footer luôn trong vùng nhìn; không cuộn ngang toàn trang; tên khách tiếng Việt dài vẫn dùng được.
- Viewport 1440×900, 1366×768, 1024×768, 768×1024. Có kiểm tra viewport nội dung 1093×614 tương đương vùng CSS khi màn 1366×768 zoom 125%; đây là mô phỏng vùng nội dung, không thao tác trình đơn zoom của trình duyệt.
- Dữ liệu trống, lỗi đọc mô phỏng, Thử lại và khôi phục mẫu.

Ảnh minh chứng tại `screenshots/`. Đã trực tiếp mở và quan sát ảnh Tổng quan, form container, sơ đồ bãi và form ở viewport tương đương 125%.

## Review và sửa hồi quy

Một reviewer độc lập đã đọc source và trả 5 vấn đề Important, không có Critical hoặc Minor để hoãn. Reviewer không chạy test; các kết quả trên do tác nhân triển khai chạy và đọc đầu ra.

1. Hủy nhập sau khi gỡ booking: giữ lifecycle có dấu hủy để lịch sử không bị mồ côi; mọi truy vấn tồn và sức chứa loại lifecycle hủy.
2. Hủy nhập container đang đỡ container khác: bị chặn bởi quy tắc sức đỡ.
3. Sửa giao dịch cũ rồi hủy xuất: thuộc tính hiện tại lấy từ các sự kiện còn hiệu lực, không dùng giá trị chụp cũ.
4. Rời form cổng hoặc đăng xuất: xác nhận trước khi bỏ nội dung chưa ghi; hủy xác nhận giữ nguyên form và phiên.
5. Chỉ sửa các dòng số lượng của lệnh: vẫn tính là thay đổi chưa lưu khi X/Esc/Hủy.

Tự rà soát còn sửa hai điểm: nút đóng footer dùng chung quy trình xác nhận với X/Esc; bản lưu hỏng bị khóa thao tác ghi cho đến khi khôi phục rõ ràng. Các lỗi trên có bài kiểm tra tái hiện thất bại trước khi sửa và đã chuyển sang pass.

## Quyết định triển khai

| Quyết định | Lý do và giới hạn |
| --- | --- |
| Giữ checkout/nhánh `Thai-DMS-week4` được cung cấp | Không tự đổi checkout hoặc tạo worktree; các file mới nằm trong frontend. Người dùng tự quyết định merge/push sau. |
| Gộp phần đầu vào một commit tích hợp | Tránh các mốc app có lazy route trỏ đến trang chưa tồn tại; lịch sử không chia nhỏ đúng từng task của kế hoạch. |
| Domain command có kiểu qua signal repository | Giảm lớp trung gian cho demo local. Không tạo generic QueryMap facade; khi nối backend cần đối chiếu DTO và thay lớp truy cập dữ liệu. |
| Dùng native HTML dialog | Đã kiểm tra focus/Esc/restore trong Chromium; yêu cầu trình duyệt hiện đại. |
| Dùng dữ liệu local, thêm chế độ trống | Đúng lựa chọn demo. Seed backend chưa tự hiện trên UI; chưa có xác thực/phân quyền thật hoặc chứng nhận nghiệp vụ production. |

Review không đánh giá lại các quyết định kiến trúc đã ghi nhận, quy tắc production backend hay xác thực thật vì ngoài phạm vi. Các quyết định này được giữ; không có phát hiện Minor bị hoãn. Không push, merge hoặc publish.

## Cập nhật bảng màu theo ảnh mẫu

Áp dụng sidebar navy `#10253b`, mục chọn navy sáng hơn với vạch cyan, nền nội dung `#f4f8fc`, nút chính teal `#007f9b`, biểu đồ cyan/indigo và nhãn xu hướng xanh nhạt. Chỉ đổi style, giữ bố cục và nghiệp vụ.

- Production build mới thành công: initial bundle 347,95 kB (ước tính truyền 97,21 kB).
- 3 kiểm tra Chromium liên quan đều pass: điều hướng toàn bộ trang/không tràn viewport, form tablet, dialog dài với focus/footer ở bốn viewport. Không chạy lại unit test nghiệp vụ trong lượt chỉ đổi màu này.
- Tương phản đo trên các cặp màu: chữ trắng/nút chính 4,66:1; chữ menu/navy 10,13:1; chữ phụ/navy 7,26:1.
- Ảnh Tổng quan, Cổng, Sơ đồ bãi, Tồn bãi và bốn ảnh form được chụp lại. Đã mở ảnh Tổng quan để kiểm tra trực quan sidebar và bảng màu mới.
