# Báo cáo FE–BE ngày 21/10/2026

Kết quả sửa lỗi review và bàn giao tiếp theo nằm trong [báo cáo 22/10](2026-10-22-integration-handoff.md).

## Kết quả

Đã chuyển toàn bộ màn Angular sang API .NET: auth/refresh/me/logout; container và chi tiết; lệnh giao/booking; validate/nhập/xuất/sửa/hủy cổng; sơ đồ bãi và bãi ảo; đối tác/owner codes; dashboard, tồn bãi, báo cáo, Excel đủ trang.

Hai nút Mở rộng bãi và Thu hẹp bãi hiển thị riêng cho Admin/YardPlanner tại block vật lý, gọi endpoint expand/shrink riêng. Kiểm thử browser chứng minh mở rộng204, thu hẹp vùng có container422 giữ form, thu hẹp hợp lệ204 và reload giữ kích thước. GateClerk chỉ đọc bãi.

## Kiểm chứng đã quan sát

| Kiểm tra | Kết quả |
| --- | --- |
| .NET SDK | 10.0.401 |
| Node / npm | v20.19.4 / 11.5.2 |
| PowerShell / Docker | 7.6.5 / 29.8.2 (Linux containers) |
| dotnet build SnpDepotManagement.sln --no-restore | 0 lỗi, 271 warning |
| dotnet test toàn solution --no-build --no-restore | 528 passed, 0 failed, 0 skipped |
| Chi tiết .NET | Domain148, Application178, Architecture8, Infra Unit34, Infra Integration30, API Integration130 |
| Angular unit suite sau gỡ demo và sửa logout | 49 passed, 0 failed; 17 file |
| Angular production build | Thành công |
| Runtime import graph | 64 module; không demo/snapshot/localStorage nghiệp vụ/alias deprecated |
| E2E API thật, lần đầu | 10 passed; 4 role, refresh TTL1phút, workflow seed35 và booking hết hạn, virtual→physical, report/Excel |
| E2E sau hồi quy hủy logout | 10 passed, gồm xác nhận hủy logout giữ phiên và chấp nhận logout xóa phiên; 1,1phút |
| Smoke Development | Seed50; 4 role login; restart2 không seed trùng, project DB riêng |
| Browser bổ sung | Container create/reload/search tổng1; order create/assign/release; gate in/edit Changes/cancel; yard expand/shrink; đối tác đổi tên→picker mới; XLSX đủ nhóm và lượt |
| Postman | Generator sinh90 requests; environment không chứa secret thật; không tuyên bố đã chạy Newman toàn collection trong phiên này |

Warnings build gồm nullable/xUnit hiện có và NU1903 từ SSH.NET transitive trong test tooling. Không nâng dependency ngoài phạm vi tích hợp. Full PostgreSQL/HTTP suite chạy thật, không skip để báo xanh.

Lượt E2E dùng project snp-integration-e2e với API port riêng, seed mới và JWT access token1phút qua override chỉ cho test. Frontend14202 riêng; không mock API nghiệp vụ, không hạ policy hoặc limiter. Credential/proxy/env/log/trace nằm trong vùng ignored.

## Seed và dữ liệu

Marker DemoSeedRun theo tenant/version; bootstrap/demo có transaction và advisory lock, kiểm tra username/role/tenant, không ghi đè dữ liệu cũ. Chỉ Development và cờ Seed:DemoData=true. Dataset fe-be-v1:50 container,5 lệnh,3 khách; ban đầu20 trong bãi (15physical+5virtual),3đã xuất,27chưa nhập. Timestamp/lifecycle/gate/movement khớp nhau. Các browser workflow đã thay đổi stack smoke; không dùng số50 làm kỳ vọng sau khi người dùng ghi nghiệp vụ.

Luồng container35–36 phải nhập bãi trước rồi mới gán DO-2026-002 và xuất. Không sửa BE để cho phép gán container NeverIn. API booking mới trả ID thật và counts theo từng item; FE không tự trừ quota. Chủ container alias được ánh xạ thành hãng khi gate-in để báo cáo không mất dữ liệu.

## Tài liệu giữ và dọn

Giữ toàn bộ day-*.md/báo cáo ngày của các tuần; API/role matrix, cấu hình, runbook, backup/deploy/UAT, yêu cầu nghiệp vụ UI/BE, thiết kế và plan đang thực thi. Những Markdown bị ignore trong checkout gốc được bổ sung có chọn lọc vào bản bàn giao; không force-add env/log/backup.

Xóa ba tài liệu đã đọc và hợp nhất:
- docs/HUONG-DAN.md: hướng dẫn cổng18084 và env/smoke cũ; thay bằng README gốc + Postman/config/runbook.
- docs/week-04/RUN-AND-TEST-GUIDE.md: trùng quick-start/native/test, có số415/85 cũ; thay README gốc và tài liệu vận hành.
- docs/design/backend-viec-can-lam-truoc-angular.md: checklist tiền Angular đã hết hiệu lực, mô tả nhiều API là chưa có; giữ yêu cầu nghiệp vụ nguồn và báo cáo hiện tại.

Di chuyển frontend/docs/demo-verification.md sang docs/reports/2026-10-20-angular-demo.md. Giữ nguyên kết quả lịch sử. Link tới tài liệu thay thế được sửa trong cả checkout gốc và worktree.

Backup trước xóa: D:/SnpDepotManagement/backups/docs-cleanup-20261021-185758.zip và manifest SHA256. Sáu entry bảo toàn cả biến thể original/worktree của checklist và báo cáo; hash guard đã dừng khi phát hiện khác byte, chỉ xóa sau khi bổ sung và kiểm hash bản tương ứng. Không xóa theo tuổi hoặc xóa nguyên thư mục.

## Giới hạn và vận hành

- FE không còn lưu snapshot nghiệp vụ; token ở sessionStorage. Storage bị chặn có cảnh báo phiên chỉ trong memory.
- Refresh single-flight, rotate nguyên tử; logout/đổi phiên hủy refresh cũ. Hủy xác nhận rời phiếu cổng giữ nguyên phiên và input.
- Dashboard lấy summary, không đếm trang container. Top lưu lâu được tính sau khi đọc đủ các trang hiện có.
- Export100dòng/trang, dừng nếu tổng/trang lỗi hoặc hủy; không tải workbook dở. Dữ liệu vẫn có thể thay đổi đồng thời mà tổng không đổi, nên không hứa snapshot transaction.
- Một số báo cáo BE có cacheTTL; reload UI không bỏ qua TTL. Tên khách trên lệnh cũ theo DTO snapshot của BE.
- Relocate API hiện tại là cùng block; phân vị trí từ virtual/unplaced có thao tác riêng.
- Kiểm thử tự động/local không thay UAT/production deployment. Chưa push, merge hoặc publish.

## Phục hồi và phạm vi checkout

Tag demo-offline-2026-10-21 trỏ commit6ac340aa173cde3ac80474eeec9bbc08460d06b9; đã kiểm git object của DemoRepository trước khi bỏ20file demo/legacy E2E. Khôi phục demo bằng checkout riêng từ tag, không trộn browser snapshot cũ vào DB.

Mã tích hợp được triển khai trong managed worktree fe-be-integration. Checkout gốc giữ trạng thái code của nó; các thay đổi dọn tài liệu ignored đã áp dụng theo yêu cầu và có ZIP phục hồi. Không sửa service/volume của project khác.

## Quyết định khi triển khai

Các quyết định dưới đây được giữ nguyên văn từ ledger; kết quả review cuối được bổ sung trước bàn giao.

- Ruling: Accepted implementation authorization selects the previously recommended Native method; no per-task implementer delegation.
- Ruling: FE baseline failed at CSS font resolution because node_modules was a cross-drive junction; install exact lockfile dependencies locally, no product/config workaround.
- Ruling: Baseline broad API run and concurrent targeted retry locked the same test executable; stopped only obsolete test hosts in this worktree and now run .NET tests serially. Unit/infrastructure baseline results retained; API baseline not established.
- Task 1: preserved demo-offline-2026-10-21 at 6ac340a. Configuration updated while old compose smoke builds its captured baseline. Ruling: PowerShell smoke requires pwsh 7.2, not Windows PowerShell 5; plan command adapted.
- Task 1: HTTP RED 307 -> GREEN 2/2; old compose smoke RED DB auth uses hardcoded postgres; new compose smoke running. FE baseline with local dependencies GREEN 29/29. Task 2: schema RED 3/3 missing table. Ruling: prepare independent Task 2 schema while Task 1 Docker smoke runs, without claiming Task 1 complete; raw SQL tests validate actual migration constraints before entity exists.
- Task 3: Ruling: move enabled seed assertions from InMemory unit test to PostgreSQL dataset test; historical SaveChanges clock and transaction cannot be proved with InMemory. Disabled seeder unit test retained; no production-only test branch.
- Task 4: Ruling: .NET 10 selected ReadOnlySpan.Contains for array captures, EF9 expression evaluation failed. Use IReadOnlyList<string> manifest collections to keep Enumerable.Contains; no dependency upgrade. Strengthened negative tests to assert intended account/marker errors, not arbitrary InvalidOperationException.
- Task 4: Ruling: hoist dotnet-ef installation into an independent Docker stage because copying changed source invalidated tool install on every rebuild; version remains 10.0.12. Full demo accounts use globally unique fixed usernames, so a second tenant with those usernames must fail rather than silently take over accounts; global master reuse remains tenant-explicit in dataset writer.
- Task 4: startup/dataset GREEN 10/10 after IReadOnlyList fix; Docker DemoData smoke GREEN with 50 containers, all 4 role logins and 2 API restarts. Task 5: original filter RED 5/5; fixed filter + summary GREEN 8/8 in combined run. Task 6: RED bookings route 404, search returned 7 rather than 1, 429 code Request.Failed rather than RateLimit.TooManyRequests. Ruling: order detail DTO default status is empty; make ID/number detail use same domain display status as list.
- Task 6: alias OpenAPI test RED (legacy customers not deprecated); introduced metadata transformer and preserved aliases. Ruling: normalize list status date bounds to the existing domain GetDisplayStatus (expiry priority and four-day exclusive boundary), and expose status on both detail routes; no new FE expiry rules.
- Task 6: Ruling: HTTP regression proved stale release returned 500 and alias Gate-In disappeared from operator inventory (0 instead of 1). Map existing Booking.NotReserved to 409 and persist resolved tenant owner mapping in both Gate-In paths. Postman had unsupported OwnerCode JSON and fixed TST prefix; generate valid owner prefixes and explicitly map them to the test operator. Cost if wrong: reports group under fallback owner until mapping corrected.
- Task 8: RED missing modules -> GREEN 13/13 auth/session/policy tests; production build GREEN. Auth raw login/refresh use HttpBackend; raw logout captures old bearer so clear is immediate even if revoke fails. Ruling: ownerCodes wire response is string[] (BE GetOwnerCodes), not object DTO[] implied by plan; Task 9 will preserve exact wire shape.
- Task 9: Ruling: master lists lack search required by form design; append optional Search to existing queries and filter before count/page. Initial test passed with only one entity; added distractors and observed RED total2 instead1. Add HTTP regression. Cost if wrong: extra optional query parameter, existing callers unchanged. Owner-code DTO is string. Shell routes temporarily use TransitionPage until Tasks10-15 migrate each screen; no demo mutations reachable.
- Task 10: GREEN mapper/facade6/6; production build GREEN; rebuilt Docker smoke login/query/restart50 GREEN; browser register/reload/search total1/detail/history PASS. Test created one additional ZQX container in isolated smoke DB (now51); future seed smoke cannot expect exact50 after mutable workflow. Ruling: logout navigates immediately after local clear rather than waiting for revoke network; raw revoke still subscribed. Early ReportsApi history client added as planned. Task11 RED missing facade -> GREEN4/4 while Docker build ran; UI pending.
- Task 11: GREEN facade4/4, production build GREEN after typed master-picker union fix; browser create/assign real in-yard container/release/reload PASS. Approval quota interrupted UI write before execution; resumed at18:20, no bypass. Task 13: Ruling: prioritize yard before Task12 per user observation of missing expand/shrink; YardApi now created in13 and consumed by12 rather than reverse. Separate explicit buttons, domain rejection displayed; same dependencies, no lost gate scope.
- Task 15: RED missing modules -> GREEN7/7 report+export+XLSX tests; production build GREEN. Browser YardPlanner summary/inventory equals live API, complete inventory and dwell XLSX row counts, no expiring/gate calls PASS. First export HMR reload during initial ExcelJS optimization prevented download; stopped only exact report-browser test process and reran warmed dev server with timeout. Ruling: Task14 completion gate initially hit unresolved Task15 test imports because Angular compiles all spec types even filtered; fixed missing planned modules then reran14 GREEN; do not overlap future missing-module spec with other completion gates. Docker build context includes frontend node_modules unnecessarily; exclude frontend (API image only) in Task16.
- Task16: final E2E GREEN10/10 including unsaved-gate logout dismissal/acceptance; frontend full49/49; final production build GREEN and graph64 PASS. Backend unchanged since full528/528. Ruling: preserve ignored execution workspace at handoff because live test stack credentials/proxy/config reside there; deleting it would lose access to seeded DBs and break E2E reproduction. Cost is retained local scratch/log disk space, not an application runtime dependency.
- Task 17: Ruling: completion base corrected to code HEAD582cc68 because documentation preparation overlapped Task16; retained measured performance-evidence outputs from the full integration run. Cost if wrong: documentation range includes benchmark evidence noise; product code unchanged.
