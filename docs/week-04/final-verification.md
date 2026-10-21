# Kiểm chứng cuối tuần 4

Trạng thái: **Technical verification passed / Awaiting UAT**, ngày2026-10-16. Chưa nghiệm thu toàn bộ tuần hoặc commit mốc hoàn tất ngày5/ngày6 khi người dùng chưa xác nhận UAT.

Final gate sau audit:

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build SnpDepotManagement.sln --no-restore -m:1` | 0 errors,205 warnings |
| `dotnet test SnpDepotManagement.sln --no-restore -m:1` | **415 passed /0 failed /0 skipped** |
| Theo project | Domain140, Application142, Architecture8, Infrastructure Unit33, Infrastructure Integration17, API75 |
| Regression audit có PostgreSQL/barrier | 14/14, gồm support race, Expand race, quota/completion race, legacy/history, condition, tax boundary, Location và metadata |
| Postman trên image sau audit | **85 requests /135 assertions /0 failed** |
| Benchmark20.000 container/hai tenant | 8/8 cases p95<1.000ms; max416,28ms; SQL/request3/2/3/2 |
| Docker image/migration mới | API healthy, UID1654, áp dụng ProtectAggregateConcurrency vào DB smoke giữ nguyên dữ liệu |
| Upgrade DB giả mới `snp_upgrade_audit` | Thiếu/sai mapping bị chặn; đúng mapping được giữ; rerun không downgrade; đủ6 migration |
| Boundary và endpoint metadata | API endpoint không dùng EF/persistence; names/tags/policies/fallback/anonymous inventory đã assert |
| Solution parity | .sln/.slnx cùng10 projects |
| Credential-pattern scan | Không có match private key/PAT/AWS access key/JWT serialized trong source/test/script/docsWeek4 và diff lịch sử commit tuần4; đây là scan theo mẫu, không cam kết tìm mọi dạng secret |
| Local inventory | `git check-ignore` khớp; inventory không thuộc tracked files/index |

Fresh reviewer đã hoàn tất, không có Critical; năm Important đã sửa và regression GREEN. Tax-code500 regrade Important và sửa400. Hai Minor còn lại ghi rõ ở [audit-review.md](audit-review.md): master-data Location chưa có GET, refresh expiry option chưa được dùng (guide đã nêu cố định7 ngày).

Postman sau sửa inventory từng phát hiện fixture cũ kỳ vọng thay đổi registry tự sửa lifecycle condition. Fixture Gate-In container4 được chỉnh Damaged theo spec; giữ assertion filter1, không đổi expected thành0 để che lỗi. Lượt cuối chạy đủ85/135 pass. Trong timeline legacy, bỏ fallback khi gate movement thực đã tồn tại, không tạo lịch sử cũ.

Logs cục bộ ignored: `final-build.log`, `final-all-tests.log`, `audit-regressions-red.log`, `audit-history-red.log`, `audit-regressions-green.log`, `postman-final.log`, `upgrade-final.log` trong `.superpowers/sdd/2026-10-16-week4-api-reports-handover`. Các evidence đã lọc nằm ở [deployment](deployment-evidence.json), [Postman](postman-results.json), [benchmark](report-performance.md), [endpoint inventory](endpoint-inventory.json).

Backup/restore20k, hash source/target, runtime packaging/CORS/413/health outage đã chạy thật ở ngày4; bằng chứng lịch sử giữ nguyên ở journal/deployment smoke. Sau audit chạy lại image/migration, upgrade và Postman; không ghi nhầm là đã chạy lại toàn bộ backup drill trên DB Postman hiện có.

Known risks: SSH.NET 2024.1.0 transitive trong Testcontainers test tooling có NU1903; không phải runtime API dependency. Tài liệu Postman hiện ghi Newman không còn maintained, nên runner v2.1 chỉ dùng để kiểm chứng cục bộ; API runtime không mang Newman/npm dependencies. Chưa có người nhận owner/due date của việc nâng dependency test, không tự gán.

UAT thật: [Awaiting](uat-results.md). Không ghi complete khi người dùng chưa nghiệm thu hoặc còn finding nghiêm trọng chưa xử lý.
