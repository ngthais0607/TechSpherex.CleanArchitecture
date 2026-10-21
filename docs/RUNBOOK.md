# Vận hành API depot

Trạng thái: đã kiểm chứng packaging, smoke, upgrade và restore trên project dùng một lần ngày 2026-10-16; xem nhật ký ngày 4 và deployment-evidence.json. Production thật vẫn cần mapping, backup và quyết định rollout của người vận hành.

## Chuẩn bị cấu hình

Docker Desktop phải chạy Linux containers. Dùng Docker Compose v2. API cần .NET 10 nếu chạy ngoài Docker; không cần SDK trong API runtime image.

Copy `.env.prod.example` thành `.env.prod` ở root repository, thay toàn bộ placeholder. Không commit `.env.prod`, mật khẩu, token hoặc CSV mapping. Cấu hình JWT secret tự sinh bằng cryptographic RNG, không mua API key bên ngoài. Hướng dẫn đầy đủ ở `docs/week-04/CONFIGURATION-GUIDE.md` khi bàn giao.

Trong PowerShell từ root repository:

```powershell
$dc = @('compose', '--env-file', '.env.prod', '-f', 'docker-compose.prod.yml')
docker @dc config --quiet
docker @dc build
docker @dc up -d
docker @dc ps
```

Không dùng `docker compose config` không có `--quiet` trong log chia sẻ vì output có thể chứa secret. Chỉ API publish cổng, mặc định `127.0.0.1:8080`; PostgreSQL/Redis ở mạng Compose nội bộ. Mỗi project có volume PostgreSQL riêng. `migrate` chạy EF bundle một lần trước API, API không tự migrate schema.

## Fresh database

Đặt `MIGRATION_MODE=fresh`; migrator áp dụng các migration đang chờ. API sau đó seed role và Admin từ cấu hình, không dùng mật khẩu fallback. Thiếu JWT secret đủ độ dài, issuer/audience, DB hoặc Admin bắt buộc sẽ dừng startup. Không dùng fresh mode để đoán TenantId cho user cũ.

## Nâng cấp database cũ

1. Dừng ghi dữ liệu và backup trước rollout. Ghi phiên bản ứng dụng/image và migration hiện tại. Thử restore backup vào DB mới trước thao tác thật.
2. Vận hành xác nhận CSV cho **mọi user hiện có**, header `user_id,tenant_id`. Không tự gán toàn bộ user về một depot. Đặt file tại `migration-input/user-tenants.csv` (hoặc thư mục `MIGRATION_INPUT_DIR`), quyền đọc giới hạn.
3. Đặt `MIGRATION_MODE=upgrade`. Migrator áp dụng tới `AddUserTenant` nếu chưa có, chạy mapping trong transaction rồi mới `RequireUserTenant` và các migration còn lại. Mapping thiếu, trắng, trùng, user lạ hoặc đổi tenant đã gán đều bị chặn. Nếu fail, giữ ứng dụng cũ dừng ghi và sửa mapping; không sửa migration để bỏ kiểm tra.
4. Khi contract đã áp dụng, lần chạy upgrade sau chỉ chạy migration mới; không downgrade về expand. Lịch sử migration bất nhất bị từ chối.
5. Kiểm tra readiness và thực hiện các luồng theo checklist UAT trước mở ghi.

CSV trong repository là file vận hành cục bộ, không đưa vào Git hoặc Docker image. Không chạy `dotnet ef database update` tới migration cũ trên DB đang dùng; rollback ưu tiên restore backup vào database mới rồi chuyển connection về đó theo quyết định vận hành.

## Health, Redis và TLS

`GET /health/live` anonymous chỉ kiểm tra process. `GET /health/ready` cần token Admin, kiểm tra PostgreSQL và Redis; thiếu token trả 401, vai trò khác trả 403.

- DB unavailable: readiness 503; liveness vẫn 200.
- `Redis__Required=true`: Redis thiếu hoặc mất kết nối trả readiness 503.
- Redis optional: thiếu Redis trả Degraded/200, kèm mô tả cho Admin; không giả Healthy. Cache có thể dùng bộ nhớ trong process theo HybridCache.

Không publish trực tiếp HTTP container ra Internet. Đặt reverse proxy terminates TLS trên domain thực; cấp và gia hạn certificate theo nhà cung cấp chứng thực phù hợp (ví dụ ACME của hạ tầng bạn dùng). Proxy chuyển tới cổng API loopback hoặc mạng Docker riêng và phải ghi đè các header forwarded do client gửi.

Nếu cần ứng dụng nhận scheme/client IP từ proxy, đặt `ReverseProxy__TrustedProxy` đúng **một IP proxy được tin cậy**. Ứng dụng chỉ chấp nhận một hop X-Forwarded-For/Proto từ IP đó. Không đặt wildcard hoặc tin toàn bộ mạng. Nếu để trống, ứng dụng không xử lý forwarded headers. Dùng `Cors__AllowedOrigins__0` là origin frontend đầy đủ, không `*`; CORS không thay thế xác thực.

## Backup

```powershell
docker @dc --profile tools run --rm backup
```

Backup dùng pg_dump PostgreSQL 17 custom format, không owner/ACL; tên `snp-depot_YYYYMMDDTHHMMSSZ.dump`. File partial chỉ đổi tên sau khi dump và đọc TOC thành công. Retention mặc định 7 file, có thể 1–10000. Chỉ prune file hoàn tất đúng mẫu trong `/backups` sau dump thành công; không chạm file khác. `BACKUP_HOST_DIR` mặc định `./backups`; phải chọn đường dẫn và quyền truy cập phù hợp. Backup chứa dữ liệu nghiệp vụ/identity; hạn chế quyền đọc, mã hóa và lưu bản sao ngoài máy theo chính sách của đơn vị. Không tự gửi backup lên dịch vụ ngoài.

## Restore vào database mới

Thay timestamp bằng file thật, tên đích phải khác database nguồn và chưa tồn tại:

```powershell
docker @dc --profile tools run --rm --entrypoint sh backup /scripts/restore-db.sh /backups/snp-depot_TIMESTAMP.dump --confirm-target snp_restore_new
docker @dc --profile tools run --rm --entrypoint psql backup -X -q -v ON_ERROR_STOP=1 -f /scripts/verify-restore.sql
docker @dc --profile tools run --rm --entrypoint psql backup -X -q -v ON_ERROR_STOP=1 -d snp_restore_new -f /scripts/verify-restore.sql
```

So sánh output source/target: counts theo tenant, quota và timeline event. Restore dùng single transaction và dừng ngay khi có lỗi. Script không drop database và không dùng `--clean`; nếu restore fail, database mới có thể còn rỗng, giữ lại để điều tra hoặc xóa theo quyết định vận hành. Chỉ chuyển connection ứng dụng sau khi kiểm chứng và được người chịu trách nhiệm rollout duyệt. Đặt lại owner/quyền DB theo role vận hành sau restore vì dump không mang ACL.

## Tái hiện smoke trên môi trường dùng một lần

```powershell
./scripts/smoke-production.ps1 -DisposableTest
./scripts/smoke-upgrade.ps1 -DisposableTest
```

Script tạo project `snp_week4_smoke`, secret thử trong `.superpowers/.../snp_week4_smoke/smoke.env`, API cổng 18084, backup và CSV riêng. Không đọc `.env`/DB developer. Smoke production seed 20.000 container trong DB thử, dừng/khởi động dependency để test readiness, kiểm tra 413 qua Kestrel, CORS, non-root, production docs, missing secret, backup/restore và retention. Upgrade drill tạo DB mới `snp_upgrade_test` chứa user giả bị disable. Không dùng hai script này với project production.

Sau khi hoàn thành Postman/UAT, chỉ dọn project thử bằng đúng env file/project đã ghi trong evidence. Không dùng `docker system prune` hoặc xóa volume của project khác. Không push image, merge hay deploy production nếu chưa được yêu cầu.
