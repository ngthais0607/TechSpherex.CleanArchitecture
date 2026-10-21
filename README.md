# SnpDepotManagement

Quản lý depot container vỏ rỗng: Angular 21 kết nối API .NET 10, PostgreSQL và Redis. Các màn đăng nhập, container, lệnh giao/booking, cổng, sơ đồ bãi, đối tác, dashboard và báo cáo dùng dữ liệu BE. Dữ liệu nghiệp vụ không lưu trong trình duyệt; phiên token lưu trong sessionStorage.

## Chuẩn bị

- Windows/PowerShell 7.2+, Docker Desktop với Linux engine.
- .NET SDK 10 để chạy/test backend trên host.
- Node.js 20.19+ và npm; cài đúng lockfile bằng `npm.cmd ci --prefix frontend`.
- Xem [báo cáo kiểm chứng ngày 21/10/2026](docs/reports/2026-10-21-fe-be-integration.md) cho kết quả thực và giới hạn.

## Chạy bằng Docker và Angular

Tại thư mục repository đang chứa bản tích hợp:

```powershell
if (-not (Test-Path -LiteralPath .env)) { Copy-Item .env.example .env }
```

Điền file `.env` cục bộ trước khi chạy:

| Biến | Mục đích |
| --- | --- |
| POSTGRES_PASSWORD | Mật khẩu PostgreSQL riêng cho DB mới |
| JWT_SECRET | Secret ngẫu nhiên tối thiểu 32 ký tự |
| ADMIN_USERNAME / ADMIN_PASSWORD / ADMIN_TENANT_ID | Bootstrap Admin và depot |
| API_PORT | Mặc định 5000; phải khớp proxy frontend |
| POSTGRES_PORT / REDIS_PORT | Chọn cổng còn trống trên máy |
| SEED_DEMO_DATA | `true` nếu muốn dataset Development trên DB mới |
| SEED_USER_PASSWORD | Mật khẩu riêng cho yard/order/gate khi bật seed |
| DEPOT_NAME / DEPOT_TIMEZONE | Tên depot; mặc định Asia/Ho_Chi_Minh |
| CORS_ALLOWED_ORIGIN | Mặc định http://localhost:4200 |

Giữ cấu hình DB/secret đã dùng cho volume hiện có. Bootstrap không đổi mật khẩu user đã tồn tại. Không commit env/password/token.

```powershell
docker compose -p snp-depot-dev --env-file .env config --quiet
docker compose -p snp-depot-dev --env-file .env up -d --build
docker compose -p snp-depot-dev --env-file .env ps -a
Invoke-RestMethod http://localhost:5000/health/live
npm.cmd ci --prefix frontend
npm.cmd start --prefix frontend
```

Mở http://localhost:4200. Proxy `/api` mặc định trỏ đến API HTTP cổng 5000. Đổi API_PORT thì cập nhật target trong `frontend/proxy.conf.json` cho khớp. PostgreSQL healthy → migrator chạy xong → API khởi động; `migrate Exited(0)` là bình thường. Trong Development có `/scalar/v1` và `/openapi/v1.json`.

Dừng bằng `docker compose -p snp-depot-dev --env-file .env stop` để giữ dữ liệu. Nâng cấp DB cũ và backup/restore theo [RUNBOOK](docs/RUNBOOK.md); không dùng ví dụ DB mới cho dữ liệu vận hành.

## Chạy API trên host

Nạp biến môi trường .NET: `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Admin__UserName`, `Admin__Password`, `Admin__TenantId`; nếu seed thêm `Seed__DemoData=true`, `Seed__UserPassword` và `ASPNETCORE_ENVIRONMENT=Development`. `.env` của Compose không tự nạp vào `dotnet run`.

```powershell
dotnet restore SnpDepotManagement.sln
dotnet tool restore
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api --launch-profile http
```

Profile HTTP dùng cổng 5187. Frontend chạy `npm.cmd run start:host --prefix frontend` để dùng proxy cổng này. HTTPS profile là opt-in ở Development. [Configuration guide](docs/week-04/CONFIGURATION-GUIDE.md) mô tả thêm cấu hình native/production.

## Seed và quyền

Seed Development phiên bản `fe-be-v1` tạo 50 container, 5 lệnh, 3 khách hàng, hãng/owner-code và block A/B/VB-01. Ban đầu: 20 container trong bãi (15 vật lý, 5 ảo), 3 đã xuất, 27 chưa nhập. Có lịch sử nhập/xuất/di chuyển đồng nhất. Marker theo tenant/version, transaction và advisory lock ngăn seed trùng khi restart/concurrent startup. Seed không chạy trong Staging/Production; dữ liệu cũ trùng mã hoặc tài khoản khác tenant/role được báo lỗi thay vì ghi đè.

| Vai trò | Quyền chính |
| --- | --- |
| Admin | Danh mục, container, lệnh, cổng, bãi, báo cáo |
| YardPlanner (user yard) | Quản lý bãi; đọc container/danh mục/báo cáo, không gọi lệnh sắp hết hạn |
| OrderClerk (user order) | Tạo lệnh, gán/gỡ booking; đọc container/danh mục/báo cáo |
| GateClerk (user gate) | Đăng ký container, nhập/xuất/sửa/hủy cổng; đọc bãi và lệnh |

Đăng nhập bằng Admin đã cấu hình hoặc user seed với SEED_USER_PASSWORD. Chỉ Admin/YardPlanner thấy hai nút **Mở rộng bãi** và **Thu hẹp bãi** khi chọn block vật lý. BE chặn thu hẹp vùng còn container. Xem [ma trận quyền và endpoint](docs/week-04/endpoint-role-matrix.md).

Container chưa nhập phải gate-in trước khi gán lệnh. Với dataset mẫu, luồng 35–36 là nhập → gán DO-2026-002 → xuất. Booking của DO-2026-004 đã hết hạn được BE quyết định khi gán lại; FE không tự giải phóng quota.

## Kiểm thử và nghiệm thu

```powershell
dotnet build SnpDepotManagement.sln
dotnet test SnpDepotManagement.sln --no-build --no-restore
npm.cmd test --prefix frontend
npm.cmd run build --prefix frontend
npm.cmd run check:api-runtime --prefix frontend
node scripts/check-doc-links.mjs
```

Integration tests dùng PostgreSQL Testcontainers, cần Docker. Chạy tuần tự build/test cùng project để tránh khóa binary. Kiểm thử E2E dùng stack mới, dữ liệu thật và token một phút riêng cho test:

```powershell
pwsh -NoProfile -File scripts/prepare-api-e2e.ps1 -ApiPort 15002
npm.cmd run e2e:api --prefix frontend
```

Script tạo project `snp-integration-e2e-<timestamp>`, seed/restart hai lần và cấu hình ignored trong `.superpowers/sdd/2026-10-21-fe-be-integration/`. E2E chạy FE cổng 14202 riêng, không mock API nghiệp vụ. Lần tiếp theo dùng API port còn trống và dataset mới vì workflow có ghi dữ liệu. TTL một phút chỉ áp dụng API E2E. [Postman collection](docs/week-04/postman/SnpDepotManagement.postman_collection.json) được sinh từ `node scripts/generate-postman.mjs`; điền credential trong environment local.

## Báo cáo và giới hạn

- KPI lấy từ summary BE; tổng container khác số nhóm tồn bãi.
- Danh sách có phân trang/lọc server; phản hồi tìm kiếm cũ bị hủy.
- Excel lấy đủ từng trang 100 dòng, có tiến độ và hủy; lỗi trang hoặc tổng thay đổi thì không tải file dở. Chuỗi bắt đầu =/+/-/@ giữ kiểu text.
- Các trang export không phải snapshot transaction đồng thời. Dashboard/report cache phía BE có TTL; nút làm mới không bảo đảm bỏ qua TTL.
- Relocate vật lý theo API hiện tại áp dụng cùng block; phân vị trí từ bãi ảo hoặc container chưa xếp có luồng riêng.
- Đây là kết quả kiểm thử tự động/local; UAT và triển khai production theo [checklist](docs/UAT-CHECKLIST.md), [runbook](docs/RUNBOOK.md) và [deployment checklist](docs/week-04/deployment-smoke-checklist.md).

## Tài liệu và phục hồi demo

- [Nhật ký ngày và báo cáo](docs/reports/README.md), [lịch sử tuần](docs/archive/README.md).
- [API guide](docs/API-GUIDE.md), [hướng dẫn Postman](docs/week-04/POSTMAN-GUIDE.md).
- [Yêu cầu UI](docs/design/ui-requirements.md), [nghiệp vụ BE từ UI](docs/design/backend-requirements-from-ui.md).
- [Thiết kế tích hợp](docs/superpowers/specs/2026-10-21-fe-be-integration-design.md), [kế hoạch](docs/superpowers/plans/2026-10-21-fe-be-integration.md).
- [Frontend](frontend/README.md), [Domain](src/Domain/README.md).

Bản demo offline được giữ ở tag `demo-offline-2026-10-21` (commit `6ac340a`). Có thể tạo checkout riêng để xem lại; tag chứa mã/snapshot demo cũ. Bản tích hợp không nhập dữ liệu localStorage cũ vào BE.
