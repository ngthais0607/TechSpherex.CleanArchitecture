# Tuần 4 — API, báo cáo và bàn giao

Trạng thái: audit kỹ thuật đã xử lý; toàn solution415 tests và Postman85 requests/135 assertions xanh. **Awaiting UAT**, chưa nghiệm thu toàn bộ tuần.

| Mốc | Nội dung | Nhật ký |
|---|---|---|
| Tiền kiểm | Solution parity, baseline build/test và Docker | [Baseline](day-00-baseline.md) |
| Ngày 1 | Nền API, tenant/auth, validation và danh mục — 354 tests xanh | [day-01.md](day-01.md) |
| Ngày 2 | API nghiệp vụ, lịch sử di chuyển nguyên tử, phân quyền — 376 tests xanh | [day-02.md](day-02.md) |
| Ngày 3 | Bốn báo cáo, đo SQL/HTTP trên 20.000 container — 397 tests xanh | [day-03.md](day-03.md) |
| Ngày 4 | HTTP E2E, Docker startup/upgrade và backup/restore thật — 406 tests xanh | [day-04.md](day-04.md) |
| Ngày 5 | Postman 85 requests/135 assertions xanh; cấu hình/guide; Awaiting UAT | [day-05.md](day-05.md) |
| Ngày 6 | Audit/fix và final gate415 tests xanh; Awaiting UAT | [day-06.md](day-06.md) |

Tài liệu để chạy và nghiệm thu:

- [Bắt đầu ở đây — bạn cần làm gì theo từng bước](../../README.md).
- [Hướng dẫn chạy dự án, build/test, Postman và xử lý lỗi](../../README.md).
- [Cấu hình và cách tự sinh key/password](CONFIGURATION-GUIDE.md) — không cần API key bên thứ ba.
- [Hướng dẫn Postman](POSTMAN-GUIDE.md), [collection](postman/SnpDepotManagement.postman_collection.json), [environment placeholder](postman/SnpDepotManagement.local.postman_environment.json).
- [API guide](../API-GUIDE.md), [ma trận quyền](endpoint-role-matrix.md), [endpoint inventory](endpoint-inventory.json).
- [UAT checklist](../UAT-CHECKLIST.md), [UAT results đang chờ](uat-results.md).
- [RUNBOOK](../RUNBOOK.md), [deployment evidence](deployment-evidence.json), [benchmark](report-performance.md).
- [Kiểm chứng cuối và giới hạn bàn giao](final-verification.md).
- [Findings audit, bản sửa và minor còn lại](audit-review.md).
- [Codebase structure and files — chỉ cục bộ](codebase-structure-and-files.md).

Không dùng số test từ tuần trước làm bằng chứng tuần này. File `codebase-structure-and-files.md` chỉ lưu cục bộ, không đưa lên Git. Chưa push code/image, chưa rollout production và chưa tự ký UAT thay người dùng.
