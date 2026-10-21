# Kiểm thử API bằng Postman

Để tự viết request và script test đầu tiên, xem [hướng dẫn chính](../../README.md). File này giải thích chi tiết collection tuần 4, thứ tự nghiệp vụ, token theo vai trò và kiểm thử bằng CLI.

Collection và environment chỉ chứa placeholder, không có secret thật:

- [SnpDepotManagement.postman_collection.json](postman/SnpDepotManagement.postman_collection.json)
- [SnpDepotManagement.local.postman_environment.json](postman/SnpDepotManagement.local.postman_environment.json)

## Import và điền cấu hình

1. Mở Postman Desktop, chọn **Import**, chọn cả hai file JSON trên.
2. Chọn environment **SnpDepotManagement - LOCAL placeholders** đang active.
3. Điền `baseUrl` đúng API đang chạy: Compose dev mặc định `http://localhost:5000`, host profile HTTP `http://localhost:5187`; các stack smoke dùng cổng riêng. HTTPS phải dùng URL thực của hạ tầng; không tắt xác minh certificate cho production.
4. Điền `adminUserName`, `adminPassword` đúng bootstrap/admin hiện có. Điền `testUserPassword` là mật khẩu tạm mạnh cho ba user demo, chỉ dùng trong môi trường test. Collection không cần JWT secret hoặc Postman API key.
5. Giữ password/token ở giá trị local, không share/sync giá trị secret lên workspace công khai. Postman cho phép giữ giá trị local và tùy chọn chia sẻ; xem [environment variables chính thức](https://learning.postman.com/docs/use/send-requests/variables/environment-variables). Đánh dấu secret giúp che màn hình nhưng không thay thế kiểm soát việc export/chia sẻ.

Nếu dùng môi trường smoke do task tạo, username/password ở file `.superpowers/sdd/2026-10-16-week4-api-reports-handover/snp_week4_smoke/smoke.env` trên máy, không nằm trong Git. Chỉ mở cục bộ, không gửi nội dung vào chat. Bạn cũng có thể tự tạo DB/Admin theo [CONFIGURATION-GUIDE](CONFIGURATION-GUIDE.md).

## Chạy đúng thứ tự

Chọn collection → **Run** (Collection Runner), chọn đủ các folder theo thứ tự, một iteration, dùng environment trên. Không chạy trên DB production: collection tạo danh mục chung, user, block, container và DO.

| Folder | Việc kiểm tra |
|---|---|
| 01 | Init suffix mới; Admin login; readiness; tạo danh mục/block; expand/shrink; layout/free slots; tạo user |
| 02 | Login riêng YardPlanner, OrderClerk, GateClerk |
| 03 | OrderClerk tạo DO quota 3 |
| 04 | GateClerk đăng ký/Gate-In bốn container |
| 05 | YardPlanner Remove/Place/Relocate; tồn bãi gồm Unplaced; chặn Gate-Out support khi còn stack/slot trùng |
| 06 | Gate-Out ba; quota 3→0; chặn xuất lần bốn/xuất lại; đọc đủ bốn báo cáo |
| 07 | 400/401/403/404/422; tenant mismatch; DO hết hạn; damage; refresh và chống replay |
| 08 | Container 40 feet dùng hai slot/một event; chặn ghép hai 20 feet thành một footprint; tái nhập giữ timeline cũ |

Request Init đầu tiên sinh suffix, DO/catalog/user riêng và số container với owner prefix ngẫu nhiên có check digit đúng; tạo ánh xạ prefix sang hãng thử nghiệm. Các Id lấy từ response, slot lấy từ layout. Chạy lại toàn collection tạo fixture mới, không reset/xóa dữ liệu cũ. Fixture là dữ liệu giả; nếu gặp trùng số container hiếm gặp, khởi tạo lượt mới trên DB test. Không sửa dữ liệu thật để nhường vị trí.

Sau mỗi request, tab **Tests** / **Post-response** có kiểm tra HTTP status, ProblemDetails, quota, vị trí và reports. Collection hiện sinh **90 requests**. Phiên tích hợp này kiểm chứng luồng bằng HTTP integration và Playwright; chưa chạy lại toàn bộ collection bằng Newman. Số85/135 của báo cáo tuần4 là mốc lịch sử, không phải kết quả hiện tại. Collection được thiết kế để giữ một container 20 feet hỏng còn trong depot ở cuối lượt; container thứ năm chỉ đăng ký để kiểm tra Gate-In trùng slot, chưa có lượt nhập. Không coi dữ liệu test còn lại là lỗi rollback.

Container thứ tư được Gate-In với Condition=Damaged. Inventory dùng condition của lifecycle/lượt nhập theo thiết kế; endpoint Mark Damage cập nhật hồ sơ Container, không sửa lại condition đã ghi của các lượt nhập. Vì vậy không dùng thay đổi hồ sơ này để kỳ vọng báo cáo lịch sử/lượt cũ tự đổi tình trạng.

## Login và token từng vai trò

Mỗi request bảo vệ chọn Bearer token riêng: `adminAccessToken`, `yardAccessToken`, `orderAccessToken`, `gateAccessToken`. Không dùng Admin token thay tất cả vai trò. Script sau Login lưu access/refresh token vào environment theo role. Ví dụ:

```javascript
const payload = pm.response.json();
pm.environment.set('gateAccessToken', payload.accessToken);
pm.environment.set('gateRefreshToken', payload.refreshToken);
```

Nếu gửi từng request thủ công, chạy Init → Admin login → tạo fixture/users → Login từng role trước. Nếu token hết hạn, chạy request Refresh với đúng username và refresh token hiện tại; token được xoay, replay token cũ phải 401. Login lại cùng tài khoản cũng thay refresh-token hash trước đó, nên tránh hai người cùng dùng một user test.

Không cần `X-Tenant-Id` khi Login hoặc gọi API. Nếu gửi header này ở endpoint bảo vệ, nó phải khớp tenant trong token; header khác depot trả 403 và không chọn được depot mới. Một tài khoản thuộc một depot. Muốn test depot khác phải dùng tài khoản đã được gán depot đó, không tự sửa JWT.

## Đọc lỗi

| Status | Ý nghĩa / ví dụ trong collection |
|---|---|
| 400 | JSON/enum/container number/query không hợp lệ |
| 401 | Thiếu/sai access token, refresh hết hạn hoặc replay |
| 403 | Vai trò không có quyền hoặc tenant header khác JWT |
| 404 | Resource không tồn tại hoặc thuộc depot khác |
| 409 | Conflict, slot trùng, container đã xuất hoặc footprint sai |
| 422 | Rule nghiệp vụ: quota hết, DO hết hạn, còn container phía trên, shrink vùng đang có container |

ProblemDetails có `status`, `title`, `type`, `code`, `traceId`; validation body có `errors`. Dùng `traceId` để tìm log nội bộ, không copy password/token vào báo lỗi. Các case expected 4xx trong collection phải **pass assertion**, không đổi thành 2xx.

GET danh sách dùng `pageIndex` từ 1, `pageSize` mặc định 20, tối đa 100. Reports dùng UTC timestamp nhưng ngày nghiệp vụ UTC+7. Timeline legacy chỉ có mốc vào/ra thực cộng movement sau rollout; không mong có lần chuyển/gỡ trong quá khứ nếu hệ thống cũ không lưu.

## Chạy collection cục bộ bằng CLI (tùy chọn)

Bạn không cần CLI để dùng Postman GUI. Task dùng file collection **v2.1** với Newman 6.2.3 để kiểm chứng tự động. Theo [tài liệu Postman hiện tại](https://learning.postman.com/docs/reference/newman-cli/newman-options), Newman không còn được duy trì và không hỗ trợ collection v3 của Postman v12 Native Git; nếu tổ chức chuyển sang v3, dùng Postman CLI theo hướng dẫn chính thức. Không export lại thành v3 rồi đưa cho Newman.

Tái hiện đúng môi trường smoke cục bộ, không cần gửi dữ liệu/token lên Postman Cloud:

```powershell
npm install --prefix .superpowers/sdd/2026-10-16-week4-api-reports-handover/postman-runner --no-audit --no-fund newman@6.2.3
node scripts/generate-postman.mjs
node scripts/run-postman-smoke.mjs --disposable-test
```

Runner chỉ đọc secret smoke trong thư mục ignored, giữ token trong memory và xuất summary đã lọc. Không tạo report HTTP đầy đủ chứa Authorization headers để commit. Kết quả nghiệm thu tự động nằm ở `postman-results.json` trong ledger và bản đã lọc trong week-04 khi bàn giao. Newman là dependency kiểm thử riêng, không được đóng gói vào API runtime.

## Sau khi chạy

Ghi actual/pass/fail vào [UAT-CHECKLIST](../UAT-CHECKLIST.md), kèm thời điểm, vai trò/người chạy và evidence không chứa secret. Kiểm thử tự động không tự ký thay người nghiệm thu. Xóa giá trị token/password trước khi export environment, hoặc chỉ chia sẻ file placeholder đã cung cấp. Môi trường smoke/backup có thể dọn sau nghiệm thu theo đúng project trong RUNBOOK; không xóa volume của project khác.
