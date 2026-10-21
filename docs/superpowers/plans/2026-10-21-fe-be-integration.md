# FE–BE Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Kết nối Angular hiện có với BE .NET bằng dữ liệu thật, sửa seed/hạ tầng, nghiệm thu từng luồng và bàn giao tài liệu gọn, đúng thực tế.

**Architecture:** Page → feature facade → API client → DTO/mapper; BE giữ toàn bộ rule và dữ liệu nghiệp vụ. Giữ bản demo bằng Git tag, không thêm runtime offline/API kép. Sửa hạ tầng và seed trước, rồi bổ sung hợp đồng đọc còn thiếu và nối các feature phụ thuộc nhau.

**Tech Stack:** .NET 10, EF Core/Npgsql, PostgreSQL 17, Angular 21, Signals, RxJS, xUnit, Testcontainers, Vitest, Playwright, ExcelJS; giữ dependency hiện có.

**Spec:** `docs/superpowers/specs/2026-10-21-fe-be-integration-design.md` (đọc cả spec trước khi thực hiện).

## Global Constraints

- Giữ giao diện navy/cyan, cấu trúc Clean Architecture và lazy routes hiện có; không triển khai production hoặc xóa DB người dùng.
- Demo chỉ chạy khi môi trường Development và `Seed:DemoData=true`; bootstrap admin không phụ thuộc bật demo.
- Dataset version `fe-be-v1`: 50 đăng ký, 20 trong bãi (15 thật, 5 ảo), 3 đã xuất, 27 chưa nhập; 23 gate-in, 3 gate-out.
- Container 35–36 chưa Reserved trước gate-in; giữ rule `Lifecycle.NotInYard`. Đây là hiệu chỉnh spec được trình cùng kế hoạch.
- FE `http://localhost:4200`, Docker API `http://localhost:5000`, host API thay thế `http://localhost:5187`; FE dùng `/api` qua proxy.
- Phân trang bắt đầu 1, pageSize không quá 100; debounce 300ms, refresh sớm 30 giây, cache lookup tối đa 1 giờ.
- Không gửi `X-Tenant-Id`, không refresh login/refresh, không tự retry command sau timeout/5xx.
- Token ở sessionStorage cho đợt tích hợp; secrets từ config, không commit `.env`, password/token hoặc log chứa chúng.
- Quyền lấy từ endpoint policy hiện có; không mở rộng quyền BE để tránh sửa FE.
- Giữ báo cáo hằng ngày và tài liệu cần thiết; chỉ dọn Markdown sau nghiệm thu, giữ khả năng phục hồi file chưa được Git theo dõi.
- Không thêm thư viện state, generic repository hoặc index/cache mới nếu chưa có bằng chứng cần thiết.

## Review Focus

1. Hai API khởi động lần đầu đồng thời: bootstrap và master data toàn cục không bị duplicate; task 4 kiểm tra cả hai cùng thành công.
2. Lỗi giữa seed hoặc marker thiếu nhưng còn mã cũ: không để lại dữ liệu một phần hoặc sửa dữ liệu người dùng; task 4 kiểm rollback/conflict.
3. Storage hỏng/bị từ chối và logout giữa refresh: không crash, không khôi phục phiên cũ; task 8 kiểm trực tiếp.
4. Search cũ trả muộn, URL chứa page NaN/quá lớn hoặc enum lạ: UI không hiển thị dữ liệu cũ và không gửi query sai; task 10 kiểm trực tiếp.
5. HTTP 200 assign một phần và export bị đổi dữ liệu/đứt mạng giữa trang: không báo hoàn tất sai hoặc tải file thiếu; task 11 và 15 kiểm trực tiếp.

## Quy ước thực hiện, file và verification

Các task phụ thuộc nhau theo thứ tự; task 12 và 13 phải có task 11 trước. Mỗi task có test cycle và commit riêng. Spec và plan được giữ đến bàn giao. Thực hiện trong worktree theo skill using-git-worktrees tại thời điểm bắt đầu; kiểm tra trạng thái Git trước khi tạo để không mất thay đổi đang có.

Các đường dẫn bên dưới tính từ root repository. `GetContainers/` và `GetDeliveryOrders/` chỉ các thư mục đầy đủ ghi tại task 5/6; các tên DTO mới do task sở hữu định nghĩa, DTO existing phải sao chép đúng shape backend, không tạo `any`. Migration có timestamp do EF sinh, không bịa tên timestamp hoặc sửa migration cũ.

Chạy .NET từ root bằng `dotnet test <project-path> --filter FullyQualifiedName~<TestClass>`; project-path là `tests/<Project>/SnpDepotManagement.<Project>.csproj`. Chạy FE từ `frontend/` bằng `npm.cmd test -- --include=<src/app/...spec.ts>`; mọi lệnh chọn file phải thực sự discover ít nhất một test. Nếu CLI đang cài không nhận include, dùng `npm.cmd test` và ghi nhận số test, không đổi runner. Không coi “0 tests” là pass.

Test mẫu inline dưới đây là assertion bắt buộc trong test có tên đã chỉ định; dựng fixture theo các fixture hiện có và dùng timestamp UTC cố định. Mọi verification phải exit 0, không skip integration test hoặc thay PostgreSQL bằng InMemory. Chỉ chạy suite rộng ở task 16, hoặc sớm hơn nếu thay đổi ảnh hưởng liên tầng. Tài liệu/config không cần test unit mô phỏng nội dung; kiểm bằng công cụ và smoke thực tế.

Các lệnh chọn test cụ thể (chạy cùng lệnh build/smoke được ghi trong task):

| Task | Lệnh test chính |
| --- | --- |
| 1 | `dotnet test tests/Api.IntegrationTests/SnpDepotManagement.Api.IntegrationTests.csproj --filter FullyQualifiedName~DevelopmentTransportTests` |
| 2 | `dotnet test tests/Infrastructure.IntegrationTests/SnpDepotManagement.Infrastructure.IntegrationTests.csproj --filter FullyQualifiedName~DemoSeedSchemaTests` |
| 3 | `dotnet test tests/Infrastructure.IntegrationTests/SnpDepotManagement.Infrastructure.IntegrationTests.csproj --filter FullyQualifiedName~DemoDatasetTests` |
| 4 | `dotnet test tests/Infrastructure.IntegrationTests/SnpDepotManagement.Infrastructure.IntegrationTests.csproj --filter FullyQualifiedName~DemoSeedStartupTests` |
| 5 | `dotnet test tests/Api.IntegrationTests/SnpDepotManagement.Api.IntegrationTests.csproj --filter "FullyQualifiedName~ContainerFilterTests\|FullyQualifiedName~ContainerSummaryTests"` |
| 6 | `dotnet test tests/Api.IntegrationTests/SnpDepotManagement.Api.IntegrationTests.csproj --filter "FullyQualifiedName~OrderReadContractTests\|FullyQualifiedName~RateLimitTests\|FullyQualifiedName~EndpointMetadataTests"` |
| 7 | `npm.cmd test -- --include=src/app/core/api/*.spec.ts` |
| 8 | `npm.cmd test -- --include=src/app/core/auth/*.spec.ts` |
| 9 | `npm.cmd test -- --include=src/app/core/api/reference-data.spec.ts` rồi route-permissions.spec.ts bằng include tương tự |
| 10 | `npm.cmd test -- --include=src/app/features/containers/*.spec.ts` |
| 11 | `npm.cmd test -- --include=src/app/features/delivery-orders/orders-facade.spec.ts` |
| 12 | `npm.cmd test -- --include=src/app/features/gate/gate-facade.spec.ts` |
| 13 | `npm.cmd test -- --include=src/app/features/yard/yard-facade.spec.ts` |
| 14 | `npm.cmd test -- --include=src/app/features/partners/partners-facade.spec.ts` |
| 15 | `npm.cmd test -- --include=src/app/features/reports/reports-facade.spec.ts` và `npm.cmd test -- --include=src/app/core/export/*.spec.ts` |

Ký tự `\|` trong ô Markdown chỉ để hiển thị dấu OR `|` của filter; khi chạy PowerShell dùng dấu `|` bên trong chuỗi quote, không thêm backslash. Với test unit seed bị ảnh hưởng, chạy `dotnet test tests/Infrastructure.UnitTests/SnpDepotManagement.Infrastructure.UnitTests.csproj --filter "FullyQualifiedName~AppDbSeederTests\|FullyQualifiedName~DemoDataSeederTests"` theo cùng quy ước.

## Task 1: Môi trường dev khởi động được và giữ bản demo

**Files:** Modify `docker-compose.yml`, `.env.example`, `src/Api/Program.cs`, `src/Api/Properties/launchSettings.json`; Create `scripts/smoke-development.ps1`, `tests/Api.IntegrationTests/Fixtures/DevelopmentApiFactory.cs`; Test `tests/Api.IntegrationTests/Endpoints/DevelopmentTransportTests.cs`. Factory mới dùng Development với DB riêng; không đổi ApiFactory Production đang bảo vệ regression production.

**Interfaces:** Consumes target `migrate` và `scripts/release-db.sh`; produces `smoke-development.ps1 -Project <name> -ApiPort <port>` dùng DB/volumes riêng và exit khác 0 khi health/login/query/restart thất bại. Chỉ tạo project name bắt đầu `snp-integration-`; không gọi down/remove volume của project khác. Tránh container_name cố định trong compose để project isolation có tác dụng.

- [x] Xác nhận tag `demo-offline-2026-10-21` chưa tồn tại hoặc trỏ đúng bản demo; tạo annotated tag tại commit nền trước feature API, không ghi đè tag. Ghi SHA vào báo cáo ngày; không push tag khi chưa có yêu cầu.
- [x] Viết `HttpDevelopment_DoesNotRedirectPreflight`: `response.StatusCode.Should().Be(HttpStatusCode.NoContent); response.Headers.Location.Should().BeNull();` với OPTIONS đúng origin; thêm kiểm GET http không 307 và lệnh smoke login/query hiện thất bại do config/migration thiếu.
- [x] Chạy test lớp DevelopmentTransportTests và smoke trên project riêng để ghi failure gốc. Nếu thiếu SDK/Docker ghi blocker, không báo tái hiện thành công.
- [x] Thêm migrate dependency, cấu hình DB/JWT/admin/seed/depot/CORS đầy đủ, environment Development, API_PORT và POSTGRES_PORT override. `.env.example` không có secret dùng được; compose dùng required env cho secret. Thêm `Transport:UseHttpsRedirection` mặc định false cho dev HTTP, profile https bật true.
- [x] Chạy lại test và `powershell -NoProfile -File scripts/smoke-development.ps1 -Project snp-integration-dev -ApiPort 15000`; kỳ vọng health 200, admin login/me, containers 200 sau migrate và API restart không mất dữ liệu. Smoke tạo secrets ngẫu nhiên trong file ignored, không in token hoặc output compose đã nội suy secrets.
- [x] Commit `fix: make development stack boot with migrations and explicit config` bằng cách stage đúng các file task này.

## Task 2: Marker seed và test scaffold PostgreSQL

**Files:** Create `src/Infrastructure/Persistence/Seed/DemoSeedRun.cs`, `src/Infrastructure/Persistence/Configurations/Seed/DemoSeedRunConfiguration.cs`, `tests/Infrastructure.IntegrationTests/Persistence/DemoSeedSchemaTests.cs`; Modify `src/Infrastructure/Persistence/AppDbContext.cs`; Generate migration `AddDemoSeedRuns` trong `src/Infrastructure/Persistence/Migrations/`.

**Interfaces:** `DemoSeedRun` là entity hạ tầng, constructor `(string tenantId, string datasetVersion, DateTime completedAtUtc)`, properties chỉ đọc `TenantId`, `DatasetVersion`, `CompletedAtUtc`; `AppDbContext.DemoSeedRuns` là `DbSet<DemoSeedRun>`. Composite key `(TenantId, DatasetVersion)`, table `demo_seed_runs`; query seed luôn chỉ định tenant, không đưa bảng này vào application abstraction.

- [x] Viết `DuplicateDatasetVersionInSameTenant_IsRejected` và `SameVersionInDifferentTenant_IsAllowed`: assert `DbUpdateException` ở trùng khóa và count 2 ở hai tenant; dùng PostgreSqlFixture, schema/DB cô lập.
- [x] Chạy lớp DemoSeedSchemaTests, xác nhận đỏ vì thiếu entity/schema.
- [x] Tạo entity/config/DbSet và chạy `dotnet ef migrations add AddDemoSeedRuns --project src/Infrastructure --startup-project src/Api` với cấu hình design-time hợp lệ, đọc migration để bảo đảm chỉ thêm bảng marker.
- [x] Chạy test lại và Architecture.Tests; kỳ vọng xanh, không sửa migration đã tồn tại hoặc Domain reference ngược Infrastructure.
- [x] Commit `feat: track completed demo seed datasets by tenant and version`.

## Task 3: Manifest seed và lịch sử đúng thời điểm

**Files:** Create `src/Infrastructure/Persistence/Seed/DemoDataset.cs`, `src/Infrastructure/Persistence/Seed/DemoDatasetWriter.cs`, `tests/Infrastructure.IntegrationTests/Persistence/DemoDatasetTests.cs`; Modify `src/Infrastructure/Persistence/Seed/DemoDataSeeder.cs`, `tests/Infrastructure.UnitTests/Persistence/DemoDataSeederTests.cs` khi thay cấu trúc dependency.

**Interfaces:** `DemoDataset.Version = "fe-be-v1"`; `DemoDataset.Containers` chứa 50 entry khai báo number/owner/operator/size/type/scenario. Chọn owner MSC/MED cho MSC và MAE/MSK cho MAEU; nhóm 23–34 dùng MSC Dry. `DemoDatasetWriter.WriteAsync(AppDbContext db, string tenantId, DateTime baselineUtc, CancellationToken ct): Task` ghi dataset trong transaction của caller, không tự commit transaction, không tự chọn tenant qua HttpContext. `SeedTimeProvider : TimeProvider` nằm private trong writer, có `SetUtcNow(DateTime utcNow): void` và override `GetUtcNow(): DateTimeOffset`, chỉ dùng bởi các context seed phụ.

- [x] Viết `Dataset_HasConsistentLifecycleBookingAndQuota`: `containers.Should().HaveCount(50); inYard.Should().HaveCount(20); shipped.Should().HaveCount(3); neverIn.Should().HaveCount(27);` và assert 15 physical/5 virtual, 23 gate-in/3 gate-out; container 35–36 không có booking; DO-005 đúng 3 Dry MSC Shipped.
- [x] Viết `HistoricalEvents_AgreeWithLifecycleDates`: assert mỗi gate-in ≤ placement < gate-out, transaction time khớp lifecycle, history có GateIn/Place/GateOut, owner/operator phù hợp, slots có support; chạy xác nhận đỏ trên seed hiện tại.
- [x] Dựng manifest bằng số ISO6346 xác định, không dùng generator ngẫu nhiên. Dựng gate-in trong block ảo trước, place 15 container vào slot tầng 1 hợp lệ của A, giữ 5 virtual; 3 container xuất có đủ lifecycle, reservation, gate-in và gate-out. DO-004 reservation trước expiry; không có booking Reserved sau xuất.
- [x] Giữ `AppDbContext.SaveChangesAsync` audit hiện hành. Vì SaveChanges ghi đè CreatedAt, writer dùng context phụ với TimeProvider điều khiển được, cùng connection/DbTransaction qua `UseTransaction`, đồng hồ riêng từng thao tác; không sửa audit global, reflection/private setter hoặc bịa OccurredAt trên FE. Tái sử dụng factories domain và kiểm `Result` trước khi dùng Value.
- [x] Chạy DemoDatasetTests trên PostgreSQL với baseline cố định và unit tests seed bị ảnh hưởng; kỳ vọng toàn bộ invariant xanh và history handler đọc được dữ liệu từ DB.
- [x] Commit `fix: build a deterministic and consistent demo lifecycle dataset`.

## Task 4: Điều phối seed an toàn, tài khoản role và khởi động đồng thời

**Files:** Modify `src/Infrastructure/Persistence/Seed/AppDbSeeder.cs`, `src/Infrastructure/Persistence/Seed/DemoDataSeeder.cs`, `src/Infrastructure/DependencyInjection.cs`; Create `src/Infrastructure/Persistence/Seed/DemoSeedCoordinator.cs`, `tests/Infrastructure.IntegrationTests/Persistence/DemoSeedStartupTests.cs`; Modify `tests/Infrastructure.UnitTests/Persistence/AppDbSeederTests.cs`, `tests/Infrastructure.UnitTests/Persistence/DemoDataSeederTests.cs`.

**Interfaces:** Giữ `IAppDbSeeder.SeedAsync(CancellationToken)` và `IDemoDataSeeder.SeedAsync(CancellationToken)`. `DemoSeedCoordinator.RunAsync(string tenantId, CancellationToken ct): Task` mở transaction nếu chưa có, lấy global master-data transaction lock rồi tenant/version lock theo thứ tự cố định, kiểm marker/conflict và gọi writer task 3. Bootstrap depot/role/admin cũng phải được tuần tự hóa, không chỉ khóa riêng demo; demo transaction độc lập sau bootstrap transaction. Thử lại bootstrap sau xung đột unique toàn cục bằng context sạch, không tiếp tục transaction PostgreSQL đã abort.

- [x] Viết `NullTenant_SecondContextSeedDoesNotDuplicate`, `ConcurrentFirstStartup_BothSucceed`, `FailureBeforeMarker_RollsBack`, `LegacyCodesWithoutMarker_RefusesToMutate`, `ProductionOrDisabled_NoDemoUsersOrData`: assert marker count 1, full dataset counts không đổi, hai startup thành công, failure không còn demo rows, Production chỉ có bootstrap.
- [x] Viết `ExistingDemoUserWrongTenantOrRole_IsConflict`: assert không đổi hash/tenant/role của user; mật khẩu demo thiếu ở Development+enabled báo config rõ. Chạy các test để xác nhận đỏ.
- [x] Tái sử dụng master global theo code, đọc tenant bằng IgnoreQueryFilters+Where, tạo yard/order/gate theo config password. Khi marker tồn tại bỏ dataset, không reset thay đổi vận hành; marker thiếu và có reserved demo codes thì fail với tên mã và hướng dẫn DB riêng, không ghi marker.
- [x] Dùng host environment và TimeProvider DI; không chạy code PostgreSQL lock trong unit InMemory, chuyển assertion transaction/idempotency sang integration thật. Lock trong test gồm hai tenant chia master data để bắt duplicate LineOperator; không đổi schema master data toàn cục.
- [x] Chạy DemoSeedStartupTests, AppDbSeederTests/DemoDataSeederTests và smoke task 1 với demo bật rồi restart API hai lần; kỳ vọng count/role đúng, không log secret.
- [x] Commit `fix: make demo startup atomic tenant explicit and repeatable`.

## Task 5: Filter, summary và sort container thống nhất phía BE

**Files:** Modify `src/Api/Endpoints/ContainerEndpoints.cs`, `src/Application/Features/Containers/Queries/GetContainers/GetContainersQuery.cs`, `GetContainersQueryHandler.cs` trong cùng thư mục; Modify `src/Application/Features/Containers/Queries/GetContainerSummary/GetContainerSummaryQuery.cs`, `GetContainerSummaryQueryHandler.cs` trong cùng thư mục; Create `src/Application/Features/Containers/Queries/ContainerReadFilter.cs`; Modify `tests/Api.IntegrationTests/Endpoints/ContainerSummaryTests.cs`; Create `tests/Api.IntegrationTests/Endpoints/ContainerFilterTests.cs`.

**Interfaces:** Query HTTP bổ sung `type`, `lineOperator`, `displayStatus`, `booking` (`assigned|unassigned`), `sort` (`number|size`), `descending` boolean; giữ parameter cũ, default sort tương thích khi không truyền. `ContainerReadFilter.Apply(IQueryable<Container> source, IAppDbContext db, ContainerFilter filter): IQueryable<Container>` được list/summary dùng chung; projection displayStatus giữ đúng semantics hiện có của BE (Reserved/InYard/OutOfYard/NeverIn).

- [x] Viết `FiltersApplyBeforePagination_AndSummaryIgnoresOnlyType`: assert toàn bộ page đều khớp operator/type/status/booking, totalCount khớp SQL count, summary ByType tính qua mọi trang; include owner alias MED→MSC, tenant khác không rò rỉ, nhiều size cùng thứ tự có khóa Id phụ.
- [x] Viết `InvalidSortOrEnum_Returns400` và `EmptyPage_KeepsTotalCount`; chạy ContainerFilterTests/ContainerSummaryTests xác nhận đỏ.
- [x] Bổ sung field optional ở cuối record để không phá positional callers cũ; filter dùng EXISTS/join server trước count/Skip/Take, projection theo page theo batch, không tải toàn bộ lifecycle trước phân trang. Summary bỏ mỗi type filter cho tab counts; áp dụng phần còn lại.
- [x] Chạy hai lớp HTTP test trên PostgreSQL và Application.UnitTests liên quan GetContainers/GetContainerSummary; đọc generated SQL/log query trong test để xác nhận không client-evaluate/N+1. Không thêm index nếu chưa đo.
- [x] Commit `feat: align container filters summaries and server pagination`.

## Task 6: API đọc booking, tìm lệnh và hợp đồng chuẩn

**Files:** Modify `src/Api/Endpoints/DeliveryOrderEndpoints.cs`, `src/Api/Endpoints/MasterDataEndpoints.cs`, `src/Api/Program.cs`; Modify `src/Application/Features/Operations/DeliveryOrders/Queries/GetDeliveryOrders/GetDeliveryOrdersQuery.cs`, `GetDeliveryOrdersQueryHandler.cs` trong cùng thư mục; Create `src/Application/Features/Operations/Bookings/Queries/GetOrderBookings/GetOrderBookingsQuery.cs`, `GetOrderBookingsQueryHandler.cs`, `OrderBookingsDto.cs` cùng thư mục; Modify `tests/Api.IntegrationTests/Authentication/RateLimitTests.cs`, `tests/Api.IntegrationTests/Endpoints/EndpointMetadataTests.cs`; Create `tests/Api.IntegrationTests/Endpoints/OrderReadContractTests.cs`; Modify `docs/week-04/endpoint-inventory.json`, `docs/week-04/endpoint-role-matrix.md`, `scripts/generate-postman.mjs`.

**Interfaces:** `GET /api/delivery-orders/{orderNumber}/bookings?pageIndex=1&pageSize=8` dùng ReadDeliveryOrders. `OrderBookingsDto(PagedResult<OrderBookingDto> Bookings, IReadOnlyList<OrderItemBookingCountsDto> ItemCounts)`; booking fields `Id, ItemId, ContainerNumber, Status, ReservedAt, ShippedAt`; counts fields `ItemId, ReservedQuantity, ShippedQuantity, AvailableQuantity` theo quota logic BE. Orders thêm `search`, `sort=createdAt|orderNumber|expiryDate`, `descending`, append record params để tương thích. Không thêm endpoint eligible: FE gọi paged containers làm danh sách ứng viên, assign command quyết định cuối.

- [x] Viết `OrderClerk_ReadsBookingsAndReleasesRealId`: assert 200, booking Guid tồn tại thật, totalCount không phụ thuộc page, counts khớp quota, DELETE dùng Id thành công; tenant khác 404, YardPlanner 403. Thêm search/sort stable và test assign container NeverIn vẫn `Lifecycle.NotInYard`.
- [x] Viết `SixthLogin_ReturnsProblemDetails429`: assert `code == "RateLimit.TooManyRequests"`, status 429, traceId; Retry-After chỉ kiểm khi limiter có metadata. Chạy ba lớp test xác nhận đỏ phần chưa có.
- [x] Implement query/handler tenant-filtered và sorted trước paging; mapping count không tái định nghĩa rule quota. Thêm OnRejected trả ProblemDetails 429; giữ alias và đánh dấu deprecated qua OpenAPI transformer phù hợp package đang cài, có metadata test, không đổi policy.
- [x] Chạy các lớp test và cập nhật generator/inventory/matrix; chạy `node scripts/generate-postman.mjs`, kiểm diff không sinh secrets hoặc đổi role ngoài chủ đích. Collection xuất ở `docs/week-04/postman/` do generator định vị; stage đúng file sinh thay đổi.
- [x] Commit `feat: expose order booking reads and normalize integration contracts`.

## Task 7: FE transport, ProblemDetails và invalidation

**Files:** Create `frontend/src/environments/environment.ts`, `environment.production.ts`, `frontend/proxy.conf.json`, `frontend/proxy.host.conf.json`; Modify `frontend/angular.json`, `frontend/package.json`, `frontend/src/app/app.config.ts`; Create `frontend/src/app/core/api/api-config.ts`, `api.types.ts`, `api-error.ts`, `api-error.spec.ts`, `error.interceptor.ts`, `query-invalidation.ts`, `query-invalidation.spec.ts`; Create `frontend/src/app/core/notifications/notifications.ts`.

**Interfaces:** `ApiPage<T>` đúng PagedResult JSON; `ApiError` giữ status/code/detail/ruleCode/errors/traceId/retryAfterSeconds; `normalizeApiError(error: HttpErrorResponse): ApiError`; `QueryInvalidation.invalidate(keys: readonly QueryKey[]): void`, `changes$: Observable<readonly QueryKey[]>`, QueryKey union `containers|orders|gate|yard|partners|reports|lookups`. `Notifications.success(message: string): void`, `error(message: string): void`; không global-toast validation để tránh lặp thông báo form.

- [x] Viết `normalizesProblemAndNonJsonNetworkError`: `expect(normalizeApiError(problem).traceId).toBe('trace-1')`, giữ field errors array; status0/HTML5xx có fallback tiếng Việt. Invalidation test chỉ emit keys đã nêu, không viết dữ liệu nghiệp vụ.
- [x] Chạy từng spec để xác nhận đỏ; tạo config `/api`, proxy 5000/host5187, production replacement và provideHttpClient. ApiError giữ Http status nếu body không hợp lệ; interceptor không tự retry hoặc chuyển 403 thành logout.
- [x] Implement notification độc lập DemoRepository; kết nối shared toast sau task 9. `api-config.ts` có `isApiUrl(url: string): boolean` kiểm URL đã resolve cùng origin và đúng `/api` boundary, chặn `/api-evil` và origin khác.
- [x] Chạy specs, `npm.cmd run build`; build phải không có localhost hardcode trong production config. Thử dev proxy health qua endpoint API có thật, không dùng health path ngoài proxy `/api` để kết luận lỗi.
- [x] Commit `feat: add typed API transport errors and query invalidation`.

## Task 8: Auth thật, refresh dùng chung và phục hồi phiên

**Files:** Create `frontend/src/app/core/auth/auth.dto.ts`, `token-storage.ts`, `auth-api.ts`, `auth-service.ts`, `auth.interceptor.ts`, `auth-service.spec.ts`, `auth.interceptor.spec.ts`, `auth.guard.ts`, `permissions.ts`, `permissions.spec.ts`, `safe-return-url.ts`; Modify `frontend/src/app/app.config.ts`.

**Interfaces:** `AuthApi.login({userName,password}): Observable<AuthResponseDto>`, `refresh(userName: string, refreshToken: string): Observable<AuthResponseDto>` gửi `{userName,refreshToken}` đúng RefreshTokenCommand, `me(): Observable<CurrentUserDto>`, `logout(): Observable<void>`. DTO bám AuthResponseDto và CurrentUserDto BE, gồm `accessTokenExpiresAt`, `roles`, `tenantId`, `depotName`. StoredSession giữ userName từ login response cùng hai token và accessTokenExpiresAt để refresh được sau reload trước me. `AuthService` public Signals `user`, `status` (`initializing|authenticated|anonymous|error`), methods `restore(): Observable<void>`, `login(input): Observable<void>`, `refreshOnce(): Observable<string>`, `logout(): Observable<void>`; interceptor public `authInterceptor: HttpInterceptorFn`. AuthApi refresh dùng HttpBackend để không tự đi qua interceptor; me/logout dùng HttpClient bình thường để có bearer.

- [x] Viết `threeExpiredRequestsUseOneRotatingRefresh`: `http.expectOne('/api/auth/refresh')`, `expect(refresh.request.body).toEqual({userName:'gate',refreshToken:'refresh-old'})`, flush token mới và assert cả 3 request retry bearer mới, mỗi request tối đa một retry. Test login/refresh401 không gọi refresh đệ quy, URL ngoài API không bearer, refresh403401 xóa phiên nhưng refresh network/5xx giữ token.
- [x] Viết `logoutDuringRefreshCannotRestoreUser`, `brokenOrDeniedSessionStorageDoesNotCrash`, `unsafeReturnUrlIsRejected`: assert response cũ không ghi token/user; storage hỏng bỏ phiên và báo rõ; storage denied dùng phiên trong memory, không giả là sẽ giữ sau reload; returnUrl `//evil`/external bị thay `/dashboard`.
- [x] Chạy các spec đỏ; implement single-flight shareReplay có cleanup và session generation/cancellation, đệm 30 giây, chỉ retry401 tài nguyên, rotate token nguyên tử. Khôi phục phiên phải chờ me; không reset phiên chỉ vì lỗi mạng tạm thời.
- [x] Implement `can(capability: Capability, roles: readonly string[]): boolean` theo policy, tách read/write và expiring-order permission; cache reset khi đổi user/tenant/logout. Dùng timezone FE cấu hình `Asia/Ho_Chi_Minh` khớp Depot default, vì me hiện không trả timezone; không giả có field đó.
- [x] Chạy auth specs bằng HttpTestingController/fake clock và build; chứng minh logout local luôn kết thúc dù BE fail, các waiter canceled không phát lại command.
- [x] Commit `feat: authenticate Angular with guarded single flight token refresh`.

## Task 9: Shell, login và lookup/master data cho form

**Files:** Modify `frontend/src/app/app.ts`, `app.html`, `app.routes.ts`, `features/login/login-page.ts`; Create `frontend/src/app/core/api/lookups.dto.ts`, `lookups-api.ts`, `partners.dto.ts`, `partners-api.ts`, `reference-data.ts`, `reference-data.spec.ts`, `frontend/src/app/core/presentation/container-labels.ts`; Test `frontend/src/app/core/auth/route-permissions.spec.ts`.

**Interfaces:** `LookupsApi.get(): Observable<LookupsDto>` giữ dictionaries; `PartnersApi.customers(params): Observable<ApiPage<CustomerDto>>`, `operators(params): Observable<ApiPage<LineOperatorDto>>`, `ownerCodes(operatorId: string): Observable<OwnerCodeDto[]>` đúng routes BE. `ReferenceData.lookups(): Observable<LookupsDto>`, `clear(): void`; TTL 1 giờ, keys user/tenant, đọc chọn master data qua tìm kiếm/phân trang, không giả lấy đủ bằng pageSize100.

- [x] Viết `cacheExpiresAndClearsOnIdentityChange`: assert cùng phiên 2 subscriber một HTTP request, sau 1 giờ/ref master mutation request mới, response user cũ không đè user mới. Route test assert OrderClerk không request yard, YardPlanner không request expiring-orders, URL trực tiếp cổng bị guard.
- [x] Chạy đỏ; chuyển shell/login/guard sang AuthService, dùng userName/depotName thật, notification task7, xóa demo toolbar/reset và tên Nguyễn Minh gõ cứng. Màn chưa migrate có nhãn chuyển đổi trong giai đoạn làm việc; không cho thao tác snapshot được coi là thao tác thật.
- [x] Map label đúng dictionary enum; bộ chọn giữ cả ID/code để API dùng đúng. Login 401 hiện lỗi tại form, 429 theo Retry-After nếu có; login loading và returnUrl nội bộ.
- [x] Chạy hai spec, auth regression và build. Kiểm bằng browser login/logout với BE thật trước task10; form lỗi vẫn giữ input trừ password theo chính sách UI.
- [x] Commit `feat: connect login shell permissions and reference data to backend`.

## Task 10: Container list, đăng ký và chi tiết

**Files:** Create `frontend/src/app/core/api/containers.dto.ts`, `containers-api.ts`; Create `frontend/src/app/features/containers/container.mapper.ts`, `container.mapper.spec.ts`, `containers-facade.ts`, `containers-facade.spec.ts`; Modify `frontend/src/app/features/containers/containers-page.ts`, `containers-page.html`, `container-detail.ts`; Create `frontend/src/app/core/presentation/container-number.ts`.

**Interfaces:** `ContainersApi.list(query): Observable<ApiPage<ContainerDto>>`, `summary(query): Observable<ContainerSummaryDto>`, `byNumber(number): Observable<ContainerDto>`, `orders(number): Observable<ContainerOrdersDto>`, `register(input: RegisterContainerRequest): Observable<{id:string}>`. Requests bám RegisterContainerCommand; view model giữ `id`, `number`, `size:20|40`, enum codes, nullable physical/virtual location và labels, không cần snapshot. `ContainersFacade.setQuery(query): void`, `select(number: string|null): void`, `register(input): Observable<void>`; public readonly Signals rows/totalCount/summary/detail/loading/error/pending.

- [x] Viết mapper assertions `expect(mapContainer(dto).size).toBe(20)` và VB01 `bay=row=tier=0` thành virtual; null slot thành null, không thành vị trí trống; gửi numeric weights và enum code đúng, không ánh xạ nhãn cũ mơ hồ.
- [x] Viết `lateSearchResponseCannotReplaceNewQuery`, `invalidUrlQueryFallsBackSafely`, `pageBeyondLastMovesToLastPageOnce`: assert 300ms debounce, hủy A khi B bắt đầu, NaN/negative page→1, enum lạ loại bỏ, trang quá lớn điều chỉnh dựa totalPages không tạo vòng lặp. Chạy đỏ.
- [x] Nối server filters/sort/summary task5, pageSize8, reset page khi đổi filter; bỏ `.slice()`/counts trên page. Đăng ký chỉ Admin/GateClerk; label/validation nhập liệu giữ ở FE, rule ISO/quota vẫn BE xác nhận.
- [x] Chi tiết đọc by-number/orders/history endpoint (history client task15 có thể khai báo sớm đúng DTO), tách loading từng tab; 404 đóng chi tiết với thông báo, không hiển thị entity cũ. Nút gán chỉ điều hướng sang task11 khi hoàn tất, không gọi demo.
- [x] Chạy mapper/facade tests, build và browser tạo container→reload→tìm lại; totalCount không tăng khi server trả lỗi. Commit `feat: connect container listing registration and detail to API`.

## Task 11: Lệnh giao, booking và gán/gỡ

**Files:** Create `frontend/src/app/core/api/orders.dto.ts`, `orders-api.ts`; Create `frontend/src/app/features/delivery-orders/orders-facade.ts`, `order.mapper.ts`, `orders-facade.spec.ts`; Modify `frontend/src/app/features/delivery-orders/orders-page.ts`, `orders-page.html`, `frontend/src/app/features/containers/container-detail.ts`.

**Interfaces:** `OrdersApi.list(query)`, `byId(id)`, `create(input)`, `bookings(number, page)`, `assign(number, containerNumbers: string[])`, `release(bookingId)` trả typed Observable tương ứng task6 và AssignContainersResult. `OrdersFacade.assign(numbers: string[]): Observable<AssignContainersResult>`, `release(id: string): Observable<void>`; counts lấy ItemCounts, danh sách ứng viên lấy ContainersApi filter InYard/operator/size/type. Đây là ứng viên, không đảm bảo eligible trước command.

- [x] Viết `partialAssignShowsAssignedAndRejectedNumbers`: `expect(message).toContain('2/3')`, failed numbers được giữ và hiện rõ; 200 partial không báo toàn bộ thành công. Test release gửi bookingId thật, expired booking reassign do BE quyết định; OrderClerk tuyệt đối không HTTP yard.
- [x] Viết `conflictKeepsFormAndRefreshesOrder`: assert 409/422 giữ selection/input, reload order/bookings/containers, không trừ quota local. Chạy đỏ.
- [x] Map customerTaxCode/lineOperator, form nhiều item numeric quantity, status lấy BE. Search/sort/paging server; counts trạng thái dùng query count đúng bộ lọc (pageSize1/totalCount), không đếm từ trang; bỏ KPI shippedToday không có quyền/source phù hợp.
- [x] Implement pending submit, create/assign/release invalidate orders/containers/reports; detail/booking paging độc lập. Không gọi demo eligible/assign/expire/lineCounts.
- [x] Chạy orders-facade spec, build; browser admin/order tạo lệnh→gán container trong bãi→gỡ→reload. Commit `feat: connect delivery orders and booking operations to backend`.

## Task 12: Cổng validate, nhập/xuất và lịch sử sửa/hủy

**Files:** Create `frontend/src/app/core/api/gate.dto.ts`, `gate-api.ts`; Create `frontend/src/app/features/gate/gate-facade.ts`, `gate.mapper.ts`, `gate-facade.spec.ts`; Modify `frontend/src/app/features/gate/gate-page.ts`, `gate-page.html`.

**Interfaces:** `GateApi.validate(input): Observable<ValidateGateResponseDto>` giữ checks/container/order; `gateIn(input): Observable<{id:string}>`, `gateOut(input): Observable<void>`, `transactions(query): Observable<ApiPage<GateTransactionDto>>`, `update(id,input): Observable<void>`, `cancel(id,reason): Observable<void>` dùng POST cancel. Payload đúng command/request hiện có. `GateFacade` giữ input/validation/pending, không phát command nếu validate request cũ chưa về nhưng kết quả validate không thay rule trong command.

- [x] Viết `mapsPassFailWarningAndIgnoresStaleValidation`, `doubleSubmitSendsOneCommand`, `timeoutDoesNotReplayWrite`, `cancelConflictKeepsDetail`: assert state Warning không bị map thành Pass, request validate bị hủy khi input đổi, 409/422 không toast success.
- [x] Chạy đỏ; nối validate/gatein/out/update/cancel và history Changes DTO; menu chỉ Admin/GateClerk. Gợi ý slot từ YardApi task13: client có thể tạo sớm, giữ contract cùng task13; không tự tính rule xếp bãi.
- [x] Giữ unsaved guard, form sau lỗi và xác nhận hủy; sau ghi invalidate gate/containers/orders/yard/reports. History search/paging phải dùng khả năng BE thật, nhãn không hứa tìm số container nếu BE chỉ lọc xe/tài xế; không lọc một trang rồi báo tổng đúng.
- [x] Chạy gate spec, acceptance Scenario08/09/14 backend nếu có sửa BE liên quan, build và browser gate-in→reload, edit có Changes, cancel đúng trạng thái. Commit `feat: connect gate validation transactions and cancellation`.

## Task 13: Sơ đồ bãi, vị trí ảo và mở/thu block

**Files:** Create `frontend/src/app/core/api/yard.dto.ts`, `yard-api.ts`; Create `frontend/src/app/features/yard/yard-facade.ts`, `yard.mapper.ts`, `yard-facade.spec.ts`; Modify `frontend/src/app/features/yard/yard-page.ts`, `yard-page.html` và `yard-page.scss` chỉ nếu cần vị trí/footprint đúng.

**Interfaces:** `YardApi.blocks()`, `layout(idOrCode,tier)`, `search(containerNumber)`, `virtualContainers(code)`, `freeSlots(blockId,size,page)`, `place(input)`, `relocate(input)`, `placeFromVirtual(code,containerId,target)`, `expand(id,input)`, `shrink(id,input)` đều typed Observable đúng endpoint hiện tại. Mapper row số↔nhãn chữ chỉ presentation, 40ft logical bay/footprint theo BE layout không tự dùng công thức slot demo.

- [x] Viết `fortyFootFootprintAndVirtualPositionMatchLayout`, `lateTierResponseDoesNotReplaceSelectedTier`, `occupiedTarget409ReloadsLayout`: assert 40ft không vẽ hai container khác nhau, VB01 không tính công suất physical, payload row number, giữ selection sau conflict.
- [x] Chạy đỏ; nối block/tier/search/place/move/expand/shrink và block ảo; read cho Admin/YardPlanner/GateClerk, mutation chỉ Admin/YardPlanner. GateClerk không cần ManageYard để lấy free-slots.
- [x] Invalidate layout/capacity/container/report sau thành công; command BE kiểm lại occupied/support/concurrency. Bỏ move/resize/isBlocked rule demo và không chặn thao tác hợp lệ bằng rule tự tính ở FE.
- [x] Chạy facade spec, build và acceptance backend Scenario05/07/11 nếu động tới contract; browser chuyển block ảo→ô thật rồi reload, thu block có container bị chặn đúng. Commit `feat: connect yard layout and placement operations to API`.

## Task 14: Partners CRUD đúng schema và quyền

**Files:** Modify `frontend/src/app/core/api/partners-api.ts`, `partners.dto.ts`; Create `frontend/src/app/features/partners/partners-facade.ts`, `partners-facade.spec.ts`, `partner.mapper.ts`; Modify `frontend/src/app/features/partners/partners-page.ts`, `partners-page.html`.

**Interfaces:** Bổ sung `createCustomer(input)`, `updateCustomer(id,{name})`, `activateCustomer(id)`, `deactivateCustomer(id)` và các method tương ứng operator theo BE; thao tác ownerCodes dùng routes hiện có, không giả endpoint. Facade thao tác xong invalidate partners/lookups/orders/containers/reports; màu hãng là hàm thuần từ code.

- [x] Viết `unsupportedContactFieldsAreNeverSubmitted`, `readRoleCannotMutate`, `deactivation409PreservesRow`: assert payload không email/phone/address/contact/color, user khác Admin không có write affordance, lỗi HasActiveOrders/Resources hiển thị và không đổi isActive local.
- [x] Chạy đỏ; bỏ trường không hỗ trợ, nối CRUD/activate/deactivate, modal chờ server, chọn master data phân trang không bỏ sót >100 mục. Không thêm filter server chưa có mà lọc local một trang.
- [x] Chạy spec/build và browser Admin đổi tên→mở lệnh thấy tên mới khi API trả; role đọc chỉ xem danh sách. Commit `feat: connect partner management to supported backend contracts`.

## Task 15: Dashboard, báo cáo, tồn bãi và Excel nhiều trang

**Files:** Create `frontend/src/app/core/api/reports.dto.ts`, `reports-api.ts`; Create `frontend/src/app/features/reports/reports-facade.ts`, `report.mapper.ts`, `reports-facade.spec.ts`, `frontend/src/app/core/export/paged-export.ts`, `paged-export.spec.ts`; Modify `frontend/src/app/features/dashboard/dashboard-page.ts`, `dashboard-page.html`, `frontend/src/app/features/inventory/inventory-page.ts`, `inventory-page.html`, `frontend/src/app/features/reports/reports-page.ts`, `reports-page.html`, `frontend/src/app/core/export/workbook-export.ts`; Create `frontend/src/app/features/dashboard/dashboard-facade.ts`, `frontend/src/app/features/inventory/inventory-facade.ts`.

**Interfaces:** `ReportsApi.summary()`, `traffic(days)`, `capacity()`, `inventory(query)`, `dwellTimes(query)`, `history(number,page)`, `expiringOrders(query)` theo DTO thật. `collectPages<T>(load: (pageIndex:number,pageSize:number)=>Observable<ApiPage<T>>, signal:AbortSignal, progress:(done:number,total:number)=>void): Promise<T[]>`; pageSize100, validate mỗi trang đúng pageIndex, không lặp trang. Inventory groups adapter giữ `totalContainers` riêng `groups.totalCount`.

- [x] Viết `groupTotalIsNotContainerTotal`, `dashboardDoesNotCountSingleContainerPage`, `yardPlannerNeverRequestsExpiringOrders`: assert số nền seed 20 trong bãi, group counts cộng đúng, KPI không đi theo page. Top lưu lâu đọc đủ page cần thiết rồi sort trên đầy đủ tập nếu backend chưa hỗ trợ top-sort; không sort riêng trang đầu và gọi là top toàn bãi.
- [x] Viết `exportsAll201RowsInThreePages`, `pageTwoFailureOrAbortDoesNotDownloadPartialWorkbook`, `changedTotalsStopWithRetryMessage`: assert load pages1/2/3, exactly201 rows, no download khi lỗi/hủy/count thay đổi; tổng ổn định vẫn hiển thị giới hạn không bảo đảm snapshot đồng thời. Chạy đỏ.
- [x] Nối KPI đúng nguồn, traffic days mặc định7, inventory/current dwell; bỏ arbitrary date filter/activity/chart không có API hoặc quyền. Gate history report không gọi gate transactions bằng role thiếu quyền; history container dùng reports endpoint.
- [x] Excel dùng filters đã chốt, progress/cancel, giữ strings bắt đầu =/+/-/@ dưới dạng text; đổi creator từ SNP Depot Demo thành SNP Depot Management. TTL/cache/invalidation theo task7–9, không export data demo.
- [x] Chạy specs/export regression/build và đối chiếu UI với API trên dataset riêng, không tính ngày từ ngày trong ảnh UI. Commit `feat: connect reporting dashboards and complete filtered exports`.

## Task 16: E2E API thật và loại bỏ phụ thuộc demo khỏi bản tích hợp

**Files:** Create `frontend/playwright.api.config.ts`, `frontend/e2e-api/auth.spec.ts`, `roles.spec.ts`, `workflow.spec.ts`, `reports.spec.ts`, `fixtures/api-fixture.ts`; Modify `frontend/package.json`, `scripts/smoke-development.ps1`; Create `scripts/check-api-runtime.mjs`; Review/remove unused runtime imports ở `frontend/src/app/` sau khi tag demo đã kiểm tra.

**Interfaces:** script npm `e2e:api` chạy config API, `check:api-runtime` quét import graph từ `src/main.ts` và loại test files, fail khi runtime reach demo/session/snapshot/localStorage business state hoặc gọi alias route literal. E2E dùng API/FE port riêng, `reuseExistingServer:false`, workers1, credentials config từ ignored env, dataset của compose project bắt đầu `snp-integration-`.

- [x] Viết E2E theo thứ tự độc lập có fixture riêng: 4 role login/menu/directURL403; refresh thật bằng access token TTL cấu hình test ngắn hoặc đồng hồ môi trường test; gate-in35→assignDO002→gate-out35; DO004 expired reassignment; virtual→physical; history/dashboard sau reload. Không mock business API và không dùng token lỗi để giả refresh thành công.
- [x] Chạy trên code đã nối để bắt lỗi còn lại. Dùng `Jwt__AccessTokenExpirationMinutes=1` trong cấu hình API E2E riêng; kiểm refresh sau cửa sổ 30 giây với timeout test đủ dài, không đổi cấu hình dev/prod hoặc thêm endpoint debug. Test workflow không reset dataset giữa các bước; role login tests dùng fixture login dùng lại storageState theo role và spacing khi cần để không vượt 5/min cùng IP, không hạ auth policy.
- [x] Thêm runtime graph check; loại imports/demo tooling còn lại, giữ helper thuần và tests còn phù hợp. Xóa/chuyển demo-only tests chỉ sau xác nhận tag có thể phục hồi; không đổi chúng thành mocks để báo API pass.
- [x] Chạy `dotnet build SnpDepotManagement.sln`, `dotnet test SnpDepotManagement.sln`, từ frontend `npm.cmd test`, `npm.cmd run build`, `npm.cmd run check:api-runtime`, `npm.cmd run e2e:api`; chạy smoke mới và restart demo hai lần. Ghi số pass/fail/skip và công cụ/SDK thật, không copy kết quả cũ.
- [x] Commit `test: verify real API workflows and remove demo runtime dependencies` khi checks xanh. Nếu môi trường chặn suite nào, báo rõ blocker và chưa đánh dấu nghiệm thu hoàn tất.

## Task 17: README, báo cáo ngày và dọn Markdown cũ

**Files:** Modify `README.md`, `frontend/README.md`; Create `docs/reports/2026-10-21-fe-be-integration.md` (nếu bàn giao ngày khác dùng đúng ngày Asia/Bangkok và ghi đường dẫn thực vào plan); Create `scripts/check-doc-links.mjs`; Review tất cả Markdown tracked và ignored dưới repository, gồm `docs/week-04/`, `docs/design/`, `docs/superpowers/`, `frontend/docs/`, `src/Domain/README.md`.

**Interfaces:** README gốc làm entrypoint; frontend README chỉ hướng dẫn riêng Angular và liên kết root. Báo cáo ngày chứa kết quả thật, giới hạn, tag/SHA demo, danh sách giữ/xóa và lý do; `node scripts/check-doc-links.mjs` chỉ kiểm đường dẫn Markdown nội bộ hiện có (ignore fenced code/external URLs, resolve URL encoding/anchors theo heading), exit khác0 nếu broken file/anchor; không truy cập network.

- [x] Kiểm kê bằng `rg --files --hidden --no-ignore -g '*.md'` loại .git/node_modules/bin/obj; đọc từng tài liệu dự kiến xóa. Giữ day-*.md và báo cáo ngày theo nội dung, API/role matrix/config/runbook/deploy/backup/UAT/business requirements còn hiệu lực, AGENTS/SKILL và spec/plan đang dùng.
- [x] Viết README theo kết quả task16: prerequisites, config keys không secret, Docker/host API/proxy, migrate/seed, roles, commands build/test/E2E, giới hạn report/Excel, phục hồi demo tag. Hợp nhất thông tin còn cần từ guide cũ trước khi bỏ bản trùng; bỏ khẳng định “sẵn sàng 100%” và số test cũ không còn dùng làm trạng thái hiện tại.
- [x] Lập danh sách đường dẫn xóa cụ thể trong báo cáo ngày. File untracked/ignored cần bản sao phục hồi vào zip ignored dưới thư mục backups với timestamp, xác minh entry/hash trước khi xóa; không stage zip. Chỉ xóa file đã có bản thay thế hoặc nội dung hết hiệu lực; không xóa nguyên thư mục theo ngày/tuổi.
- [x] Xác minh absolute resolved path nằm trong repository trước delete/move, dùng PowerShell Remove-Item -LiteralPath từng file đã chọn. Sửa link trỏ tới tài liệu thay thế, thêm index báo cáo ngày; giữ original report ngày không sửa kết quả lịch sử.
- [x] Chạy `node scripts/check-doc-links.mjs`, `git diff --check`, đọc toàn bộ deletion diff, đối chiếu mọi báo cáo ngày còn tồn tại và chạy lại lệnh quick-start trên project test hiện có. Thay đổi docs không yêu cầu rerun toàn bộ suite code nếu không sửa code/config.
- [x] Commit `docs: publish integration guide and remove superseded markdown` với danh sách file rõ ràng. Tài liệu bị ignore nhưng cần bàn giao phải stage bằng `git add -f -- <specific-path>`; không mở ignore toàn bộ backups/env hoặc force-add cả thư mục.

## Tự rà kế hoạch và bàn giao thực hiện

- [x] Đối chiếu spec: hạ tầng task1, seed task2–4, contract task5–6, transport/auth task7–9, features task10–15, nghiệm thu task16, README/dọn MD task17.
- [x] Kiểm tra interfaces: OrdersApi dùng booking endpoint task6; GateApi trả object checks; Reports inventory groups adapter không ép ApiPage container; me không có timezone; refresh token payload có userName đúng DTO; container chưa nhập không được assign.
- Khi thực hiện, mỗi task chỉ stage file thuộc task đó; ghi kế hoạch/checklist đã xong và báo cáo ngày. Không xóa/ghi đè thay đổi người dùng xuất hiện giữa chừng. Tag demo phải được kiểm chứng trước bỏ runtime demo.

Tình trạng ban đầu: kế hoạch đã được người dùng duyệt và cho triển khai inline. Các checkbox hiện phản ánh công việc đã thực hiện; bằng chứng và lệch kế hoạch nằm trong báo cáo ngày 21/10/2026. Review toàn nhánh là bước nghiệm thu cuối sau task17.
