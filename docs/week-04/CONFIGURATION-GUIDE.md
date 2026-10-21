# Cấu hình và các key cần chuẩn bị

**Không cần API key trả phí của bên thứ ba cho phạm vi tuần 4.** Bạn tự tạo JWT secret, mật khẩu PostgreSQL và mật khẩu Admin ban đầu. Access token/refresh token do API cấp sau Login; không mua hoặc lấy từ nhà cung cấp khác. Không gửi secret thật vào chat.

## Các biến đang được ứng dụng dùng

| Biến .NET / Docker | Yêu cầu và ý nghĩa |
|---|---|
| `ConnectionStrings__DefaultConnection` | Bắt buộc; Npgsql connection string của PostgreSQL. Compose production tự ghép từ ba biến POSTGRES bên dưới. |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Bắt buộc với Compose production; database, role và mật khẩu do bạn quản lý. |
| `Jwt__Secret` | Bắt buộc; tự sinh ít nhất 32 byte ngẫu nhiên rồi base64. API kiểm tra độ dài tối thiểu 32 ký tự, không chấp nhận placeholder `REPLACE_...`. |
| `Jwt__Issuer`, `Jwt__Audience` | Nhãn cấu hình, không phải secret. Chọn cố định, ví dụ tên hệ thống và nhóm client; issuer/validator phải cùng giá trị. |
| `Jwt__AccessTokenExpirationMinutes` | Mặc định 60 nếu không override; đơn vị phút. |
| Refresh token | Hiện cố định 7 ngày trong Login/Refresh handler. `Jwt__RefreshTokenExpirationDays` có trong options nhưng chưa được handler dùng; đổi biến này không đổi hạn refresh token. |
| `Admin__UserName`, `Admin__TenantId`, `Admin__Password` | Bootstrap Admin một depot. Password và TenantId không có fallback; điền username rõ ràng. |
| `Admin__Email`, `Admin__FirstName`, `Admin__LastName` | Tùy chọn; mặc định trong appsettings. Email của user là duy nhất toàn hệ thống. |
| `ConnectionStrings__Redis` | Tùy chọn khi chạy native; Compose đặt Redis nội bộ. Không dùng API key. |
| `Redis__Required` | true: Redis lỗi khiến readiness 503; false: Redis thiếu trả Degraded/200. |
| `Cors__AllowedOrigins__0` | Origin frontend đầy đủ, ví dụ `https://depot.example.vn`; không dùng `*`. Native không cấu hình thì không mở CORS cho browser khác origin. Có thể thêm `__1`, `__2` khi chạy native. |
| `ASPNETCORE_ENVIRONMENT` | `Development` cho máy dev; `Production` cho deploy. Scalar/OpenAPI chỉ Development. |
| `ReverseProxy__TrustedProxy` | Tùy chọn, một IP proxy đáng tin. Chỉ bật khi có TLS reverse proxy thật; không nhập IP tùy ý hoặc wildcard. |
| `API_PORT` | Cổng host Compose, mặc định 8080, bind loopback. API trong container luôn 8080. |
| `MIGRATION_MODE` | `fresh` hoặc `upgrade`; upgrade cần CSV mapping user/depot được vận hành xác nhận. |
| `BACKUP_HOST_DIR`, `BACKUP_RETENTION` | Mặc định `./backups`, 7 bản; retention 1–10000. Chỉ prune sau dump thành công. |
| `MIGRATION_INPUT_DIR` | Thư mục chứa `user-tenants.csv`, mặc định `./migration-input`; không đưa vào Git. |

JWT secret/password trong appsettings đang để trống có chủ đích. Các nhãn `JWT_SECRET` trong ví dụ `.env` cũ không tự ánh xạ thành `Jwt__Secret`; .NET dùng tên có **hai dấu gạch dưới**. File `.env` cũng không tự được `dotnet run` nạp vào process.

## Tự sinh secret trên Windows

PowerShell 7:

```powershell
$jwtSecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$dbPassword = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$adminPassword = 'Admin9!' + [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
```

Các lệnh trên chỉ giữ giá trị trong biến, không in secret. Lưu vào password manager hoặc file cấu hình cục bộ có quyền đọc giới hạn. Không paste các biến vào issue, log, screenshot hoặc Git.

## Docker production

1. Copy `.env.prod.example` thành `.env.prod` ở root. Giữ file example nguyên placeholder, không thay bằng secret thật để commit.
2. Mở `.env.prod` cục bộ, thay toàn bộ `REPLACE_...` bằng giá trị của bạn. Dùng `$jwtSecret`, `$dbPassword`, `$adminPassword` đã tạo. Chọn TenantId ổn định, ví dụ mã depot của đơn vị; đây là danh tính nghiệp vụ, không tùy ý đổi sau khi có dữ liệu.
3. `POSTGRES_DB`/`POSTGRES_USER` là tên bạn đặt. Không cần lấy ở website nào. Nếu dùng PostgreSQL do đơn vị quản lý, lấy host/database/user/password từ DBA và cấu hình connection theo hạ tầng đó thay vì tạo DB Compose mới.
4. Chạy theo [RUNBOOK](../RUNBOOK.md). Không dùng `config` không có `--quiet` trong log chia sẻ vì sẽ lộ env đã resolve.

```powershell
$dc = @('compose','--env-file','.env.prod','-f','docker-compose.prod.yml')
docker @dc config --quiet
docker @dc up -d --build
docker @dc ps
```

Chạy trên DB mới dùng `fresh`. DB cũ phải backup, cung cấp mapping user/depot và chạy `upgrade`; không gán bừa toàn bộ user về DEPOT-A. Tài khoản Admin của depot được quản lý bốn danh mục dùng chung, còn dữ liệu container/bãi/DO/history thuộc depot trong JWT.

## Chạy native trên Windows

Cần .NET SDK 10 và PostgreSQL đã chuẩn bị. Không dùng database production để thử lệnh migration. Đặt biến cho **process PowerShell hiện tại**, không ghi secret vào script được commit:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = '<NPGSQL_CONNECTION_CUA_DB_DEV>'
$env:Jwt__Secret = $jwtSecret
$env:Jwt__Issuer = 'SnpDepotManagement'
$env:Jwt__Audience = 'SnpDepotManagement'
$env:Admin__UserName = 'admin'
$env:Admin__TenantId = '<MA_DEPOT_DEV>'
$env:Admin__Password = $adminPassword
$env:Redis__Required = 'false'
dotnet restore SnpDepotManagement.sln
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api --no-launch-profile --urls http://localhost:8080
```

Nếu `dotnet ef` chưa có, cài theo tool/version đang được repo sử dụng; image migrator đã đóng gói EF 10.0.12 và không cần EF tool ở API runtime. Cách Docker trong RUNBOOK là luồng triển khai đã chạy startup/restore thật. Không chạy `database update` thẳng trên DB cũ chưa có mapping.

Redis native nếu có: đặt `ConnectionStrings__Redis` theo server của bạn. Compose hiện để Redis không auth trong mạng riêng, không publish cổng. Nếu đơn vị dùng Redis ACL/password hoặc dịch vụ managed, lấy credential từ người quản lý Redis và cấu hình hạ tầng/connection tương ứng; không có key Redis bên thứ ba bắt buộc cho demo này.

## Lấy token và dùng HTTPS

POST `/api/auth/login` với username/password, nhận `accessToken`, `refreshToken`, `expiresAt`. Gửi `Authorization: Bearer <accessToken>` cho các API bảo vệ. Xem [POSTMAN-GUIDE](POSTMAN-GUIDE.md). Không gửi JWT secret vào Postman request.

Local HTTP loopback không cần certificate. Public deployment cần TLS certificate cho domain thật tại reverse proxy, theo hệ thống chứng thực/ACME của hạ tầng. Không có yêu cầu mua certificate hoặc dùng một nhà cung cấp cụ thể. Tin đúng IP proxy qua `ReverseProxy__TrustedProxy`; xem RUNBOOK.

## Đổi và thu hồi secret

- Đổi JWT secret/issuer/audience và restart các API instance cùng cấu hình sẽ làm access token cũ không còn hợp lệ. Refresh token còn lưu có thể cấp access token mới; nếu cần buộc mọi người đăng nhập lại, vận hành phải thu hồi refresh-token hash/expiry trong DB theo quy trình được duyệt, không chỉ đổi JWT secret.
- Admin password chỉ dùng **khi tạo tài khoản ban đầu**. Đổi `Admin__Password` rồi restart không reset mật khẩu user đã tồn tại. Tuần 4 chưa có endpoint đổi/reset password; thực hiện bảo trì identity được người quản lý hệ thống duyệt, dùng password hasher của ứng dụng, không lưu plaintext vào DB.
- Đổi mật khẩu PostgreSQL của role thật trước, cập nhật các connection/env liên quan rồi restart có kiểm soát. Thay POSTGRES_PASSWORD trong env không tự đổi password của volume PostgreSQL đã được khởi tạo. Không reset/xóa volume để đổi password.
- Nếu lộ credential thật, ghi phạm vi và thực hiện rotate/revoke; chỉ xóa file hoặc commit không đủ. Không gửi credential lộ vào chat để nhờ kiểm tra.

Môi trường smoke dùng secret ngẫu nhiên riêng trong thư mục `.superpowers` ignored. Không dùng các giá trị đó cho production. Không cần cung cấp key nào cho mình để hoàn tất tài liệu hoặc kiểm thử tự động.
