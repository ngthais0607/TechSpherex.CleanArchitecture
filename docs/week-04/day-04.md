# Ngày 4 — Full flow và triển khai thử

Trạng thái: full flow, deployment/restore/upgrade và daily gate đã xanh ngày 2026-10-16.

FullFlowTests chỉ dùng HTTP và token thật: Admin tạo danh mục/block/user; OrderClerk tạo DO quota 3; GateClerk đăng ký/Gate-In bốn container; YardPlanner Remove→Place→Relocate; GateClerk xuất ba container. Quota 3→2→1→0, inventory 4→1; xuất container thứ tư bị chặn 422, xuất lại bị chặn 409. Timeline container đầu có đủ năm sự kiện. Không mock auth/handler và không gọi Place ngay sau Gate-In.

Docker multi-stage có API runtime .NET 10, migrator EF bundle riêng. API chạy UID **1654**, không chứa SDK, EF bundle hoặc source folder. Chỉ API publish `127.0.0.1:18084` trong môi trường thử; DB/Redis không có HostConfig.PortBindings. [Deployment evidence](deployment-evidence.json) đã lọc secret.

Các kiểm tra thực qua Kestrel/Docker đã pass:

- Liveness 200 anonymous; readiness Admin 200 khi dependency tốt, 503 khi DB/Redis bắt buộc bị dừng; liveness vẫn 200 trong outage. Dependency khởi động lại phục hồi readiness.
- Redis optional không cấu hình trả Degraded/200 trong HTTP integration test; required thiếu Redis trả 503.
- CORS allowlist/preflight đúng; origin ngoài danh sách không có allow header.
- Body >1 MiB trả 413 ProblemDetails. Production không mở Scalar/OpenAPI.
- Thiếu JWT secret không startup; example JWT placeholder cũng bị chặn ở image mới nhất. Guard DB rỗng thất bại sớm. EF design-time factory yêu cầu env, không hardcode credential.
- `ReverseProxy__TrustedProxy` chỉ nhận một IP hợp lệ, một hop forwarded headers; bỏ trống thì không đọc forwarded headers.

Backup/restore thực trên 20.000 container hai tenant: pg_dump custom, restore sang database mới `snp_restore_drill`; counts, quota và timeline source/target có cùng SHA-256 **180D730CAD6A6E4486E6D6D7E90D320D541B611C2B2E083A8CD4605E13B6BAF8**. Restore vào nguồn/đích đã có bị từ chối. Retention 2 giữ đúng hai dump, giữ file không liên quan; dump lỗi không prune backup cũ. Secret thử chỉ nằm trong file ignored cục bộ.

Upgrade drill tạo DB riêng từ schema tuần 3 và một user giả bị disable. Thiếu mapping hoặc mapping user lạ đều bị chặn trước contract. Mapping đúng đưa user về DEPOT-B, áp dụng đủ 5 migration, chạy lại không downgrade. DB thử thành công: `snp_upgrade_retry`; không sửa DB developer/production. Các lần lỗi ở script test được giữ trong log: nhầm `-c` EF CLI thành configuration (đúng là `--configuration`), tên cột migration history thực là `migration_id`, PowerShell trả body ProblemDetails dạng byte[] và Compose port không publish trả `:0`. Đã xác định nguyên nhân, sửa và chạy lại; không coi các lỗi harness này là kiểm thử sản phẩm xanh.

Daily gate: **406 passed, 0 failed, 0 skipped**: Domain 140, Application 142, Architecture 7, Infrastructure Unit 33, Infrastructure Integration 17, API Integration 67. Build 0 lỗi, 186 cảnh báo. Log `day4-build.log`, `day4-all-tests.log`. Runtime Docker mới nhất cũng đã build/start lại sau guard/Location fixes.

Hướng dẫn vận hành: [RUNBOOK](../RUNBOOK.md). Script smoke chỉ tạo project dùng một lần; API thử đang được giữ để chạy Postman/UAT. Không push Git/image hoặc triển khai production. UAT của người dùng chưa thực hiện.
