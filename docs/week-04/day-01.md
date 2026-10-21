# Tuần 4 — Ngày 1: nền API và danh mục

Trạng thái: daily gate xanh ngày 2026-10-16; commit `01a1766`.

## Đã triển khai

- Fallback authorization, policy constants và các endpoint Identity/Container dùng handler interface.
- Tenant tài khoản lưu bền vững, JWT tenant_id, auth trước tenant middleware; header không được chọn depot khác.
- Migration AddUserTenant → mapping vận hành → RequireUserTenant; script mapping từ chối thiếu/trống/đổi assignment.
- JWT IssuedAt/NotBefore/Expires dùng cùng TimeProvider, sửa lỗi test baseline theo thời gian.
- ProblemDetails tập trung; validation body, binding/query, lỗi enum và paging; mapping 400/401/403/404/409/422/500.
- CORS allowlist, giới hạn request 1 MiB, liveness/readiness routes; dependency checks readiness thuộc mốc triển khai production tiếp theo.
- Bốn danh mục dùng chung theo lựa chọn tối ưu đã trao đổi; chỉ Admin ghi, bốn role đọc; list paging tại database.
- Bỏ secret/mật khẩu/connection credentials có sẵn trong appsettings và fallback JWT/DB. Cần cấp env khi chạy máy phát triển; hướng dẫn đầy đủ bàn giao sau.
- Thêm Api.IntegrationTests vào cả hai solution, dùng PostgreSQL Testcontainers và token thật.

## Bằng chứng kiểm thử

- Tenant middleware ban đầu 3 regression fail/1 pass; sau sửa Infrastructure Unit 28/28 xanh, sau thêm kiểm tra secret 29/29 xanh.
- Domain bắt buộc depot: 2 test đỏ trước; toàn bộ Domain 137/137 xanh sau.
- HTTP auth ban đầu 3 test đỏ; sau sửa 3/3 xanh.
- HTTP ProblemDetails ban đầu 4 test đỏ; lỗi enum query riêng 2 test đỏ trước bổ sung validation.
- Bốn route danh mục ban đầu trả 404; sau triển khai đợt HTTP 18/18 xanh. Đợt mở rộng mapping lỗi 25/25 xanh.
- Daily gate toàn solution: **354 passed, 0 failed, 0 skipped**: Domain 137, Application 142, Architecture 7, Infrastructure Unit 29, Infrastructure Integration 7, Api Integration 32. Build 0 lỗi; 211 cảnh báo (chủ yếu analyzer cancellation của test và NU1903), không coi là build không cảnh báo.
- 32 HTTP/integration tests gồm role matrix danh mục, refresh/replay, tenant isolation, enum query, ProblemDetails và migration upgrade cả trường hợp có/không có mapping.

Một lượt test enum bị lỗi MSB3027 do chạy build khi runner API trước chưa thoát; không coi đó là regression code. Đã chờ runner thoát và chạy lại tuần tự, nhận hai failure enum 200 thay vì 400 đúng nguyên nhân. Các lượt build/test cùng project từ đây chạy tuần tự.

## Lưu ý triển khai

- Danh mục dùng chung: Admin của depot ghi sẽ ảnh hưởng catalogue toàn hệ thống. Dữ liệu nghiệp vụ vẫn tenant-scoped.
- Chưa áp dụng migration lên database người dùng; chỉ DB test tự tạo. Database cũ bắt buộc có mapping user/depot.
- NU1903 từ SSH.NET gián tiếp vẫn được báo, không ẩn. UAT, production startup và backup/restore chưa thực hiện.
