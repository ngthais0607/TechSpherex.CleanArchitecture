# Ngày 3 — Báo cáo và ngày nghiệp vụ

Trạng thái: daily gate xanh ngày 2026-10-16; benchmark đủ bốn báo cáo đạt ngưỡng.

Bốn API báo cáo dùng handler Application và truy vấn SQL có phân trang:

| Route GET | Nội dung / bộ lọc |
|---|---|
| `/api/reports/yard-inventory` | `lineOperator`, `size`, `type`, `condition`, `blockId`; nhóm theo hãng, kích cỡ, loại, tình trạng, block |
| `/api/reports/dwell-times` | `lineOperator`, `minDays`, `includeGatedOut` (mặc định false); một dòng cho mỗi lượt vào depot |
| `/api/reports/containers/{number}/history` | Timeline tăng dần theo thời gian, nguồn, Id và loại sự kiện |
| `/api/reports/expiring-orders` | `days` 0–365, mặc định 7; `lineOperator`; chỉ DO còn quota và chưa hết hạn |

Tất cả có `pageIndex` từ 1, `pageSize` 1–100. Tồn bãi trả `groups` có phân trang và `totalContainers` trước phân trang. Container 40 feet được đếm một lần. Container còn trong depot đã Remove nhưng chưa Gate-Out vẫn thuộc nhóm `blockId=null` (Unplaced). Vị trí lấy từ occupancy thực, không suy đoán từ liên kết cũ có thể chưa đồng bộ.

Ngày nghiệp vụ là Asia/Ho_Chi_Minh (UTC+7). Timestamp API/database là UTC. Dwell là chênh lệch ngày địa phương, cùng ngày bằng 0; lượt đã xuất dùng OutDate, mỗi lần tái nhập là một lượt riêng. Mỗi lần query lấy một mốc asOf từ TimeProvider để count và page cùng dùng một biên ngày.

DO hết hạn ngày 15 bị loại từ 00:00 ngày 16 địa phương, ngay cả khi UTC vẫn là ngày 15. Report và Gate-Out dùng lịch chung trong Domain; Domain không phụ thuộc Application. Query expiry chuyển biên ngày thành khoảng UTC `[đầu hôm nay, đầu ngày sau ngày cuối]`, so sánh tại SQL. Không lọc bằng thuộc tính RemainingQuantity không được EF mapping.

Lượt version 0 trả mốc Gate-In/Gate-Out thực có trong lifecycle và movement mới ghi sau rollout. Lượt version 1 chỉ lấy movement. Không tạo movement giả cho quá khứ. `eventId` được dựng ổn định từ source, Id và loại sự kiện; các mốc legacy không có snapshot/actor được trả null. SQL UNION, sort và paging chạy trước bước dựng DTO snapshot, không kéo cả timeline về RAM.

Kiểm chứng ban đầu: hai report đầu RED 404 → 2/2 xanh; history/expiry RED 404 → tổng 6 test HTTP report xanh; regression ngày địa phương RED → 4 test Domain rule xanh. Test cover legacy đang mở/đã xuất, movement mới cùng timestamp, phân trang ổn định, lượt tái nhập, không có lượt, tenant khác, quota rỗng và biên days 0/7.

Benchmark tạo 20.000 container trên PostgreSQL riêng, hai tenant, 20/40 feet, lượt cũ/mới, có vị trí/Unplaced. Không dùng database người dùng. SQL seed có chốt database + cờ enable; test tạo container DB riêng và tự hủy. SQL thực, EXPLAIN ANALYZE BUFFERS và timings lưu tại [performance-evidence](performance-evidence/); phân tích tại [report-performance.md](report-performance.md). Lần daily gate có p95 cao nhất khoảng 155 ms. Giữ index hiện có vì chưa có bằng chứng cần index mới; không tạo migration giả.

Daily gate: **397 passed, 0 failed, 0 skipped** (Domain 140, Application 142, Architecture 7, Infrastructure Unit 29, Infrastructure Integration 17, API Integration 62). Build 0 lỗi, 205 cảnh báo. Log: `day3-build.log`, `day3-all-tests.log`. Benchmark tự assert ngưỡng p95 < 1 giây và SQL/request cố định. UAT và kiểm chứng deployment chưa thuộc mốc này.
