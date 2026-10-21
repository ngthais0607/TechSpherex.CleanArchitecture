# Tuần 3: Cấu Trúc Infrastructure, Api Và Giải Thích Kỹ Thuật

## 1. Phạm Vi

Tuần 3 xây dựng toàn bộ tầng `Infrastructure` và `Api`:
- Tầng `Infrastructure`: Hiện thực hóa toàn bộ abstraction từ `Application` (EF Core, Npgsql PostgreSQL 17, Redis 7, HybridCache, JWT, PBKDF2, Multi-Tenancy, Seeder).
- Tầng `Api`: Đóng vai trò là Composition Root, thiết lập HTTP pipeline (Middleware, Authentication, Authorization), tài liệu hóa API qua Scalar UI và phơi bày các Minimal API Endpoints.
- Tầng kiểm thử: `tests/Infrastructure.UnitTests` (kiểm thử đơn vị) và `tests/Infrastructure.IntegrationTests` (kiểm thử tích hợp thực tế trên Docker container PostgreSQL 17 qua `Testcontainers`).

```text
src/
├── Infrastructure/
│   ├── Caching/
│   │   └── HybridCacheService.cs         # ICacheService: HybridCache (.NET 9) + Redis L2
│   ├── Identity/
│   │   ├── CurrentUser.cs                # ICurrentUser trích xuất từ ClaimsPrincipal
│   │   ├── JwtOptions.cs                 # Cấu hình JWT (Secret, Issuer, Audience, Expiry)
│   │   ├── PasswordHasher.cs             # IPasswordHasher dùng PBKDF2 (100k vòng, SHA-256)
│   │   └── TokenService.cs               # ITokenService sinh JWT Access & Refresh Token
│   ├── Persistence/
│   │   ├── AppDbContext.cs               # DbContext, Global Query Filter, Auto-Audit/Tenant
│   │   ├── AppDbContextFactory.cs        # IDesignTimeDbContextFactory cho dotnet-ef CLI
│   │   ├── Configurations/               # 13 Fluent API Entity Configurations
│   │   │   ├── Containers/
│   │   │   ├── Identity/
│   │   │   ├── MasterData/
│   │   │   ├── Operations/
│   │   │   └── Yard/
│   │   ├── Migrations/                   # EF Core InitialCreate migration
│   │   └── Seed/
│   │       └── AppDbSeeder.cs            # IAppDbSeeder seed 4 roles và default Admin user
│   ├── Tenancy/
│   │   ├── TenantMiddleware.cs           # Middleware trích xuất header X-Tenant-Id
│   │   └── TenantProvider.cs             # ITenantProvider quản lý tenant context
│   └── DependencyInjection.cs            # Đăng ký dịch vụ Infrastructure vào DI
└── Api/
    ├── Endpoints/
    │   ├── ContainerEndpoints.cs         # GET /api/containers, POST /api/containers
    │   └── IdentityEndpoints.cs          # POST /api/auth/login, refresh, register
    ├── Program.cs                        # WebApplication pipeline, Auth, Scalar, Seeding
    └── appsettings.json                  # Cấu hình DB, Redis, JWT và Admin ban đầu

tests/
├── Infrastructure.UnitTests/             # Unit tests cho Tenant, Filter, Cache, Token, Hasher, Seeder
└── Infrastructure.IntegrationTests/      # Integration tests trên PostgreSQL 17 Testcontainers
```

---

## 2. Nhiệm Vụ Từng Thành Phần Trong Infrastructure

### 2.1. Đa Người Thuê (Multi-Tenancy) & Cô Lập Dữ Liệu

- **`TenantProvider` & `TenantMiddleware`:**
  - `TenantMiddleware` đọc header `X-Tenant-Id` từ mỗi HTTP request và nạp vào `TenantProvider` (scoped).
  - `TenantProvider` cung cấp `GetCurrentTenantId()` cho toàn bộ tầng ứng dụng và hạ tầng.
- **Global Query Filter:**
  - Trong `AppDbContext.OnModelCreating`, mọi thực thể kế thừa `ITenantEntity` được tự động cấu hình bộ lọc toàn cục:
    ```csharp
    builder.Entity<T>().HasQueryFilter(e => _tenantProvider.GetCurrentTenantId() == null || e.TenantId == _tenantProvider.GetCurrentTenantId());
    ```
  - Khi truy vấn qua `IAppDbContext`, lập trình viên không cần thêm `.Where(x => x.TenantId == ...)` thủ công, loại bỏ rủi ro rò rỉ dữ liệu giữa các depot.
- **Tự động điền Tenant và Audit:**
  - Trong `SaveChangesAsync`, mọi thực thể thêm mới (`EntityState.Added`) được tự động gán `TenantId` từ tenant context hiện tại nếu chưa được chỉ định.
  - Các trường audit (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`) được tự động cập nhật thời gian UTC thông qua `TimeProvider`.

### 2.2. Persistence & Entity Configurations

Toàn bộ 13 thực thể Domain được ánh xạ qua `IEntityTypeConfiguration<T>` riêng biệt, lưu trữ theo quy ước `snake_case`, thời gian `timestamptz` (UTC) và enum dạng chuỗi:

| Nhóm Thực Thể | Thực Thể | Cấu Hình Đặc Thù |
|---|---|---|
| **Identity** | `ApplicationUser`, `Role`, `UserRole` | Unique index trên `email`, `user_name`, `name`. Khóa ngoại liên kết qua bảng trung gian `user_roles`. |
| **MasterData** | `Customer`, `LineOperator`, `Voyage`, `Vehicle` | Unique index theo mã nghiệp vụ kết hợp `TenantId` (mỗi depot quản lý danh mục riêng). |
| **Containers** | `Container` | ValueConverter chuyển đổi hai chiều giữa Value Object `ContainerNumber` và cột chuỗi 11 ký tự ISO 6346. Unique index composite: `(tenant_id, container_number)`. |
| **Yard** | `YardBlock`, `YardSlot` | `YardSlot` chứa owned entity `YardPosition` ánh xạ chung bảng `yard_slots` (các cột `position_block`, `position_bay`, `position_row`, `position_tier`). Composite unique index theo vị trí toạ độ. |
| **Operations** | `DeliveryOrder`, `DeliveryOrderItem`, `ContainerLifecycle` | Unique index trên `(tenant_id, order_number)`. Khóa ngoại `Restrict` giữa Container và Lifecycle. |

### 2.3. Kiểm Soát Đồng Thời Lạc Quan (Optimistic Concurrency với `xmin`)

- **Vấn đề giải quyết:** Tránh tình trạng hai xe nâng cùng gắp container vào một ô slot trống (Double-booking), hoặc hai nhân viên cổng cùng xuất rỗng một container khi quota lệnh giao nhận chỉ còn 1.
- **Cơ chế triển khai:**
  - PostgreSQL cung cấp cột hệ thống `xmin` kiểu dữ liệu nội bộ `xid` (thay đổi tự động mỗi khi hàng dữ liệu được ghi nhận commit).
  - Ánh xạ qua EF Core dưới dạng shadow property:
    ```csharp
    builder.Property<uint>("xmin")
        .HasColumnType("xid")
        .ValueGeneratedOnAddOrUpdate()
        .IsConcurrencyToken();
    ```
  - Khi hai giao dịch đọc cùng một snapshot và cùng cập nhật, giao dịch thứ hai bị PostgreSQL phát hiện xung đột và EF Core ném ra ngoại lệ `DbUpdateConcurrencyException`.

### 2.4. Bảo Mật & Danh Tính (Identity & Security)

- **`PasswordHasher`:**
  - Sử dụng giải pháp tiêu chuẩn PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`) với 100,000 vòng lặp, thuật toán `SHA-256`, muối ngẫu nhiên 16-byte (128-bit) và khóa 32-byte (256-bit).
  - Chuỗi mã hóa dạng `{iterations}.{salt_base64}.{hash_base64}`. Xác minh mật khẩu dùng `CryptographicOperations.FixedTimeEquals` chống tấn công kênh phụ đo thời gian (side-channel timing attack).
- **`TokenService`:**
  - Kiểm tra nghiêm ngặt JWT Secret lúc khởi động: Bắt buộc độ dài tối thiểu 32 ký tự (256-bit).
  - Tạo Access Token với đầy đủ claims: `sub`, `nameid`, `unique_name`, `email`, `role`, `jti`.
  - Refresh Token được tạo ngẫu nhiên bằng bộ sinh số an toàn mật mã học (`RandomNumberGenerator`, 64-byte).

### 2.5. Caching Hai Tầng (HybridCache + Redis)

- Triển khai thư viện chuẩn .NET 9 `Microsoft.Extensions.Caching.Hybrid`.
- Kết hợp bộ nhớ đệm tiến trình L1 In-Memory với bộ nhớ phân tán L2 Redis:
  - Tự động gán tiền tố theo depot: `tenant:{tenantId}:{key}` (hoặc `global:{key}` cho dữ liệu hệ thống).
  - Hỗ trợ phân cấp tags khi ghi nhận cache entry. Khi gọi `RemoveByPrefixAsync`, hệ thống đồng thời kích hoạt `RemoveByTagAsync` của `HybridCache` và quét lệnh `SCAN` trên Redis thông qua `IConnectionMultiplexer` để dọn sạch toàn bộ keys cùng tiền tố.

### 2.6. Migration & Database Seeding

- **`AppDbContextFactory`:** Cung cấp context tại design-time cho công cụ `dotnet-ef`, cho phép sinh migration độc lập mà không cần khởi chạy toàn bộ Web host.
- **`AppDbSeeder`:**
  - Đảm bảo tính idempotent: Chỉ seed nếu vai trò hoặc tài khoản chưa tồn tại.
  - Seed 4 vai trò nghiệp vụ: `Admin`, `YardPlanner`, `OrderClerk`, `GateClerk`.
  - Khởi tạo tài khoản `admin` mặc định từ cấu hình `appsettings.json`.

---

## 3. Tầng Minimal API & Composition Root

Tầng `Api` hoàn thiện chu trình Clean Architecture:

1. **Dependency Injection:** Gọi `AddApplication()` và `AddInfrastructure()`.
2. **Middleware Pipeline:**
   - `TenantMiddleware`: Phân giải tenant từ HTTP Header `X-Tenant-Id`.
   - `AuthenticationMiddleware` & `AuthorizationMiddleware`: Kiểm tra JWT token và vai trò người dùng.
3. **Tài Liệu Hóa API Hiện Đại:**
   - Tích hợp OpenAPI và giao diện `Scalar.AspNetCore` tại đường dẫn `/scalar/v1`, mang lại trải nghiệm tra cứu và thử nghiệm API trực quan, đẹp mắt.
4. **Endpoints:**
   - **Nhóm Xác thực (`/api/auth`):**
     - `POST /api/auth/login`: Xác thực tài khoản và phát hành bộ token.
     - `POST /api/auth/refresh`: Cấp mới access token khi token cũ hết hạn.
     - `POST /api/auth/register`: Đăng ký người dùng mới (yêu cầu quyền `Admin`).
   - **Nhóm Quản lý Container (`/api/containers`):**
     - `GET /api/containers`: Xem danh sách container theo phân trang, tự động lọc theo tenant đang đăng nhập.
     - `POST /api/containers`: Đăng ký thông tin container mới vào kho bãi.

---

## 4. Kiểm Thử (Testing Strategy)

Hệ thống kiểm thử của dự án bao gồm 294 tests tự động, phân bổ theo các tầng:

```text
[Domain.UnitTests]           : 125 tests (Invariants, Value Objects, Domain Rules, State Machines)
[Application.UnitTests]      : 131 tests (CQRS Handlers, FluentValidation, Business Logic Mocking)
[Infrastructure.UnitTests]   :  24 tests (Tenant Filter, PasswordHasher, TokenService, Cache, Seeder)
[Infrastructure.Integration] :   7 tests (PostgreSQL 17 Testcontainers: Concurrency, Quota, Isolation, Rollback)
[Architecture.Tests]         :   7 tests (NetArchTest: Clean Architecture Layer Dependencies, Sealed Handlers)
---------------------------------------------------------------------------------------------------------
Tổng cộng                    : 294 tests (100% Passed)
```

### Điểm Sáng Trong Integration Tests (Testcontainers)

Thay vì chỉ kiểm thử qua `InMemoryDatabase` (vốn bỏ qua các đặc tính ACID, khóa ngoại quan hệ và hệ thống lock của RDBMS), dự án sử dụng `Testcontainers.PostgreSql` để dựng một container PostgreSQL 17 thật cho các tình huống quan trọng:
- **Slot Concurrency Test:** Chứng minh 2 giao dịch song song tranh chấp 1 slot thì đúng 1 giao dịch thành công, giao dịch còn lại nhận `DbUpdateConcurrencyException`.
- **Quota Concurrency Test:** Chứng minh 2 giao dịch tranh chấp quota container cuối cùng nhận ngoại lệ xung đột đồng thời.
- **Tenant Isolation Test:** Chứng minh dữ liệu giữa hai tenant không thể nhìn thấy nhau qua query filter, và hai depot khác nhau có thể lưu trùng số hiệu container nhờ index phân vùng theo tenant.
- **ACID & Rollback Test:** Chứng minh tính toàn vẹn của khóa ngoại `Restrict` và việc hoàn trả trạng thái ban đầu khi transaction rollback.

---

## 5. Từ Điển Thuật Ngữ Chuyên Ngành (Domain & Technical Glossary - Week 1, 2, 3)

Bảng tra cứu toàn diện các thuật ngữ chuyên ngành Cảng / Depot Logistics và các khái niệm kỹ thuật cốt lõi xuất hiện xuyên suốt codebase từ Tuần 1 đến Tuần 3.

### 5.1. Thuật Ngữ Nghiệp Vụ Cảng & Kho Bãi Container (Logistics & Depot Operations)

| Thuật Ngữ | Tên Tiếng Anh & Viết Tắt | Vị Trí Trong Codebase | Giải Thích Bản Chất & Ý Nghĩa Nghiệp Vụ |
|---|---|---|---|
| **Cảng cạn / Bãi container rỗng** | **Depot / ICD** *(Inland Container Depot)* | Toàn hệ thống, gắn với `Tenant` | Bãi cạn nội địa cách xa cảng biển, chuyên lưu giữ, cấp phát, hạ bãi, kiểm định (survey) và sửa chữa (M&R) container rỗng cho các hãng tàu và chủ hàng xuất nhập khẩu. |
| **Đơn vị thuê / Khai thác** | **Tenant** | `ITenantEntity`, `TenantProvider`, `TenantMiddleware` | Mỗi Depot (ví dụ: *Depot Cát Lái*, *Depot Sóng Thần*) là một Tenant độc lập. Dữ liệu giữa các Tenant được phân vùng và cô lập tuyệt đối. |
| **Số hiệu container (Chuẩn ISO 6346)** | **Container Number** *(ISO 6346)* | `ValueObjects/Containers/ContainerNumber.cs` | Chuỗi định danh chuẩn quốc tế gồm đúng 11 ký tự: **3 chữ cái đầu** (Mã hãng sở hữu), **1 chữ cái loại thiết bị** (`U`: freight container, `J`: thiết bị tháo rời, `Z`: rơ-moóc), **6 chữ số sê-ri**, và **1 chữ số kiểm tra (Check Digit)** tính theo thuật toán Modulo 11 để phát hiện sai sót khi gõ phím. |
| **Mã quy chuẩn container** | **ISO Code** *(ISO 6346 Size & Type)* | `Container.IsoCode` (ví dụ: `22G1`, `45G1`, `22R1`) | Mã 4 ký tự quy định kích thước và tính năng container: 2 số đầu là chiều dài/chiều cao (`22` = 20ft tiêu chuẩn, `42` = 40ft tiêu chuẩn, `45` = 40ft High Cube); 2 ký tự sau là chủng loại (`G1` = General Purpose hàng bách hóa, `R1` = Reefer cont lạnh). |
| **Trọng lượng vỏ rỗng (Trọng lượng bì)** | **Tare Weight** | `Container.TareWeightKg` | Khối lượng tịnh của riêng vỏ thùng container rỗng khi chưa chứa bất kỳ hàng hóa nào (tính bằng kg). |
| **Trọng lượng toàn tải tối đa** | **Max Gross Weight** | `Container.MaxGrossWeightKg` | Tổng khối lượng tối đa cho phép của cả vỏ thùng container cộng với hàng hóa đóng bên trong (thường khoảng 30.480 kg cho cont 20ft/40ft). |
| **Sức chở hàng cho phép** | **Payload** | Nghiệp vụ Domain (`MaxGrossWeight - TareWeight`) | Khối lượng hàng hóa thực tế tối đa được phép đóng vào trong container. |
| **Hãng tàu / Chủ khai thác** | **Line Operator / Carrier** | `Entities/MasterData/LineOperator.cs` | Doanh nghiệp vận tải đường biển quốc tế sở hữu hoặc thuê tàu và container (ví dụ: Maersk, CMA CGM, ONE, Evergreen, MSC...). |
| **Chuyến tàu** | **Voyage** | `Entities/MasterData/Voyage.cs` | Một lượt hải trình cụ thể của tàu biển cập và rời cảng (gồm tên tàu và số hiệu chuyến `VoyageNumber`). |
| **Khách hàng** | **Customer** | `Entities/MasterData/Customer.cs` | Chủ hàng xuất nhập khẩu (Shipper / Consignee) hoặc công ty dịch vụ giao nhận vận tải (Freight Forwarder). Quản lý định danh qua Mã số thuế 10 hoặc 13 chữ số. |
| **Phương tiện vận tải** | **Vehicle** | `Entities/MasterData/Vehicle.cs` | Xe đầu kéo (Tractor truck), rơ-moóc (Chassis/Trailer) đưa đón container ra vào cổng bãi depot. |
| **Khối bãi container** | **Yard Block** | `Entities/Yard/YardBlock.cs` | Khu vực phân chia trong bãi để quy hoạch xếp container. Gồm 2 loại: **Standard Block** (bãi chuẩn có các ô quy hoạch) và **Virtual Block** (khu vực bãi ảo dành cho bảo trì sửa chữa M&R hoặc kiểm định). |
| **Toạ độ 3D ô bãi** | **Yard Slot & Yard Position** | `YardSlot.cs`, `YardPosition.cs` | Vị trí không gian 3 chiều của một ô chứa container trong bãi, gồm 3 toạ độ: **Bay** (khoang dọc), **Row** (hàng ngang) và **Tier** (tầng cao). |
| **Khoang dọc (Chiều dài block)** | **Bay** | `YardPosition.Bay` | Vị trí theo chiều dài block bãi. Theo chuẩn logistics quốc tế: **Số lẻ** (`01, 03, 05...`) dành cho container 20ft; **Số chẵn** (`02, 06...`) là khoang đôi gộp 2 bay lẻ dành cho container 40ft. |
| **Hàng ngang (Chiều rộng block)** | **Row** | `YardPosition.Row` | Vị trí theo chiều ngang của dãy block (đánh số từ trong ra ngoài hoặc từ trái sang phải: `01, 02, 03...`). |
| **Tầng xếp chồng (Chiều cao)** | **Tier** | `YardPosition.Tier` | Vị trí theo phương thẳng đứng. `Tier 1` là tầng sát mặt đất, xếp chồng lên là `Tier 2, 3, 4, 5...`. |
| **Lệnh giao nhận vỏ (Lệnh cấp rỗng)** | **Delivery Order (DO)** | `Entities/Operations/DeliveryOrder.cs` | Chứng từ điện tử/giấy do Hãng tàu cấp cho chủ hàng/tài xế cho phép đến bãi depot nhận container rỗng đạt tiêu chuẩn để đóng hàng xuất khẩu. |
| **Hạn ngạch cấp vỏ** | **DO Quota** | `DeliveryOrderItem.QuantityRequested` & `QuantityAllocated` | Số lượng container tối đa theo từng chủng loại (ISO Code) được phép xuất theo lệnh DO. Khi số lượng đã cấp bằng hạn ngạch yêu cầu, lệnh tự động khóa quota. |
| **Vào cổng (Hạ bãi vỏ rỗng)** | **Gate-In** | `GateInContainerCommand.cs`, `ContainerLifecycle` | Quy trình tiếp nhận xe đầu kéo đưa container vào cổng bãi: kiểm tra số cont, khảo sát tình trạng hư hỏng, chỉ định ô bãi và hạ container xuống bãi. |
| **Ra cổng (Xuất bãi container)** | **Gate-Out** | `GateOutContainerCommand.cs`, `ContainerLifecycle` | Quy trình xe đầu kéo vào lấy container rỗng theo lệnh DO, gắp container lên xe, kiểm tra đối soát tại cổng và cho xe xuất bãi. |
| **Vòng đời lượt ra vào của container** | **Container Lifecycle** | `Entities/Operations/ContainerLifecycle.cs` | Lịch sử theo dõi một chu kỳ luân chuyển hoàn chỉnh của container tại bãi: bắt đầu từ lúc vào cổng (`GateIn`), lưu bãi (`InYard`), gắn với lệnh DO và kết thúc khi xuất cổng (`GatedOut`). |
| **Phẩm cấp chất lượng vỏ** | **Classification / Grade** | `Enums/ContainerClassification.cs` | Đánh giá chất lượng kỹ thuật vỏ container: **Grade A** (vỏ đẹp, kín nước kín sáng, sạch không mùi, đủ chuẩn đóng thực phẩm/điện tử), **Grade B** (đóng hàng bách hóa tổng hợp), **Grade C** (đóng hàng thô/nặng), **Scrapped** (vỏ nát, chờ thanh lý). |
| **Tình trạng kỹ thuật vỏ** | **Condition** | `Enums/ContainerCondition.cs` | Ghi nhận trạng thái hư hỏng vật lý của thùng: **Normal** (bình thường), **Dented** (móp méo), **Bent** (cong vênh đà/trụ), **Hole** (thủng vách/nóc), **Odor** (ám mùi hôi), **Swept** (đã quét vệ sinh rác). |
| **Quy tắc xếp bãi an toàn** | **Yard Business Rules** | `src/Domain/Rules/*.cs` | 8 quy tắc bắt buộc: không xếp vượt quá chiều cao tối đa của block (`Overcapacity`), không xếp tầng lơ lửng khi tầng dưới chưa có container (`UnsupportedTier`), không xếp container nặng đè lên container nhẹ (`WeightStacking`), không xếp container 40ft đè lên 20ft (`SizeMismatch`), container hỏng phải đưa về bãi ảo sửa chữa. |

---

### 5.2. Thuật Ngữ Kiến Trúc & Kỹ Thuật (.NET, EF Core & Database)

| Thuật Ngữ | Tên Tiếng Anh | Hiện Diện Trong Codebase | Giải Thích Bản Chất & Cơ Chế Hoạt Động |
|---|---|---|---|
| **Kiến trúc sạch** | **Clean Architecture** | Phân chia 4 dự án: `Domain`, `Application`, `Infrastructure`, `Api` | Mô hình phân lớp đồng tâm tuân thủ Dependency Inversion Principle: `Domain` độc lập hoàn toàn ở trung tâm, `Application` chứa use cases, `Infrastructure` hiện thực kết nối DB/bảo mật, và `Api` đóng vai trò giao tiếp bên ngoài. |
| **Thực thể gốc của tập hợp** | **Aggregate Root** | `YardBlock.cs`, `DeliveryOrder.cs` | Thực thể đóng vai trò cửa ngõ chính, chịu trách nhiệm bảo toàn tính toàn vẹn và bất biến (invariants) cho toàn bộ tập thực thể con nằm bên trong (ví dụ: `YardSlot` chỉ được thêm/sửa qua `YardBlock`). |
| **Đối tượng giá trị** | **Value Object** | `ContainerNumber`, `YardPosition` | Đối tượng bất biến (`record`), không có định danh `Id` riêng, được so sánh bình đẳng dựa trên toàn bộ giá trị thuộc tính bên trong nó. |
| **Mẫu kết quả** | **Result Pattern** | `Common/Result.cs`, `Error.cs` | Đóng gói đầu ra của hàm bằng `Result<T>` chứa dữ liệu hoặc lỗi nghiệp vụ `Error`. Thay thế hoàn toàn việc ném `Exception` trong luồng xử lý thông thường, giúp tiết kiệm CPU và code minh bạch. |
| **CQRS Tự Phát Triển** | **In-house CQRS** | `Abstractions/Messaging/*.cs` | Phân tách rành mạch lệnh Ghi (`Command` / `ICommandHandler`) và lệnh Đọc (`Query` / `IQueryHandler`) bằng generic interfaces tự viết, không phụ thuộc thư viện MediatR giúp tối ưu hiệu năng và kiểm soát luồng chạy. |
| **Bộ máy thẩm định quy tắc** | **Rule Engine** | `IRuleEngine`, `RuleEngine.cs` | Đánh giá danh sách các quy tắc nghiệp vụ `IBusinessRule` độc lập, dừng và trả về lỗi ngay khi quy tắc đầu tiên bị vi phạm. |
| **Cô lập đa người thuê** | **Tenant Isolation & Multi-Tenancy** | `TenantMiddleware`, `TenantProvider`, `ITenantEntity` | Khả năng phục vụ nhiều depot trên cùng một ứng dụng và database nhưng dữ liệu phân cách độc lập tuyệt đối. |
| **Bộ lọc truy vấn toàn cục** | **Global Query Filter** | `AppDbContext.OnModelCreating` | Tính năng của EF Core tự động chèn mệnh đề `WHERE tenant_id = @current_tenant` vào tất cả các câu lệnh SQL sinh ra, triệt tiêu nguy cơ lập trình viên quên filter gây rò rỉ dữ liệu giữa các depot. |
| **Kiểm soát đồng thời lạc quan** | **Optimistic Concurrency Control (OCC)** | `builder.Property<uint>("xmin").IsConcurrencyToken()` | Cơ chế chống tranh chấp ghi đồng thời (như 2 xe nâng cùng đặt container vào 1 slot, hoặc 2 nhân viên cùng xuất 1 suất quota DO cuối cùng). EF Core phát hiện qua sự thay đổi của cột hệ thống `xmin` trên PostgreSQL và ném `DbUpdateConcurrencyException`. |
| **Transaction ID của PostgreSQL** | **`xmin` (`xid`)** | PostgreSQL system column | Cột hệ thống ẩn có sẵn trong mọi hàng của PostgreSQL, lưu ID của transaction cập nhật dòng đó gần nhất. Được dự án dùng làm Concurrency Token nguyên bản mà không cần thêm cột `row_version` nhân tạo. |
| **Thuộc tính bóng** | **Shadow Property** | `Property<uint>("xmin")` | Thuộc tính được cấu hình trong EF Core model và ánh xạ vào database nhưng không khai báo thành property trong C# class. |
| **Thực thể sở hữu** | **Owned Entity** | `builder.OwnsOne(x => x.Position)` | Kiểu thực thể phụ thuộc của EF Core, được nhúng chung vào cùng một bảng database của thực thể cha (ví dụ: toạ độ `YardPosition` lưu chung trong bảng `yard_slots`). |
| **Chuyển đổi kiểu dữ liệu EF** | **Value Converter** | `HasConversion(v => v.Value, v => ContainerNumber.Create(v).Value)` | Cơ chế ánh xạ hai chiều giữa kiểu dữ liệu C# phức tạp (Value Object `ContainerNumber`) và cột kiểu dữ liệu nguyên thủy trong database (`VARCHAR(11)`). |
| **Hàm phái sinh khóa mật mã** | **PBKDF2** | `PasswordHasher.cs` (`Rfc2898DeriveBytes.Pbkdf2`) | Thuật toán băm mật khẩu chuẩn bảo mật cao: 100,000 vòng lặp SHA-256 kết hợp muối (Salt) 128-bit ngẫu nhiên và so sánh thời gian cố định (`CryptographicOperations.FixedTimeEquals`) chống tấn công đo thời gian (Timing Attack). |
| **Bộ nhớ đệm lai 2 tầng** | **HybridCache** | `HybridCacheService.cs` (.NET 9) | Kết hợp bộ nhớ đệm In-Memory siêu nhanh (L1) trong RAM tiến trình với bộ nhớ đệm Redis 7 phân tán (L2). Tự động phân vùng key theo tiền tố tenant và chống hiện tượng Cache Stampede. |
| **Kiểm thử trên container thực** | **Testcontainers** | `Testcontainers.PostgreSql` trong IntegrationTests | Thư viện khởi chạy container PostgreSQL 17 thật từ Docker trong suốt quá trình chạy `dotnet test`. Cho phép kiểm thử chính xác các hành vi ACID, khóa ngoại quan hệ, locking và concurrency mà database ảo InMemory không thể mô phỏng. |
| **Giao diện tài liệu API hiện đại** | **Scalar UI** | `Scalar.AspNetCore` (`/scalar/v1`) | Giao diện tài liệu tương tác OpenAPI thế hệ mới, thay thế Swagger UI với thiết kế hiện đại, tốc độ cao và hỗ trợ test endpoint trực quan. |

---

### 5.3. Ý Nghĩa & Trách Nhiệm Của Từng Thư Mục Trong Codebase (Directory Tree)

Hệ thống thư mục được tổ chức chuẩn hóa theo Clean Architecture và Domain-Driven Design (DDD):

```text
SnpDepotManagement/
├── src/
│   ├── Domain/                  # Tầng 1: Lõi nghiệp vụ (Domain Entities, Value Objects, Rules)
│   │   ├── Common/              # Lớp nền: BaseEntity, Result, Error, PagedResult, Audit
│   │   ├── Entities/            # Thực thể nghiệp vụ phân chia theo ngữ cảnh (Bounded Context)
│   │   │   ├── Containers/      # Thực thể container vỏ rỗng
│   │   │   ├── Identity/        # Người dùng, vai trò tài khoản
│   │   │   ├── MasterData/      # Danh mục nền tảng: Hãng tàu, Khách hàng, Chuyến tàu, Xe
│   │   │   ├── Operations/      # Lệnh giao nhận vỏ DO, Vòng đời container Lifecycle
│   │   │   └── Yard/            # Dãy bãi YardBlock, Ô vị trí YardSlot
│   │   ├── Enums/               # Danh sách trạng thái chuẩn: Condition, Classification, Status...
│   │   ├── Rules/               # Tập luật nghiệp vụ độc lập: UnsupportedTier, WeightStacking...
│   │   └── ValueObjects/        # Đối tượng giá trị bất biến: ContainerNumber, YardPosition
│   ├── Application/             # Tầng 2: Điều phối ca sử dụng (Use Cases & CQRS)
│   │   ├── Abstractions/        # Giao diện hợp đồng (Interfaces): Data, Caching, Identity, Tenancy
│   │   ├── Common/              # Hằng số Roles, Bộ máy đánh giá RuleEngine
│   │   └── Features/            # Phân nhóm tính năng theo lát cắt đứng (Vertical Slices)
│   │       ├── Containers/      # Commands & Queries xử lý container
│   │       ├── Identity/        # Đăng nhập, cấp mới token, quản lý tài khoản
│   │       ├── MasterData/      # Quản lý danh mục đối tác và phương tiện
│   │       ├── Operations/      # Xử lý lệnh DO, Gate-In, Gate-Out
│   │       └── Yard/            # Tạo block, tìm slot trống, điều chuyển container
│   ├── Infrastructure/          # Tầng 3: Triển khai kỹ thuật & Hạ tầng ngoài (Adapters)
│   │   ├── Caching/             # Hiện thực hóa HybridCacheService (.NET 9 + Redis)
│   │   ├── Identity/            # Hiện thực hóa TokenService (JWT), PasswordHasher (PBKDF2)
│   │   ├── Persistence/         # Hiện thực hóa Entity Framework Core & PostgreSQL
│   │   │   ├── Configurations/  # Fluent API Entity Configurations cho từng bảng DB
│   │   │   ├── Migrations/      # Lịch sử lược đồ cơ sở dữ liệu EF Core
│   │   │   └── Seed/            # Nạp dữ liệu mặc định ban đầu (Roles, Admin User)
│   │   └── Tenancy/             # Hiện thực hóa cô lập Tenant (TenantMiddleware, TenantProvider)
│   └── Api/                     # Tầng 4: Điểm tiếp nhận HTTP (Composition Root)
│       ├── Endpoints/           # Minimal API Endpoints (ContainerEndpoints, IdentityEndpoints)
│       └── Program.cs           # Khởi tạo DI container, HTTP Pipeline, Scalar UI, Seeding
└── tests/                       # Toàn bộ dự án kiểm thử tự động (294 tests)
    ├── Domain.UnitTests/        # Kiểm thử bất biến Entity, Value Object, Domain Rules
    ├── Application.UnitTests/   # Kiểm thử Handlers, FluentValidation, RuleEngine
    ├── Infrastructure.UnitTests/# Kiểm thử TenantFilter, TokenService, PasswordHasher, Cache
    ├── Infrastructure.IntegrationTests/ # Kiểm thử trên Docker PostgreSQL 17 (Testcontainers)
    └── Architecture.Tests/      # Kiểm thử quy tắc phụ thuộc Clean Architecture (NetArchTest)
```

---

### 5.4. Bảng Tra Cứu Tên Biến & Thuộc Tính Cốt Lõi (Core Properties & Variables)

Bảng đối chiếu ý nghĩa của các thuộc tính, tên biến và trường dữ liệu quan trọng nhất trong mã nguồn:

#### A. Nhóm Container & Thiết Bị (`Entities/Containers`)
| Tên Biến / Thuộc Tính | Kiểu Dữ Liệu | Ý Nghĩa Trong Codebase & Vận Hành Thực Tế |
|---|---|---|
| `ContainerNumber` | `ContainerNumber` (VO) | Số hiệu container 11 ký tự chuẩn ISO 6346 (vd: `TCLU1234567`). |
| `IsoCode` | `string` | Mã kích cỡ và chủng loại quốc tế (vd: `22G1` = 20ft thường, `45G1` = 40ft High Cube). |
| `TareWeightKg` | `int` / `decimal` | Khối lượng vỏ thùng rỗng không tải (tính bằng kilôgam). |
| `MaxGrossWeightKg` | `int` / `decimal` | Tổng khối lượng tối đa cho phép gồm vỏ và hàng hóa bên trong (kg). |
| `Payload` | `int` (Calculated) | Sức chứa hàng cho phép: `MaxGrossWeightKg - TareWeightKg`. |
| `Status` | `ContainerStatus` (Enum) | Trạng thái hiện tại của vỏ: `InYard` (đang lưu bãi), `Allocated` (đã gắn vào lệnh DO), `GatedOut` (đã xuất khỏi cổng). |
| `Condition` | `ContainerCondition` (Enum) | Tình trạng kỹ thuật vỏ thùng: `Normal` (tốt), `Dented` (móp), `Bent` (cong vênh), `Hole` (thủng), `Odor` (ám mùi), `Swept` (sạch). |
| `Classification` | `ContainerClassification` (Enum) | Phẩm cấp chất lượng: `GradeA` (chuẩn thực phẩm/hàng cao cấp), `GradeB` (bách hóa), `GradeC` (hàng thô), `Scrapped` (phế liệu). |
| `LineOperatorId` | `Guid` | Khóa ngoại liên kết tới Hãng tàu sở hữu vỏ container. |
| `CurrentBlockId` | `Guid?` | Id của dãy bãi (Yard Block) mà container đang được đặt (null nếu đang ở ngoài bãi). |
| `CurrentSlotId` | `Guid?` | Id của ô vị trí toạ độ bãi (Yard Slot) mà container đang chiếm giữ. |

#### B. Nhóm Mặt Bằng & Toạ Độ Bãi (`Entities/Yard`)
| Tên Biến / Thuộc Tính | Kiểu Dữ Liệu | Ý Nghĩa Trong Codebase & Vận Hành Thực Tế |
|---|---|---|
| `Code` (YardBlock) | `string` | Ký hiệu định danh dãy bãi (ví dụ: Block `"A"`, `"B"`, `"M&R"`). |
| `BlockType` | `BlockType` (Enum) | Phân loại block: `Standard` (bãi tiêu chuẩn chia ô) hoặc `Virtual` (bãi ảo sửa chữa/giám định). |
| `TotalBays` | `int` | Tổng số khoang dọc theo chiều dài của dãy block. |
| `TotalRows` | `int` | Tổng số hàng ngang theo chiều rộng của dãy block. |
| `TotalTiers` | `int` | Chiều cao tối đa cho phép xếp chồng container (tổng số tầng). |
| `MaxCapacityTeu` | `int` | Sức chứa tối đa của block tính theo đơn vị TEU (Twenty-foot Equivalent Unit: 1 cont 20ft = 1 TEU, 1 cont 40ft = 2 TEU). |
| `Position` | `YardPosition` (Owned VO) | Đối tượng toạ độ 3 chiều của một ô bãi gồm: `Bay`, `Row`, `Tier`. |
| `Bay` | `int` | Khoang dọc: số lẻ (`01, 03...`) cho cont 20ft, số chẵn (`02, 06...`) cho cont 40ft. |
| `Row` | `int` | Số thứ tự hàng ngang tính từ lề đường vào trong (`01, 02...`). |
| `Tier` | `int` | Số thứ tự tầng xếp: `1` là tầng sát mặt đất, `2, 3...` là các tầng phía trên. |
| `IsOccupied` | `bool` | Cờ đánh dấu ô slot đã có container đặt bên trong hay chưa (`true` = đã chiếm chỗ, `false` = còn trống). |
| `CurrentContainerId` | `Guid?` | Id của container đang được đặt tại ô slot này (null nếu ô đang trống). |
| `xmin` | `uint` (Shadow Property) | Mã transaction ID hệ thống của PostgreSQL (`xid`), dùng làm token kiểm soát đồng thời lạc quan chống double-booking slot. |

#### C. Nhóm Điều Hành Lệnh & Cổng (`Entities/Operations`)
| Tên Biến / Thuộc Tính | Kiểu Dữ Liệu | Ý Nghĩa Trong Codebase & Vận Hành Thực Tế |
|---|---|---|
| `OrderNumber` | `string` | Số chứng từ lệnh giao nhận vỏ DO do Hãng tàu cấp (vd: `DO-2026-00123`). |
| `ExpiryDate` | `DateTime` / `Instant` | Thời điểm hết hạn của lệnh DO. Quá hạn hệ thống sẽ chặn không cho cấp container. |
| `QuantityRequested` | `int` | Số lượng container tối đa hãng tàu cho phép lấy theo từng dòng chủng loại ISO Code (Quota). |
| `QuantityAllocated` | `int` | Số lượng container thực tế nhân viên bãi đã chọn và xuất cho khách hàng. |
| `GateInTime` / `GateOutTime` | `DateTimeOffset?` | Thời điểm chính xác xe đầu kéo qua cổng vào bãi / rời khỏi bãi (lưu theo chuẩn UTC). |
| `GateInVehicleId` / `GateOutVehicleId` | `Guid?` | Id của phương tiện vận tải (xe đầu kéo/rơ-moóc) thực hiện chở container lúc vào/ra. |
| `GateInDriverName` / `GateOutDriverName` | `string` | Họ tên tài xế xe đầu kéo trực tiếp lái xe nhận/trả container tại cổng. |
| `DeliveryOrderId` | `Guid?` | Khóa ngoại gắn container với lệnh DO được sử dụng trong lượt xuất bãi đó. |

#### D. Nhóm Phân Quyền, Danh Tính & Đa Người Thuê (`Identity` & `Tenancy`)
| Tên Biến / Thuộc Tính | Kiểu Dữ Liệu | Ý Nghĩa Trong Codebase & Vận Hành Thực Tế |
|---|---|---|
| `TenantId` | `string` | Mã định danh đơn vị thuê/quản lý depot (ví dụ: `"DEPOT-CATLAI"`). Áp dụng vào mọi thực thể `ITenantEntity`. |
| `X-Tenant-Id` | `Header Key` | Tên HTTP Header client gửi lên trong mỗi request để `TenantMiddleware` bóc tách xác định depot hiện tại. |
| `UserName` | `string` | Tên tài khoản đăng nhập của người dùng trong hệ thống (vd: `"admin"`, `"planner_01"`). |
| `Email` | `string` | Địa chỉ email người dùng, định danh duy nhất trong hệ thống. |
| `PasswordHash` | `string` | Chuỗi mật khẩu đã được băm an toàn qua PBKDF2 theo định dạng `{iterations}.{salt_base64}.{hash_base64}`. |
| `RefreshTokenHash` | `string` | Mã băm của Refresh Token dùng để đối soát cấp lại Access Token mới. |
| `RefreshTokenExpiryTime` | `DateTimeOffset` | Thời điểm hết hạn của Refresh Token. |
| `Roles` | `List<string>` / `Enum` | Danh sách 4 vai trò nghiệp vụ: `Admin`, `YardPlanner` (điều độ bãi), `OrderClerk` (nhân viên thủ tục lệnh DO), `GateClerk` (nhân viên cổng bãi). |
| `CreatedAt` / `LastModifiedAt` | `DateTimeOffset` | Thời điểm tạo bản ghi và thời điểm cập nhật bản ghi gần nhất (tự động cập nhật qua `TimeProvider` UTC). |
| `CreatedBy` / `LastModifiedBy` | `string?` | Tên tài khoản người dùng thực hiện thao tác tạo hoặc cập nhật bản ghi đó. |
