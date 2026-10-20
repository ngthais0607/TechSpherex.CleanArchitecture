# Angular Depot Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bàn giao demo Angular đầy đủ các màn đã duyệt, có dữ liệu mẫu nhất quán, thao tác nghiệp vụ liên thông và form cân đối trên laptop/tablet.

**Architecture:** Frontend độc lập trong `frontend/`, Angular standalone với lazy routes, Reactive Forms và Signals. UI gọi các service có kiểu; adapter demo xử lý truy vấn/command trên snapshot chung, kiểm tra trước khi commit nguyên tử và lưu localStorage. Quy tắc, truy vấn, thành phần dùng chung và trang được tách theo trách nhiệm.

**Tech Stack:** Angular/CLI/CDK 21, TypeScript 5.9, RxJS 7, SCSS, Angular CLI unit-test runner Vitest, Playwright cho E2E, ExcelJS cho XLSX, Manrope đóng gói bằng `@fontsource/manrope`. Khóa phiên bản thực tế bằng `package-lock.json`; không dùng API experimental hay SSR.

**Spec:** `docs/superpowers/specs/2026-10-20-angular-demo-design.md` (đã được người dùng duyệt).

## Global Constraints

- “Ứng dụng độc lập tại `frontend/`, không tái cấu trúc backend.”
- “Người dùng đã chọn demo dữ liệu mẫu, yêu cầu đầy đủ màn hình, bổ sung Tồn bãi và chưa làm quản lý Người dùng.”
- “Giữ phong cách cyan/navy của ảnh tham chiếu; tối ưu tỷ lệ chữ, khoảng cách, form và bảng.”
- “Nội dung và dữ liệu 14–16px, nhãn 14px/medium, hướng dẫn 12–13px, tiêu đề nhóm 16–18px, tiêu đề trang 26–28px.”
- “Input và nút cao 40–44px trên desktop, tối thiểu 44px trên tablet.”
- “Kiểm tra ở 1440×900, 1366×768, 1024×768 và 768×1024; zoom 125% không làm mất nút hoặc cắt chữ.”
- “Dùng đồng hồ demo múi giờ `Asia/Bangkok` (UTC+7), ngày hết hạn có hiệu lực đến hết ngày depot”.
- “Mỗi container tối đa một booking hiệu lực.”
- “Excel là file `.xlsx` thật”; xuất toàn bộ dữ liệu đã lọc, không chỉ trang đang thấy.
- Không gọi backend; không tạo Figma; không có quản lý tài khoản hay phân quyền thật. Nhãn “Chế độ demo”, không giả báo tình trạng máy chủ.
- Tên/nhãn tiếng Việt, ngày `dd/MM/yyyy`, giờ `HH:mm`, số `vi-VN`. Trạng thái/lỗi/tải/trống và trợ năng thuộc từng task UI.

## Review Focus

1. Số container có khoảng trắng, chữ thường hoặc dán thừa khoảng trắng: chuẩn hóa trước tìm/validate, không tạo bản trùng — Task 2/5.
2. Bản lưu JSON hợp lệ nhưng sai quan hệ dữ liệu: báo hỏng, không âm thầm ghi đè hoặc chạy với vị trí/booking mâu thuẫn — Task 2.
3. Gõ container nhanh và nhận validate cũ sau phản hồi mới: kết quả cũ không bật nút ghi sai — Task 7.
4. Thời điểm đúng 00:00 UTC+7, ngày hết hạn và mốc 72 giờ: trạng thái và KPI đổi đúng, không phụ thuộc múi giờ hệ điều hành — Task 2/4/10.
5. Chuỗi tên/ghi chú bắt đầu bằng dấu `=`, tên khách dài và dấu tiếng Việt: Excel giữ chuỗi dữ liệu, UI không tràn viewport — Task 10/11.

## Môi trường, quy ước và cấu trúc file

Đã kiểm tra Node `20.19.4`, npm `11.5.2`, chưa có `frontend/` hay Angular workspace. Angular 21 hỗ trợ Node này và TypeScript 5.9 theo [tài liệu phiên bản](https://angular.dev/reference/versions). Dùng [runner của CLI](https://angular.dev/guide/testing) và [ExcelJS browser workbook](https://github.com/exceljs/exceljs#browser). Không nâng Node toàn máy.

Trước triển khai đọc lại spec, `git status`, hướng dẫn AGENTS nếu mới xuất hiện; dùng skill worktrees ở thời điểm thực thi nếu cần tách checkout. Không ghi đè thay đổi của người dùng. Không tự push hoặc deploy.

Các lệnh trong task chạy tại `frontend/`, trừ khi ghi rõ. Dùng `npm.cmd` trên PowerShell. Mỗi file component có `.ts`, `.html`, `.scss` cùng basename; danh sách dưới ghi `.ts` để chỉ bộ ba file. Logic thuần dùng Vitest; UI hành vi dùng TestBed khi cần, E2E dùng Playwright. Không viết test cho từng màu hoặc từng dòng markup.

| Đường dẫn | Trách nhiệm |
| --- | --- |
| `frontend/src/app/core/models/{entities,commands,queries,views,result}.ts` | Hợp đồng domain demo, đầu vào lệnh/truy vấn, projection và lỗi |
| `frontend/src/app/core/data/depot-data-source.ts` | Abstract service trả Observable, điểm thay adapter sau này |
| `frontend/src/app/core/{clock,format,session}/` | Ngày depot, định dạng và phiên demo |
| `frontend/src/app/demo/{seed,storage,rules,commands,queries}/` | Dữ liệu mẫu, persistence, quy tắc, thay đổi snapshot, projections |
| `frontend/src/app/demo/{demo-repository,demo-data-source,demo-control}.ts` | Commit snapshot, adapter bất đồng bộ, độ trễ/lỗi đọc có chủ đích |
| `frontend/src/app/shared/{ui,forms,feedback,charts}/` | Thành phần dùng chung, dialog CDK, field errors, biểu đồ truy cập được |
| `frontend/src/app/layout/` | Sidebar/topbar/drawer, trạng thái demo và reset |
| `frontend/src/app/features/{login,dashboard,containers,delivery-orders,gate,yard,partners,reports,inventory}/` | Lazy pages, dialog và form theo chức năng |
| `frontend/src/styles/{tokens,base,forms,tables}.scss` | Hệ thống hình thức dùng chung |
| `frontend/e2e/` | Luồng nghiệp vụ, điều hướng, storage và responsive |

### Hợp đồng dùng xuyên các task

Task 2 định nghĩa đầy đủ các kiểu; task sau không tự tạo biến thể cùng thực thể. ID là chuỗi; thời điểm ISO UTC; ngày lệnh `yyyy-MM-dd`, tháng sản xuất `yyyy-MM`.

`DepotSnapshot` gồm `schemaVersion: 1`, `revision`, `seededAt`, `containers`, `customers`, `operators`, `orders`, `bookings`, `lifecycles`, `blocks`, `gateTransactions`, `movements`. Order chứa line có ID; booking và giao dịch tham chiếu `lifecycleId` để một container xuất nhiều lần không làm sai lịch sử.

`Position = { kind: 'physical'; blockId: string; bay: number; row: string; tier: number } | { kind: 'virtual'; blockId: string }`. Container có number/type/size/iso/ownerCode/operatorId/grade/condition/weights/manufactureMonth; không lưu KPI hoặc displayStatus. Snapshot bất biến từ góc nhìn consumer.

`Result<T> = { ok: true; value: T } | { ok: false; error: { code: string; message: string; fields?: Record<string,string>; ruleCodes?: string[] } }`.

`DepotClock.now(): Date`, `today(): string`, `setForTest(instant: string | null): void`; `normalizeContainerNumber(value: string): string`. Clock phát thay đổi lúc phút mới/tab focus để trạng thái hết hạn và tuổi lưu cập nhật.

`QueryMap` ghép đầu vào/đầu ra: `containers: ContainerFilters → Page<ContainerRow>`, `container: {number} → ContainerDetail`, `containerOrders: {number} → ContainerOrdersView`, `orders: OrderFilters → OrdersView`, `order: {id} → OrderDetail`, `eligibleContainers: {orderId} → ContainerRow[]`, `eligibleOrders: {number} → OrderRow[]`, `gateHistory: GateFilters → Page<GateRow>`, `gateTransaction: {id} → GateDetail`, `yard: YardFilters → YardView`, `yardSearch: {number} → YardSearchResult`, `partners: PartnerFilters → PartnersView`, `dashboard: {} → DashboardView`, `report: ReportFilters → ReportView`, `inventory: InventoryFilters → InventoryView`.

`CommandMap`: `registerContainer(RegisterContainerInput) → ContainerDetail`, `createOrder(CreateOrderInput) → OrderDetail`, `assign({orderId, numbers}) → OrderDetail`, `release({bookingId}) → OrderDetail`, `gateIn(GateInInput) → GateDetail`, `gateOut(GateOutInput) → GateDetail`, `editGate(EditGateInput) → GateDetail`, `cancelGate({id}) → GateDetail`, `move({number, target}) → ContainerDetail`, `resizeBlock({blockId, oddBays, rows, tiers}) → YardView`, `saveCustomer(CustomerInput) → Customer`, `saveOperator(OperatorInput) → Operator`, `deactivatePartner({kind, id}) → PartnersView`, `reset({}) → void`. Form input là DTO, không cho gửi toàn bộ entity để sửa trường khóa.

`DepotDataSource.watch<K extends keyof QueryMap>(key: K, input$: Observable<QueryMap[K]['input']>): Observable<LoadState<QueryMap[K]['output']>>`; `execute<K extends keyof CommandMap>(key: K, input: CommandMap[K]['input']): Observable<Result<CommandMap[K]['output']>>`; `validateGate(input: GateInput): Observable<Result<GateCheck[]>>`; `suggestPosition(number: string, blockId: string): Observable<Result<Position>>`. `LoadState<T>` là union loading/error/ready; ready có dữ liệu rỗng để UI render empty. Không giữ kết quả truy vấn riêng làm nguồn ghi.

## Task 1: Ứng dụng chạy được, khung giao diện và phiên demo

**Files:** Tạo `frontend/{package.json,package-lock.json,angular.json,tsconfig.json,playwright.config.ts}`, `src/{main.ts,styles.scss}`, `src/styles/{tokens,base,forms,tables}.scss`, `src/app/{app.ts,app.config.ts,app.routes.ts}`, `layout/{app-shell,demo-tools}.ts`, `core/session/{demo-session,session.guard}.ts`, `features/login/login-page.ts`, `e2e/session.spec.ts`; sửa `.gitignore` chỉ thêm ignore output frontend.

**Interfaces:** `DemoSession.login(username: string,password: string): Result<void>`, `logout(): void`, `currentUser` signal; `safeReturnUrl(raw: string|null): string` chỉ nhận route nội bộ. Credentials cố định `demo`/`Demo@123`, hiện rõ trên login. Route `/login` công khai; các route còn lại dùng guard. Shell có `<router-outlet>`.

- [ ] Tại repo root scaffold bằng `npx.cmd --yes @angular/cli@21 new snp-depot-demo --directory frontend --routing --style scss --standalone --strict --skip-git --skip-install --defaults`; chưa tạo trang giả hoặc copy React monolith.
- [ ] Cấu hình npm scripts `start`, `build`, `test` (`ng test --watch=false`), `e2e` (`playwright test`); cài CDK cùng major Angular, font, ExcelJS và Playwright; giữ runner Vitest của CLI và lockfile. Đặt routes theo bảng phạm vi: `/dashboard`, `/containers`, `/delivery-orders`, `/gate`, `/yard`, `/partners`, `/reports`, `/inventory`; fallback dashboard.
- [ ] Viết test session: `expect(login('demo','wrong').ok).toBe(false)`; đăng nhập đúng giữ phiên khi reload, logout chặn route, `safeReturnUrl('https://example.com') === '/dashboard'`. E2E `session.spec.ts` kiểm tra quay lại `/inventory` sau login.
- [ ] Chạy `npm.cmd test -- --include='**/demo-session.spec.ts'` và `npm.cmd run e2e -- session.spec.ts`; phải FAIL vì chưa có xử lý session/guard, không coi lỗi cài runner là red hợp lệ.
- [ ] Implement session/guard/login và shell có nav đủ tám mục, không có Người dùng. Áp tokens: sidebar 248px/topbar 64px, body 15px, label 14px, title 28px, desktop control 44px, gap 16/24px. Drawer dưới 1100px; phiên chỉ sessionStorage.
- [ ] Chạy lại test session và `npm.cmd run build`; PASS, login và shell xem được. Commit riêng task: `feat(frontend): add Angular shell and demo session`.

## Task 2: Hợp đồng, seed và persistence nhất quán

**Files:** Tạo `core/models/{entities,commands,queries,views,result}.ts`, `core/clock/depot-clock.ts`, `core/format/container-number.ts`, `core/data/depot-data-source.ts`, `demo/seed/{create-demo-snapshot,seed-invariants}.ts`, `demo/storage/snapshot-storage.ts`, `demo/demo-repository.ts`; tests cùng tên `.spec.ts`.

**Interfaces:** Định nghĩa các hợp đồng ở trên. `createDemoSnapshot(now: Date): DepotSnapshot`; `assertSnapshot(snapshot: unknown): Result<DepotSnapshot>`; `DemoRepository.snapshot: Signal<DepotSnapshot>`, `transact<T>(fn: (draft: DepotSnapshot) => Result<T>): Result<T>`, `reset(): Result<void>`; `SnapshotStorage.load(): Result<DepotSnapshot|null>`, `save(value): Result<void>`.

- [ ] Viết test seed: mọi số qua checksum, 7 loại/2 cỡ, ít nhất 3 container có >=2 khách lịch sử, booking/gate/lifecycle khớp, slot không trùng. `expect(normalizeContainerNumber(' msku 482190 4 ')).toBe('MSKU4821904')` (chỉ kiểm tra normalize, không coi đây là checksum fixture).
- [ ] Viết test storage JSON đúng syntax nhưng 2 container cùng vị trí → `ok === false`; schema không đúng → báo phục hồi; quota exception → vẫn giữ thay đổi trong RAM và cảnh báo. Transaction trả lỗi phải giữ revision và snapshot cũ.
- [ ] Chạy `npm.cmd test -- --include='**/create-demo-snapshot.spec.ts' --include='**/snapshot-storage.spec.ts' --include='**/demo-repository.spec.ts'`; FAIL ở assertions/hàm thiếu.
- [ ] Implement typed entities, number generator checksum ISO, seed từ clock, validators cấu trúc/quan hệ; persist schema 1. Snapshot storage key `snp-depot-demo:v1`. Không tự ghi đè snapshot hỏng; expose recovery state, người dùng xác nhận reset qua shell.
- [ ] Implement clock UTC+7, test `today()` trước/sau `2026-10-20T17:00:00Z` ra `2026-10-20`/`2026-10-21`; hoàn thiện `assertSnapshot` theo quy tắc Task 3/4 khi có. Chạy tests task và build, PASS; commit `feat(frontend): add shared demo snapshot and persistence`.

## Task 3: Quy tắc vị trí và di chuyển bãi

**Files:** Tạo `demo/rules/yard-rules.ts`, `demo/commands/yard-commands.ts`, `demo/queries/yard-query.ts`, tests tương ứng.

**Interfaces:** `occupiedSlots(position: Position, size: ContainerSize): string[]`; `validatePlacement(snapshot, number, target): Result<void>`; `isBlocked(snapshot, number): boolean`; `moveContainer(draft, {number,target}, now): Result<ContainerDetail>`; `resizeBlock(draft,input): Result<YardView>`; `suggestPosition(snapshot,number,blockId): Result<Position>`; `projectYard(snapshot,filters): YardView` và `searchYard(snapshot,number): YardSearchResult`.

- [ ] Test fixture block 3 slot, rows A/B, 3 tier: 20ft bay 1 chiếm slot 1; 40ft bay 2 chiếm slots 1/2; 40ft bay 6 vượt block. Tier 2 trên một 20ft thiếu đỡ → lỗi Y4; trên 2×20ft đạt; 20ft trên 40ft → Y4.
- [ ] Test di chuyển bị đè/đích đã chiếm → không đổi snapshot; block ảo không có tọa độ; thu hẹp làm cont ngoài vùng → Y6 kèm number/position; vị trí gợi ý phải qua cùng validator.
- [ ] Chạy `npm.cmd test -- --include='**/yard-*.spec.ts'`; FAIL trước implement.
- [ ] Implement công thức bay/slot tài liệu, vị trí ảo, tự loại vị trí nguồn khi kiểm tra move, nhật ký move giữ nguyên gateInAt. Query trả màu/chú thích và block capacity TEU, không nhân đôi container 40ft trong count.
- [ ] Chạy tests task và build, PASS; commit `feat(frontend): add yard placement and movement rules`.

## Task 4: Quy tắc container, booking và giao dịch cổng

**Files:** Tạo `demo/rules/{container-rules,booking-rules,gate-rules}.ts`, `demo/commands/{container-commands,order-commands,booking-commands,gate-commands}.ts`, `demo/queries/{container-query,order-query,gate-query}.ts`; tests cùng basename.

**Interfaces:** `registerContainer(draft,input,now): Result<ContainerDetail>`; `createOrder(draft,input,today): Result<OrderDetail>`; `assignContainers(draft,{orderId,numbers},now): Result<OrderDetail>`; `releaseBooking(draft,{bookingId},now): Result<OrderDetail>`; `expireBookings(draft,today): void`; `evaluateGate(snapshot,input,now): GateCheck[]`; `applyGateIn`, `applyGateOut`, `editGateTransaction`, `cancelGateTransaction` nhận `(draft,input,now)` trả `Result<GateDetail>`. Query projections tương ứng `projectContainers`, `projectContainer`, `projectContainerOrders`, `projectOrders`, `projectOrder`, `projectEligibleContainers`, `projectEligibleOrders`, `projectGateHistory`, `projectGateTransaction` theo QueryMap.

- [ ] Viết test container: checksum/trùng/ISO-cỡ/tare>=max/tháng tương lai bị chặn; order dòng trùng hoặc quantity 0 bị chặn; số lệnh trùng sau trim không tạo mới. Fixture hợp lệ dùng generator Task 2.
- [ ] Test gán 2 container khi một sai hãng không giữ chỗ con nào; gán lặp cùng số trong batch → lỗi; same container không giữ 2 lệnh; release giữ lịch sử; hết ngày UTC+7 giải phóng booking, lệnh giao đủ ưu tiên hơn hết hạn.
- [ ] Test nhập→gán→xuất→hủy xuất khôi phục lifecycle/vị trí/booking; trực tiếp chưa gán tạo Shipped; xuất sai booking/bị đè từ chối; vị trí cũ bị chiếm làm hủy fail nguyên tử. Hủy nhập có move/booking bị chặn; sửa bản ghi cũ không ghi đè condition hiện tại.
- [ ] Chạy `npm.cmd test -- --include='**/container-*.spec.ts' --include='**/order-*.spec.ts' --include='**/booking-*.spec.ts' --include='**/gate-*.spec.ts'`; FAIL trước implement.
- [ ] Implement bằng validators dùng chung, snapshot transaction Task 2, yard rules Task 3; liên kết các lần nhập/xuất bằng lifecycleId. Error mang mã C/D/G/Y hiện có; lỗi riêng demo dùng prefix `DEMO_`, không gán bừa mã backend. Sửa audit lưu tên người mẫu/thời điểm/old/new. Queries chỉ derive displayStatus, không lưu riêng.
- [ ] Nâng seed invariant checker kiểm tra support/booking/lifecycle bằng rules; chạy toàn bộ unit tests và build, PASS; commit `feat(frontend): implement demo container order and gate workflows`.

## Task 5: Adapter, thành phần dùng chung và màn Container

**Files:** Tạo `demo/{demo-data-source,demo-control}.ts`, `shared/ui/{button,badge,pagination,dialog,confirm-dialog}.ts`, `shared/forms/field-errors.ts`, `shared/feedback/{load-panel,toast}.ts`, `core/format/display.ts`, `features/containers/{containers-page,container-form,container-detail}.ts`, `e2e/containers.spec.ts`; sửa providers và route containers.

**Interfaces:** Implement DepotDataSource.watch/execute/validateGate/suggestPosition đã định nghĩa. `DemoControl.failNextRead(): void`, `latencyMs` signal mặc định 180; `ConfirmDialog.open({title,message,confirmLabel}): Observable<boolean>`; `Toast.show(message: string): void`; `ContainerDetail` đọc route query `container`, tab `detailTab`.

- [ ] Test adapter mutation thành công phát projection mới, lỗi không đổi dữ liệu; watch đổi filters bỏ response cũ; failNextRead lỗi đúng một lần, Thử lại trả dữ liệu.
- [ ] E2E container dán số có space/lowercase tìm đúng bản ghi; search không có → empty; từ trang 2 đổi loại → trang 1; mở modal dài, Tab không thoát dialog, Esc trả focus, form dirty cần xác nhận. Tạo container hợp lệ → Chờ nhập; không có trường order trong create.
- [ ] Chạy unit adapter và `npm.cmd run e2e -- containers.spec.ts`; FAIL vì chưa có hành vi.
- [ ] Implement adapter switchMap/timer, revision và clock subscriptions; normalize booking hết hạn trước đọc/ghi, chỉ commit nếu có thay đổi để tránh vòng phát lại. UI cùng CDK Dialog/a11y, trường có label/errors, reusable load panel. Làm đủ tabs/filters/cột/sort/paging/owner/form validation/detail theo spec. Chuẩn hóa filter URL invalid về mặc định. Demo tools cho reset/failNextRead.
- [ ] Chạy tests task, build; xác nhận localStorage reload giữ create và reset xóa sau confirm. Commit `feat(frontend): build shared UI and container screens`.

## Task 6: Màn Lệnh giao đầy đủ

**Files:** Tạo `features/delivery-orders/{orders-page,order-form,order-detail,assign-containers}.ts`, `e2e/orders.spec.ts`; sửa route orders và liên kết từ container.

**Interfaces:** Dùng QueryMap.orders/order/eligibleContainers/eligibleOrders và CommandMap.createOrder/assign/release. Query URL `status`, `customerId`, `search`, `page`, `order` (ID chi tiết).

- [ ] E2E tạo lệnh 2 dòng, chặn loại/cỡ trùng và hạn quá khứ; gán nhiều tới quota khóa checkbox; gỡ có confirm và update số còn; lệnh hết hạn disable assign. `expect(reserved + shipped + remaining).toEqual(quantity)` cho từng dòng sau thao tác.
- [ ] Chạy `npm.cmd run e2e -- orders.spec.ts`; FAIL trước khi build màn.
- [ ] Implement 3 KPI và toggle/bỏ lọc; form hai cột, helper hạn lệnh, danh sách khách/hãng active, chi tiết và select-many. Từ container mở đúng lệnh, từ lệnh mở đúng container. Lỗi không đóng form, pending chặn submit lặp.
- [ ] Chạy E2E task và build, PASS; commit `feat(frontend): build delivery orders and booking dialogs`.

## Task 7: Màn Cổng nhập/xuất và lịch sử

**Files:** Tạo `features/gate/{gate-page,gate-in-form,gate-out-form,business-checks,gate-history,gate-detail,gate-edit-form,gate-validation}.ts`, `features/gate/gate-validation.spec.ts`, `e2e/gate.spec.ts`; sửa route gate.

**Interfaces:** `watchGateValidation(input$: Observable<GateInput>, source: DepotDataSource): Observable<LoadState<{containerNumber: string; checks: GateCheck[]}>>` debounce 300ms/switchMap; CommandMap gate actions và QueryMap gateHistory/gateTransaction, service suggestPosition. Query `tab=in|out|history`, `transaction`, `container`, `orderId`, `page`.

- [ ] Test giả lập request A chậm, B nhanh: `expect(lastChecks.containerNumber).toBe(numberB)` qua view model có number echo; chỉ checks cho input hiện tại có thể bật submit. Click submit khi snapshot đổi phải bị kiểm tra lại.
- [ ] E2E chọn ảo ẩn Bay/Row/Tier; suggest điền vị trí hợp lệ; input thiếu không ghi; chọn xuất theo booking tự hiện đúng DO; sửa audit; hủy chỉ bản gần nhất, confirm và phản ánh toàn bộ màn.
- [ ] Chạy unit validation và `npm.cmd run e2e -- gate.spec.ts`; FAIL trước implement.
- [ ] Implement 3 tabs, panel Chờ/Đạt/Lỗi, cảnh báo lý do disabled ngắn gọn, form 2/3 và checks 1/3; cột Bay/Row/Tier thẳng hàng. Lệnh chọn đầu form xuất; driver/vehicle/grade/condition bắt buộc; hai loại phương tiện dùng nhãn tương ứng.
- [ ] Chạy tests task và build, PASS; commit `feat(frontend): build gate forms and transaction history`.

## Task 8: Màn Sơ đồ bãi

**Files:** Tạo `features/yard/{yard-page,block-cards,yard-grid,slot-detail,virtual-block,move-container,resize-block}.ts`, `e2e/yard.spec.ts`; sửa route yard.

**Interfaces:** QueryMap.yard/yardSearch và CommandMap.move/resizeBlock. URL `block`, `tier`, `color=size|type|operator|booking`, `container`, `view=grid|list`.

- [ ] E2E 40ft hiển thị một card span 2; tìm trên tier khác chọn đúng block/tier và focus/highlight; tìm ảo mở list; ngoài bãi báo rõ. Move container bị đè/đích occupied fail, move hợp lệ update detail/list. Shrink bị chặn nêu đúng container.
- [ ] Chạy `npm.cmd run e2e -- yard.spec.ts`; FAIL trước implement.
- [ ] Implement block cards, tier tabs, 4 color modes/chú thích, list view dùng cùng projection, slot details, quy tắc hiển thị cố định và các dialog move/resize. Màu hãng ổn định theo ID, không theo thứ tự lọc. Không thêm nút 3D; hỗ trợ keyboard chọn ô.
- [ ] Chạy E2E task, unit yard và build, PASS; commit `feat(frontend): build interactive yard and virtual block screens`.

## Task 9: Khách hàng và hãng khai thác

**Files:** Tạo `demo/commands/partner-commands.ts`, `demo/queries/partner-query.ts`, `features/partners/{partners-page,customer-form,operator-form}.ts`, `demo/commands/partner-commands.spec.ts`, `e2e/partners.spec.ts`; sửa adapter mappings/route partners.

**Interfaces:** `saveCustomer(draft,input): Result<Customer>`, `saveOperator(draft,input): Result<Operator>`, `deactivatePartner(draft,{kind,id},today): Result<PartnersView>`, `projectPartners(snapshot,filters): PartnersView`. Save input có optional id cho edit; reject thay MST/mã hãng đã được tham chiếu.

- [ ] Test MST/mã trùng sau normalize bị chặn; ngưng khách có DO hiệu lực hoặc hãng có cont trong bãi fail; sửa tên phản ánh DO/containers nhờ ID reference; giữ nguyên lịch sử và ID.
- [ ] E2E hai tab/search/pagination, create/edit, confirm deactivate, inactive không có trong form mới; click “N DO” mở orders đúng customerId, 0 DO disable.
- [ ] Chạy unit partner và E2E partners; FAIL trước implement.
- [ ] Implement service/adapter/UI đủ contact/email/phone/address với labels, state feedback. Ngưng hoạt động thay xóa; không tạo CRUD users.
- [ ] Chạy tests task và build, PASS; commit `feat(frontend): build customer and operator management demo`.

## Task 10: Tổng quan, Báo cáo, Tồn bãi và Excel

**Files:** Tạo `demo/queries/{dashboard-query,report-query,inventory-query,metrics}.ts`, `shared/charts/gate-traffic-chart.ts`, `core/export/workbook-export.ts`, `features/dashboard/dashboard-page.ts`, `features/reports/reports-page.ts`, `features/inventory/inventory-page.ts`; tests queries/export, `e2e/analytics.spec.ts`; sửa adapter mappings và routes.

**Interfaces:** `projectDashboard(snapshot,now): DashboardView`; `projectReport(snapshot,filters,now): Result<ReportView>`; `projectInventory(snapshot,filters,now): InventoryView`; `trend(current:number,previous:number): {percent:number|null;label:string}`; `buildWorkbook(kind:'report'|'inventory', view:ReportView|InventoryView, now:Date): Promise<Uint8Array>`; `downloadWorkbook(...): Promise<Result<void>>`.

- [ ] Fixture 1 physical 20ft+1 physical 40ft+1 virtual 40ft trong sức chứa 10 TEU → inYard 3, physicalUsedTeu 3, occupancy 30%. Cổng đã hủy không vào traffic. Test kỳ so sánh cùng số ngày và previous=0 không chia 0.
- [ ] Test tuổi lưu 71h59m không vào >=3 ngày, 72h có; trung bình 1 chữ số, top5 ổn định; ngày báo cáo hai đầu theo UTC+7, từ>đến trả lỗi; report dwell dùng lifecycle xuất còn inventory chỉ active.
- [ ] Test export readback bằng ExcelJS: toàn bộ hàng lọc ngoài trang 1, số/date đúng kiểu, chuỗi `=1+1` có value string không formula, nhãn/filter/thời gian có; empty khóa xuất. E2E download `.xlsx` và mở chi tiết từ inventory/recent activities.
- [ ] Chạy unit query/export và E2E analytics; FAIL trước implement.
- [ ] Implement queries và adapter, SVG chart 7 ngày với focus tooltip/chú thích; capacity >80% cảnh báo; cards links. Report 2 loại như spec; inventory có hãng/type/size filters, summary/by-operator/top5; export dynamic import ExcelJS để không tăng initial bundle.
- [ ] Chạy tests task, full unit và build, PASS; commit `feat(frontend): add dashboard reports inventory and Excel export`.

## Task 11: Nghiệm thu xuyên màn hình, responsive và bàn giao

**Files:** Tạo `e2e/{workflow,persistence,responsive,feedback}.spec.ts`, `frontend/README.md`, `frontend/docs/demo-verification.md`; chỉnh đúng các component/style có lỗi quan sát được, sửa README root thêm link chạy frontend.

**Interfaces:** Dùng toàn bộ routes/services hiện có; E2E setup seed/reset riêng mỗi test, không phụ thuộc thứ tự chạy. `playwright.config.ts` webServer bind localhost, start một server frontend, mỗi test storage độc lập.

- [ ] Viết `workflow.spec.ts`: login→create container→gate in→create order/assign→move→gate out→verify dashboard/container/order/yard/report/inventory→cancel out→verify restored. Kiểm tra không có ghi trùng khi double-click.
- [ ] Viết persistence/feedback: reload giữ snapshot và URL, quota/hỏng schema có thông báo/reset confirm, failNextRead→retry, empty filters, clock boundary, logout/returnUrl, không ghi đè dữ liệu khi người dùng hủy confirm.
- [ ] Viết responsive test từng viewport spec; fixture tên khách 160 ký tự và tiếng Việt, dialog dài cuộn body riêng, footer visible, `expect(scrollWidth).toBeLessThanOrEqual(clientWidth)` cho document. Dialog Tab trap/Esc/restore focus; bảng chỉ overflow trong khung. Chạy riêng các test mới, FAIL nếu còn thiếu; không cố tạo fail giả khi hành vi đã đạt.
- [ ] Hoàn thiện mọi lỗi từ kiểm thử; kiểm tra trực quan các trang, form create/detail/assign, panel gate và bãi ở bốn viewport, zoom 125% bằng trình duyệt. Lưu ảnh minh chứng trong `frontend/docs/screenshots/`, ghi kết quả thật vào demo-verification, không chỉ ghi “đã kiểm tra”.
- [ ] Tại frontend chạy `npm.cmd test`, `npm.cmd run e2e`, `npm.cmd run build`; PASS không failed. Tại root chạy `git diff --check`, kiểm tra scope chỉ frontend/docs/.gitignore/README. Không cần chạy suite .NET nếu không thay backend.
- [ ] README ghi Node/npm, `npm.cmd ci`, start/build/test/e2e, cách cài browser `npx.cmd playwright install chromium`, credentials, reset, snapshot key/schema, cách demo error, lựa chọn G5/UTC+7/TEU, giới hạn nhiều tab và chưa nối API. Nếu bất kỳ kiểm tra không chạy được, ghi rõ giới hạn và bằng chứng đã có.
- [ ] Commit `feat(frontend): verify complete depot demo and document usage`. Review toàn bộ diff theo spec, mở preview demo và bàn giao link/cách chạy; không push/deploy tự động.

## Coverage và handoff

Spec 1–2 → tất cả tasks, không giảm màn; spec 3 → Task 1/5/11; spec 4–5 → Task 2/5; spec 6.1 → Task 4/5/10; spec 6.2 → Task 4/6; spec 6.3 → Task 4/7; spec 6.4 → Task 3/8; spec 6.5 → Task 9; spec 6.6 → Task 10; spec 7 → Task 5/11; spec 8 → các test task và Task 11.

Thực thi theo thứ tự 1→11 vì UI cần hợp đồng và kho dữ liệu chung. Duyệt kế hoạch này và chọn cách thực thi trước khi scaffold/cài dependency. Đề xuất **Native**: tác nhân chính triển khai tuần tự trong cuộc trao đổi, phù hợp các màn dùng cùng hợp đồng và tránh phân tán việc thay đổi snapshot. Có thể chọn **Subagent-driven** để mỗi task được một tác nhân triển khai và một reviewer kiểm tra riêng; nhiều lượt review hơn nhưng tăng chi phí ngữ cảnh. Review độc lập chỉ dispatch sau khi đã chọn phương thức thực thi phù hợp.
