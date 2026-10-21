# UAT tuần 4 — người dùng nghiệm thu

**Trạng thái: Awaiting UAT.** Tự động hóa đã có bằng chứng build/test, HTTP flow, benchmark và deployment. Chưa có người dùng ký pass, không coi việc Codex chạy test là chữ ký nghiệm thu.

Người chạy: ______  Ngày/giờ (Asia/Bangkok/UTC+7): ______  Môi trường/baseUrl: ______  Commit/image: ______

Chỉ dùng môi trường test, không dùng production. Import collection/environment và làm theo [POSTMAN-GUIDE](week-04/POSTMAN-GUIDE.md). Giữ secret local, không gửi token/password trong evidence. Ghi actual, pass/fail và tên người chạy từng case hoặc ghi rõ một lần chạy collection đã quan sát đầy đủ các case.

| Case | Role / bước | Expected | Actual / kết quả / người chạy |
|---|---|---|---|
| U01 | Login Admin rồi login riêng ba role được Admin tạo | Token đúng vai trò/depot; request không token 401 | Awaiting |
| U02 | Admin tạo danh mục; ba role đọc và thử ghi danh mục | Đọc được; chỉ Admin ghi; danh mục có phạm vi chung | Awaiting |
| U03 | Admin/YardPlanner tạo, expand rồi shrink block trống | Thành công, layout/slot count phù hợp | Awaiting |
| U04 | OrderClerk tạo DO đủ customer/vessel và quota 3 | 201; RemainingQuantity=3 | Awaiting |
| U05 | GateClerk đăng ký/Gate-In bốn container | 4 active, vị trí chiếm ngay; không Place lại ngay | Awaiting |
| U06 | YardPlanner Remove→Place→Relocate | Inventory vẫn 4, có Unplaced khi remove; vị trí/history đồng bộ | Awaiting |
| U07 | Gate-Out container đỡ stack; Gate-In vào slot đang chiếm | 422/409; không mất vị trí/quota/history | Awaiting |
| U08 | Shrink vùng có container | 422, layout/occupancy giữ nguyên | Awaiting |
| U09 | GateClerk xuất ba container với DO quota 3 | Quota 3→2→1→0; inventory 4→1 | Awaiting |
| U10 | Xuất container thứ tư rồi xuất lại container đầu | 422 hết quota; 409 đã xuất; không ghi event lỗi | Awaiting |
| U11 | Container number/enum sai, role sai, resource lạ, tenant header khác | 400/403/404; ProblemDetails có code/traceId | Awaiting |
| U12 | Tạo DO đã hết hạn rồi xuất container còn active | 422, quota/vị trí còn nguyên | Awaiting |
| U13 | 40ft Gate-In→Remove→Place→Relocate→Gate-Out | Hai slot cùng ContainerId, một event mỗi thao tác, bay logic chẵn | Awaiting |
| U14 | Hai 20ft riêng ở bay lẻ kề nhau; Remove/Relocate bay chẵn | 409 InvalidFootprint, giữ cả hai container | Awaiting |
| U15 | Tái nhập hai container đã xuất, rồi xuất bằng DO mới | Lượt mới riêng, timeline cũ không mất/đếm đôi | Awaiting |
| U16 | Mark damage, xem inventory/dwell/history/expiry và paging | Filter/total/quota đúng; 40ft không đếm đôi; page ổn định | Awaiting |
| U17 | Refresh rồi replay refresh token cũ | Refresh mới 200; replay 401; access mới dùng được | Awaiting |
| U18 | User depot A đọc/ghi id depot B, header đổi depot | Không lộ dữ liệu B; 404/403; B không đổi | Awaiting |
| U19 | Xem timeline legacy + movement sau rollout | Chỉ mốc thực; nguồn LegacyLifecycle/Movement rõ; không dựng quá khứ | Awaiting |
| U20 | Chạy test biên nửa đêm bằng TimeProvider cố định | Report/Gate-Out cùng ngày UTC+7, không đổi đồng hồ máy | Awaiting |
| U21 | Xem/chạy evidence startup, dependency outage và restore DB riêng | Health đúng, restore counts/quota/tenant/history khớp | Awaiting |

Collection mở rộng bao phủ U01 và phần lớn U03–U17 bằng assertions. U02/U18 còn có role/tenant matrix HTTP tự động; người nghiệm thu có thể chạy và xác nhận bằng output:

```powershell
dotnet test tests/Api.IntegrationTests --filter 'FullyQualifiedName~MasterDataTests|FullyQualifiedName~AuthorizationMatrixTests|FullyQualifiedName~TenantAuthenticationTests|FullyQualifiedName~ReportAuthorizationTests'
dotnet test tests/Api.IntegrationTests --filter 'FullyQualifiedName~HistoryReportTests|FullyQualifiedName~ExpiringOrdersTests'
```

Với DB smoke đã seed, history `MSKU6000153` có Gate-In legacy và Remove sau rollout; `MSKU6000127` có Gate-In/Gate-Out legacy. Đây là dữ liệu giả được tạo trong DB thử, không đại diện hồ sơ cũ thật. DB mới không có legacy: dùng HistoryReportTests làm evidence và ghi rõ điều kiện môi trường, không tạo sự kiện giả trong production để đánh dấu pass.

Nếu muốn tạo môi trường smoke sạch khác, dùng tên project/cổng mới để không đụng dữ liệu lần trước:

```powershell
./scripts/smoke-production.ps1 -DisposableTest -Project snp_week4_uat -Port 18086
./scripts/smoke-upgrade.ps1 -DisposableTest -Project snp_week4_uat
```

Không chạy lại seed trên project đã có dữ liệu Postman rồi xóa volume để vượt lỗi. GUI có thể dùng baseUrl 18086 và credential local ở thư mục đúng project mới.

Khi có failure: ghi case, role, steps, expected, actual, traceId/evidence đã che secret, severity, người chịu trách nhiệm và hạn xử lý do người nhận xác nhận. Không tự điền owner/due date thay người nhận. Chưa nghiệm thu khi còn lỗi nghiêm trọng hoặc người dùng chưa xác nhận kết quả.

Quyết định nghiệm thu: ______  Người nghiệm thu: ______  Thời điểm: ______  Ghi chú/ngoại lệ được chấp nhận: ______
