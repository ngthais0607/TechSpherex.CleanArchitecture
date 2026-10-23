# SnpDepotManagement

Hệ thống quản lý depot container vỏ rỗng, hỗ trợ theo dõi container từ khi đăng ký, nhập cổng, lưu và điều chuyển trong bãi đến khi cấp phát theo lệnh giao và xuất cổng.

Ứng dụng gồm giao diện Angular, API .NET và cơ sở dữ liệu PostgreSQL. Dữ liệu nghiệp vụ được đọc và ghi thông qua API; quyền truy cập được kiểm soát theo vai trò và depot của người dùng.

## Chức năng chính

- **Tổng quan:** theo dõi tồn bãi, sức chứa, lệnh giao và hoạt động cổng.
- **Quản lý container:** đăng ký hồ sơ, kích thước, chủng loại, tình trạng và lịch sử từng lượt vào/ra.
- **Điều phối bãi:** quản lý block vật lý, block ảo, vị trí xếp container và điều chuyển trong bãi.
- **Lệnh giao và giữ chỗ:** quản lý số lượng theo kích thước/chủng loại, gán container và theo dõi tiến độ giao.
- **Vận hành cổng:** kiểm tra điều kiện nhập/xuất, ghi nhận giao dịch, chỉnh sửa và hủy theo quy tắc nghiệp vụ.
- **Danh mục:** quản lý khách hàng, hãng khai thác, mã chủ sở hữu, phương tiện và chuyến tàu.
- **Báo cáo:** tồn bãi, thời gian lưu bãi, lịch sử di chuyển, lệnh sắp hết hạn và xuất Excel.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Frontend | Angular 21, TypeScript, SCSS |
| Backend | .NET 10, ASP.NET Core Minimal APIs |
| Truy cập dữ liệu | Entity Framework Core, Npgsql |
| Cơ sở dữ liệu | PostgreSQL 17 |
| Cache | HybridCache, Redis |
| Xác thực và phân quyền | JWT, role-based authorization |
| Kiểm thử | xUnit, Testcontainers, Vitest, Playwright |
| Hạ tầng | Docker Compose |

## Kiến trúc và cấu trúc mã nguồn

Backend tổ chức theo **Clean Architecture**, chia các ca sử dụng theo feature và tách thao tác đọc/ghi bằng **CQRS nhẹ**. Entity và value object biểu diễn nghiệp vụ; business rule kiểm tra điều kiện vận hành. Dependency injection kết nối các abstraction với triển khai hạ tầng.

Frontend tổ chức theo từng màn chức năng. Component phụ trách giao diện, facade điều phối dữ liệu và trạng thái, API client giao tiếp với backend, mapper chuyển DTO sang dữ liệu hiển thị.

```text
frontend/             Giao diện Angular
src/
  Api/                Endpoint HTTP, validation, authorization và ánh xạ lỗi
  Application/        Command/query handler, DTO và abstraction
  Domain/             Entity, value object, enum và business rule
  Infrastructure/     Persistence, migrations, seed, identity, tenancy và cache
tests/                Kiểm thử nghiệp vụ, kiến trúc và tích hợp
scripts/              Công cụ vận hành và kiểm chứng
docs/                 Tài liệu thiết kế, kỹ thuật và vận hành
```

Domain độc lập với các tầng còn lại. Application phụ thuộc Domain; Infrastructure triển khai các abstraction của Application; API lắp ghép dịch vụ và cung cấp giao diện HTTP.

## Luồng nghiệp vụ

Đăng ký container → nhập cổng → phân vị trí và lưu bãi → tạo lệnh giao, giữ chỗ → xuất cổng → theo dõi lịch sử và báo cáo.

Backend kiểm tra điều kiện về vị trí, sức đỡ, hãng khai thác, hạn lệnh và số lượng trước khi ghi nhận thay đổi. Lifecycle, vị trí bãi, booking, số lượng đã giao và lịch sử di chuyển được cập nhật theo từng nghiệp vụ.

## Vai trò người dùng

| Vai trò | Trách nhiệm chính |
| --- | --- |
| Admin | Quản trị tài khoản, danh mục và các nghiệp vụ |
| YardPlanner | Quy hoạch bãi, phân vị trí và điều chuyển container |
| OrderClerk | Quản lý lệnh giao và giữ chỗ container |
| GateClerk | Đăng ký container và thực hiện giao dịch cổng |

## Cài đặt và chạy dự án

### Yêu cầu

- Docker Desktop chạy **Linux containers**, có Docker Compose v2.
- Node.js **22.12 trở lên trong nhánh 22**; dùng npm **11.5.2** để cài dependency frontend.
- .NET SDK **10** nếu muốn chạy backend trực tiếp hoặc chạy bộ kiểm thử backend. Chạy backend bằng Docker không cần cài SDK trên máy.
- Các lệnh bên dưới dùng PowerShell tại thư mục gốc repository.

### 1. Chuẩn bị cấu hình

```powershell
git clone --branch Thai-DMS https://github.com/ngthais0607/TechSpherex.CleanArchitecture.git
cd TechSpherex.CleanArchitecture
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Nếu đã có mã nguồn thì mở terminal tại thư mục dự án, bỏ qua bước clone. Mở `.env` và điền các giá trị sau; các biến khác có thể giữ mặc định trong `.env.example`:

```dotenv
POSTGRES_PASSWORD=<mat-khau-database>
JWT_SECRET=<chuoi-ngau-nhien-it-nhat-32-ky-tu>
ADMIN_PASSWORD=<mat-khau-admin>
SEED_USER_PASSWORD=<mat-khau-cac-tai-khoan-mau>
SEED_DEMO_DATA=true
POSTGRES_PORT=5433
REDIS_PORT=6380
API_PORT=5000
```

Thay toàn bộ giá trị trong dấu `<...>` bằng giá trị của bạn. Giữ `ADMIN_TENANT_ID` ổn định cho depot. File `.env` chứa cấu hình riêng và được Git ignore. Với stack đã có dữ liệu, tiếp tục dùng đúng env-file đang sử dụng; thay biến password không tự đổi mật khẩu đã lưu trong database.

### 2. Chạy backend và hạ tầng

```powershell
docker compose --env-file .env up -d --build
docker compose --env-file .env ps -a
```

Docker Desktop hiển thị nhóm **`snp-depot`** với API, PostgreSQL và Redis. Tác vụ `migrate` áp dụng schema trước khi API khởi động, rồi dừng ở trạng thái **Exited (0)** khi thành công. Frontend chạy riêng trên Windows.

API mặc định tại `http://localhost:5000`; kiểm tra tiến trình bằng `http://localhost:5000/health/live`. Tài liệu API trong môi trường Development tại `http://localhost:5000/scalar/v1`.

Khi bật `SEED_DEMO_DATA=true`, backend tạo dữ liệu mẫu trong môi trường Development. Khởi động lại giữ dữ liệu đã thao tác và không nhân đôi dataset.

### 3. Chạy frontend

Mở terminal khác tại thư mục gốc:

```powershell
npx.cmd --yes npm@11.5.2 ci --prefix frontend
npm.cmd start --prefix frontend
```

Mở `http://localhost:4200`. Frontend gọi `/api` qua proxy đến API cổng 5000.

- Admin: tên đăng nhập theo `ADMIN_USERNAME` (mặc định `admin`), mật khẩu theo `ADMIN_PASSWORD`.
- Khi bật dữ liệu mẫu: `yard`, `order`, `gate` dùng mật khẩu `SEED_USER_PASSWORD`, với vai trò tương ứng.

Nếu đổi cổng API, cập nhật `frontend/proxy.conf.json` tương ứng. Nếu cổng trên máy đang được dùng, chọn cổng khác trong env-file trước khi chạy.

### 4. Dừng và chạy lại

```powershell
docker compose --env-file .env stop
docker compose --env-file .env up -d
```

Dừng frontend bằng `Ctrl+C` ở terminal chạy npm. Volume PostgreSQL giữ dữ liệu qua các lần dừng/khởi động; không dùng `down -v` nếu cần giữ dữ liệu.
