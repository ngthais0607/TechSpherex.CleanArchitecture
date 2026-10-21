# Hiệu năng báo cáo tuần 4

Đo trên PostgreSQL 17-alpine trong Testcontainers riêng, 20.000 container và hai tenant. HTTP chạy bằng WebApplicationFactory trong process; kết quả không bao gồm Internet/TLS/reverse proxy.

Môi trường: Microsoft Windows NT 10.0.26300.0; 8 logical processors; 15.75 GiB RAM khả dụng cho .NET; runtime 10.0.12. Thời điểm UTC: 2026-10-16T13:54:48.2179524Z.

Mỗi trường hợp warm-up 5 lượt, đo 30 lượt tuần tự. p95 lấy phần tử ceil(30 × 0,95) sau sort. Ngưỡng nghiệm thu: dưới 1.000 ms.

| Trường hợp | p95 HTTP (ms) | SQL/request |
|---|---:|---:|
| inventory-all | 416.28 | 3 |
| inventory-selective | 303.37 | 3 |
| dwell-all | 334.80 | 2 |
| dwell-selective | 59.72 | 2 |
| history-first | 28.49 | 3 |
| history-second | 30.88 | 3 |
| expiry-week | 14.90 | 2 |
| expiry-today | 12.85 | 2 |

Tất cả trường hợp đạt ngưỡng. Query count được assert ở mọi lượt: inventory 3, dwell 2, history 3 (gồm lookup container), expiry 2; không tăng theo số dòng DTO.

Đã chạy ANALYZE, sau đó EXPLAIN (ANALYZE, BUFFERS) cho từng SQL count/page thực do EF sinh, bind đúng parameters. Không disable sequential scan. [Raw timings](performance-evidence/timings.json) và các file .txt trong performance-evidence lưu SQL, tham số dữ liệu giả và plan.

Quyết định index: giữ các index hiện hữu. Plan tồn bãi chọn scan cho các tập có độ chọn lọc thấp (một tenant chiếm 50%, active chiếm 75%) và bitmap index tenant cho slot; history dùng index lookup, expiry xử lý tập DO nhỏ. Mức đo hiện tại đạt ngưỡng; chưa có bằng chứng cần thêm index nên không tạo migration AddReportIndexes chỉ để đủ tên task. Cần đo lại với quy mô và tải đồng thời production trước khi đặt SLA vận hành.

Lệnh tái hiện từ root repository:

```powershell
dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~ReportPerformanceTests
```

Test tạo PostgreSQL dùng riêng và tự hủy khi xong, không lấy connection string người dùng. Seed SQL chỉ chấp nhận database snp_api_test với cờ snp.allow_report_benchmark=true. Không chạy seed ở production.

Daily gate cũng chạy benchmark nên evidence có thể được cập nhật bằng lần đo mới; file timings.json là nguồn số liệu chính xác của lần gần nhất.
