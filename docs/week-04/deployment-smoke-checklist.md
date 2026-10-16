# Checklist triển khai trước packaging

Trạng thái: các mục đã pass trên project dùng một lần ngày 2026-10-16; bằng chứng ở day-04, deployment-evidence.json và RUNBOOK. Chưa rollout production thật hoặc ký UAT.

- Build image API runtime .NET 10, chạy non-root UID; runtime không chứa SDK/EF tool/source/secret.
- Compose chỉ publish API trên cổng cấu hình; PostgreSQL/Redis chỉ mạng nội bộ. Volume riêng theo project thử.
- Migration fresh hoàn tất trước API; startup thiếu JWT/DB/Admin config thất bại rõ ràng.
- Upgrade dừng tại AddUserTenant, bắt buộc CSV mapping được vận hành xác nhận, kiểm tra trước RequireUserTenant. Không downgrade migration đã áp dụng.
- Liveness anonymous 200 chỉ kiểm tra process. Readiness yêu cầu Admin; DB mất kết nối trả 503. Redis Required=true mất kết nối trả 503; optional thiếu Redis trả Degraded với 200, mô tả rõ.
- Production không mở Scalar/OpenAPI. Origin ngoài allowlist không có Access-Control-Allow-Origin; preflight origin hợp lệ hoạt động.
- Request > 1 MiB qua Kestrel trả 413 ProblemDetails; không chỉ test qua TestServer.
- Full flow HTTP xanh theo từng vai trò, không dùng Admin thay mọi vai trò.
- Backup pg_dump custom thành công rồi mới prune đúng mẫu/timestamp/retention. Không xóa backup ngoài thư mục đã xác thực.
- Restore chỉ vào database mới, tên đích phải gõ xác nhận; không ghi đè database đang có. Kiểm chứng counts, quota, tenant và history trước/sau.
- Tài liệu lệnh chạy đúng trên môi trường sạch; HTTPS qua reverse proxy/TLS và trusted forwarded headers có hướng dẫn.
