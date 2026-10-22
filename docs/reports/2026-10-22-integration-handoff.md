# Bàn giao tích hợp FE–BE ngày 22/10/2026

Tiếp nối [báo cáo triển khai 21/10](2026-10-21-fe-be-integration.md). Mã tích hợp ở managed worktree `fe-be-integration`; checkout gốc chưa được merge. Hướng dẫn chạy nằm trong [README](../../README.md).

## Review và sửa lỗi

Reviewer độc lập đọc toàn nhánh `6ac340a..e1108d1`, không thay đổi checkout. Sáu phát hiện được xử lý trong một đợt sửa, gồm việc nâng mức lỗi trạng thái container từ Minor lên Important vì ảnh hưởng trực tiếp đến filter hợp lệ.

| Phát hiện | Bản sửa và hồi quy |
| --- | --- |
| Đổi block có thể dỡ/di chuyển nhầm container | Xóa layout/selection cũ ngay, khóa ghi đến khi layout khớp block; hủy tra cứu container khi đổi context; tọa độ gắn với layout đang hiển thị. Hai test yard đã quan sát RED trước khi GREEN. |
| Request cũ có thể retry dưới tài khoản mới | Hủy toàn pipeline HTTP khi phiên đổi; kiểm session version trước retry 401. Test request A đang chờ rồi logout/chuyển B đã RED→GREEN. |
| Refresh bị từ chối nhưng không về login | Phát sự kiện hết phiên, chuyển login cùng returnUrl nội bộ; hết phiên cho rời phiếu cổng, còn hủy logout chủ động vẫn giữ phiên. Router regression RED→GREEN. |
| Lỗi tạm thời khi khôi phục phiên thiếu nút retry | Login hiển thị lỗi khôi phục và nút Thử lại phiên, giữ token, gọi lại me, trở về URL cũ mà không nhập mật khẩu. Router/HTTP regression RED→GREEN. |
| Search cũ vẫn trả trong khoảng debounce | Hủy request ngay khi input đổi; đặt timer300ms bên trong switchMap. Áp dụng cho container, lệnh/candidate, gate, partner, report và master picker. Test kiểm cancellation ngay trước khi advance timer RED→GREEN. |
| Out không khớp OutOfYard của API | Chuẩn hóa mã filter và nhãn Đã xuất về OutOfYard. Mapper/label regression RED→GREEN. |

Nhóm hồi quy ban đầu: 7 failed/26 passed đúng các hành vi cần sửa; sau sửa 33/33. Test bổ sung tra cứu placement đã quan sát fail khi bỏ cancellation, rồi khôi phục bản sửa.

## Kiểm chứng cuối

- Angular toàn bộ: **56 passed**, 18 file; production build thành công.
- Backend: **528 passed**, 0 failed/0 skipped ở lượt toàn solution ngày21; không sửa mã backend trong đợt review cuối.
- E2E API thật sau review: **10 passed**, 2,5 phút, ngày22/10; bốn role, refresh token thật, nhập→gán→xuất, booking hết hạn và virtual→physical.
- Runtime import graph: **65 module PASS**. Liên kết Markdown/heading: **68 file PASS**; git diff --check PASS.
- Không có minor bị hoãn; giới hạn được reviewer nêu riêng được giữ cùng lý do và chi phí trong danh sách quyết định dưới đây.

## Docker và tài liệu

Các stack test được tách DB để seed/E2E không sửa dữ liệu của project đang dùng. Sau phản hồi của người dùng, đã chạy compose down không có -v cho ba stack test cũ (dev, seed, E2E đầu), xác minh mọi volume trước đó vẫn tồn tại. Stack E2E kế tiếp được thay bằng dataset sạch cho lượt review cuối. Sau10/10 E2E, đã dọn cả stack cuối bằng compose down không có -v; không còn container/network snp-integration-* của task và mọi volume trước/sau được đối chiếu còn đủ. Các project có sẵn, gồm snp_week4_smoke, không bị sửa.

Giữ cấu hình/credential test trong vùng ignored để có thể truy cập lại DB. Không commit env, token, log hoặc ZIP backup. Kết quả dọn Markdown ngày21: bỏ ba guide đã có bản thay thế, giữ đủ27 day-*.md, chuyển báo cáo demo20/10 vào mục reports và bảo toàn ZIP/hash trước xóa. Xem danh sách và đường dẫn backup trong báo cáo21/10.

Commit mã sửa review: bc2db47. Chưa push, merge, publish hoặc triển khai production. UAT/vận hành theo [runbook](../RUNBOOK.md) và [checklist](../UAT-CHECKLIST.md). Export vẫn là đọc đủ trang, không phải snapshot transaction; relocate vật lý hiện dùng cùng block; dependency warning hiện có được ghi ở báo cáo21/10.

## Quyết định triển khai đầy đủ

Danh sách Ruling từ ledger được chép vào đây trước bàn giao để bảo toàn cả các quyết định ban đầu và những điều chỉnh sau đó.

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
- Final: Ruling: export rows can change concurrently without changing totals — documented nontransactional all-pages export stands, as no snapshot endpoint exists — cost if wrong: simultaneous writes may yield a mixed-time workbook.
- Final: Ruling: physical cross-block relocation — retain existing same-block API limitation and explicit UI message — cost if wrong: operators need a separate placement workflow or future API enhancement.
- Final: Ruling: identical fixed demo usernames across tenants — refuse a second tenant rather than transfer accounts — cost if wrong: a separate demo deployment needs unique account naming.
- Final: Ruling: production rollout, scale testing and operational UAT — retain as subsequent deployment work, local evidence only — cost if wrong: production-specific behavior remains unverified.
- Final: Ruling: existing dependency warnings — retain reported warnings without broad dependency upgrades in this integration — cost if wrong: transitive test-tooling vulnerabilities require separate remediation.
