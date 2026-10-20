# SnpDepotManagement: việc backend cần làm trước khi làm Angular

Tài liệu giao việc cho Gemini. Nguồn yêu cầu: `docs/design/u
i-requirements.md` và `docs/design/backend-requirements-from-ui.md`.

## Bối cảnh (dán đầu mỗi phiên)

- Repo SnpDepotManagement, .NET 10, Clean Architecture. Dùng CQRS tự viết (`ICommandHandler`/`IQueryHandler`), Result pattern, Minimal API, EF Core với PostgreSQL.
- Mỗi tính năng làm theo thứ tự Domain → Application → Infrastructure (config, migration) → Api → test.
- Mã lỗi mới phải đăng ký trong `ResultExtensions.StatusCodesByError`, nếu không sẽ trả 500.
- Thay đổi dữ liệu con của aggregate phải cập nhật bản ghi cha trong `AppDbContext.SaveChangesAsync`, theo mẫu hiện có cho `YardSlot` và `DeliveryOrderItem`.
- Mỗi task một migration. Không đổi giá trị số của enum đang lưu trong DB.
- Không được làm hỏng 415 test hiện có. Task nào sửa hành vi thì sửa test tương ứng.
- Cuối mỗi task cập nhật `endpoint-inventory` và Postman (`scripts/generate-postman.mjs`), rồi chạy lại toàn bộ test và báo kết quả.

## Hiện trạng: API chưa khớp UI đã duyệt

| UI cần | Backend hiện có |
|---|---|
| Gán container vào lệnh, giữ chỗ, gỡ, `displayStatus` | Không có Booking. Gate-out trừ thẳng số lượng của lệnh giao. Lệnh giao không có trạng thái (Còn hạn/Sắp hết hạn) |
| Danh sách container: search, lọc loại, vị trí, lệnh hiện tại, số theo loại | Chỉ lọc `ownerCode`, `size`, `condition` |
| `/gate/validate`, gợi ý vị trí, sửa/hủy giao dịch | Không có. Gate in/out nhận Guid thay vì số container, số lệnh, vị trí |
| Sơ đồ bãi: tô màu theo kích thước, loại, hãng, đặt chỗ; tìm container; chi tiết ô | Layout chỉ trả `{id, bay, row, tier, containerId, isOccupied}`. Chưa có endpoint liệt kê block |
| Tổng quan, `/lookups`, sửa/ngưng hoạt động khách và hãng, `/auth/me`, logout | Không có |

Các chỗ khác nhau về dữ liệu:

- Enum JSON đang dùng số (Dry = 85 vì `'U'` là 85), `Size` là 20 hoặc 40.
- `ContainerCondition` thiếu "Méo".
- Phân trang dùng `pageIndex` và `totalCount`, tài liệu UI dùng `page` và `total`.
- Thông điệp lỗi tiếng Anh, mã lỗi dạng `OrderRule.LineOperatorMismatch`. UI cần tiếng Việt kèm mã D3, G4…
- `MaxBays` là số bay lớn nhất, không phải số slot 20ft. Container 40ft chiếm 2 slot cùng `containerId`.
- `AuthResponseDto.ExpiresAt` là hạn của refresh token, không phải access token.

## Chốt với khách trước khi làm T3 và T4

- **Có cho xuất thẳng khi chưa giữ chỗ không (G5)?** -> **CÓ** (Theo thông lệ nghiệp vụ depot cảng trên thế giới & Việt Nam: cho phép xuất thẳng container phù hợp theo hạn ngạch của lệnh khi chưa qua bước giữ chỗ trước; cấu hình qua `Gate:AllowDirectShip = true`).
- **Lệnh hết hạn có tự giải phóng container đang giữ chỗ không?** -> **KHÔNG** (Không tự động giải phóng/hủy booking khi hết hạn, điều độ/nhân viên lệnh phải hủy thủ công hoặc gia hạn lệnh).
- **Số kiểm tra tính "dư 10 = 0" hay "X"?** -> **DÙNG "X"** (Ký tự thứ 11 theo ISO 6346 Modulo 11 khi dư 10 được tính là 'X', đồng thời chấp nhận '0' tương thích).
- **Có giữ hai trạng thái "Đang cấp điện" và "Đang kiểm tra" không?** -> **CÓ** (Giữ cả hai trạng thái: `ReeferPowered` cho cont lạnh và `UnderInspection` cho cont đang kiểm tra PTI/giám định).

## T0. Nền kỹ thuật

**Việc cần làm:** enum JSON dạng chữ, thêm tình trạng "Méo", thông điệp lỗi tiếng Việt kèm `ruleCode`, bổ sung auth, chuẩn bị môi trường dev.

**Hướng giải quyết**

- Thêm `JsonStringEnumConverter` qua `ConfigureHttpJsonOptions`, không cho nhận số. Không đổi giá trị enum trong DB. Sửa Postman vì hiện đang gửi `"Type": 85`.
- Thêm `Warped` vào cuối `ContainerCondition`.
- Sửa `Description` của `Error` sang tiếng Việt ngay tại nơi tạo lỗi để có một nguồn duy nhất. Ở lớp Api thêm từ điển `code → ruleCode` (D2, G4…) và đưa vào ProblemDetails.
- Giữ nguyên `pageIndex` và `totalCount`. Sửa tài liệu UI cho khớp code, không đổi code.
- Auth:
  - Thêm `GET /auth/me` (user, vai trò, tenant, tên depot) và `POST /auth/logout` (gọi `RevokeRefreshToken`).
  - Trả thêm hạn access token.
  - Dùng `RefreshTokenExpirationDays` thay cho giá trị 7 ngày gõ cứng.
  - Dùng `AddRateLimiter` giới hạn `/auth/login` theo IP.
- Dev: thêm `Cors__AllowedOrigins__0=http://localhost:4200` vào `.env.example` và thêm service API vào docker-compose.

**Xong khi:** Postman chạy với enum chữ, `/auth/me` và logout có test, test rate limit đạt.

## T1. Sửa Domain và validate hiện có

**Việc cần làm:** `OwnerCode` suy ra từ số container, validate ISO và ngày sản xuất, kiểm tra dữ liệu khi tạo lệnh giao, sửa quy tắc xếp chồng.

**Hướng giải quyết**

- Bỏ `OwnerCode` khỏi `RegisterContainerCommand`. Thêm Grade và Condition, mặc định A và Normal.
- Validate ISO bằng regex `^\d{2}[A-Z]\d$`. Chữ số đầu 2 ứng với 20ft, 4 ứng với 40ft. Ngày sản xuất không lớn hơn ngày hiện tại theo `DepotCalendar`.
- Tạo lệnh giao:
  - Hãng và khách phải có trong danh mục. Lấy tên khách từ danh mục thay vì tin client.
  - Hạn lệnh không ở quá khứ.
  - Chặn dòng trùng (cỡ, loại) bằng mã lỗi mới.
- Thêm rule `StackCompatibilityRule` dùng chung cho GateIn, Place, Relocate và FindFreeSlots, để gợi ý và kiểm tra không lệch nhau.
  - 20ft chỉ đặt lên 20ft.
  - 40ft đặt lên một 40ft cùng vị trí, hoặc lên hai 20ft.
  - Cách làm: nạp kích thước container ở hai ô bên dưới. Hai ô cùng `containerId` thì container đó phải là 40ft.

**Xong khi:** có test cho 20ft lên 40ft, 40ft lên 20ft cộng 40ft lệch, và 40ft lên 40ft đúng vị trí.

## T2. Hãng khai thác và tiền tố container

**Việc cần làm:** tách `LineOperator.Code` khỏi `OwnerCode` (tài liệu ghi rõ EGL khác EGH).

**Hướng giải quyết**

- Thêm bảng `line_operator_owner_codes (line_operator_id, owner_code)`, duy nhất theo tenant.
- Rule `ContainerBelongsToLineOperator` nhận hãng đã tra từ tiền tố, rồi so với `LineOperator` của lệnh giao.
- UI hiển thị hãng đã tra này.

**Xong khi:** có test cho container EGH xuất theo lệnh hãng EGL.

## T3. Booking (phần lõi)

**Việc cần làm:** gán container vào lệnh, gỡ, và xuất theo đặt chỗ.

**Hướng giải quyết**

- Thêm entity `DeliveryOrderBooking`: `OrderId`, `ItemId`, `ContainerId`, `Status` (Reserved, Shipped, Cancelled, Expired), `ReservedAt`, `ShippedAt`, `CancelledAt`.
- Chỉ mục duy nhất có điều kiện trên `container_id` khi `Status = Reserved`. Đặt tên constraint cố định, bắt `UniqueViolation` theo tên constraint để trả `Booking.ContainerAlreadyReserved` (409).
- Giữ `DeliveredQuantity` hiện có làm số Shipped. Số Reserved tính từ Booking, không lưu trùng. Còn lại = Tổng − Reserved − Delivered.
- Thêm vào `DeliveryOrder` các phương thức `Reserve`, `Release` và trạng thái tính ra (Còn hạn, Sắp hết hạn, Hết hạn, Đã giao đủ). "Sắp hết hạn" nghĩa là còn 3 ngày trở xuống theo ngày depot.
- Hai command: `AssignContainersToOrder(orderNumber, containerNumbers[])` chạy một giao dịch, thành công hết hoặc không gán gì; và `ReleaseBooking(id)`.
- Xử lý hết hạn theo hai lớp:
  - Khi gán, nếu container có Booking Reserved của lệnh đã hết hạn thì chuyển sang Expired ngay trong cùng giao dịch (nếu không, chỉ mục duy nhất sẽ chặn gán sang lệnh khác).
  - Thêm `BackgroundService` chạy hằng ngày để dọn các Booking còn sót.
- Sửa GateOut:
  - Container đã giữ chỗ cho lệnh X thì chỉ xuất được cho lệnh X, ngược lại trả G4.
  - Xuất xong chuyển Booking sang Shipped.
  - Container chưa giữ chỗ: dùng cờ cấu hình `Gate:AllowDirectShip`. Nếu bật thì tự tạo Booking Shipped, nếu tắt thì từ chối. Cờ này chờ khách chốt câu G5.

**Xong khi:** pass các kịch bản 2, 3, 4, 6, 8, 10 ở mục 9 của `backend-requirements-from-ui.md`, kèm test đồng thời gán cùng một container.

## T4. Container: trạng thái hiển thị và danh sách

**Việc cần làm:** `displayStatus`, vị trí, lệnh hiện tại, search, lọc, summary, lịch sử lệnh.

**Hướng giải quyết**

- Thêm `VirtualBlockId` (nullable) vào `ContainerLifecycle` để container nằm ở block ảo. Hiện không làm được vì `CurrentSlotId` bắt buộc có.
- Dùng một projection EF (left join lifecycle đang hoạt động, slot, Booking Reserved) thay vì lưu cột trạng thái. `displayStatus` tính theo bảng thứ tự ưu tiên ở mục 2.2 của tài liệu.
- Bỏ qua hai trạng thái "Đang cấp điện" và "Đang kiểm tra" cho đến khi khách xác nhận.
- Search dùng `ILike` trên số container và ISO. Thêm các chỉ mục theo `container_id` cho lifecycle, slot và booking (dùng chỉ mục có điều kiện).
- `GET /containers/summary` dùng một truy vấn GROUP BY với cùng bộ lọc.
- `GET /containers/{number}/orders` trả lệnh hiện tại, tóm tắt (số khách đã qua, số lần xuất, khách đặt gần nhất) và lịch sử.
- Chạy lại benchmark 20.000 container trong `scripts/benchmarks`.

**Xong khi:** các truy vấn mới nằm trong ngưỡng của `performance-evidence` hiện có.

## T5. Cổng nhập/xuất

**Việc cần làm:** validate, nhập/xuất theo số, sửa, hủy, gợi ý vị trí.

**Hướng giải quyết**

- Thêm `GateTransaction` (có `Status` và `PreviousStatus`) và `GateTransactionChange`. Giữ `ContainerMovement` làm sổ cái chỉ thêm. Khi hủy thì thêm movement bù trừ (kiểu `GateInCancelled`, `GateOutCancelled`), không sửa movement cũ.
- Tách bước đánh giá rule thành `GateRuleEvaluator`, trả toàn bộ danh sách kiểm tra `{ruleCode, state, label, detail}` chứ không dừng ở lỗi đầu tiên như `RuleEngine` hiện tại. `POST /gate/validate` và hai command ghi dùng chung evaluator này. Validate không thay đổi dữ liệu và không trả 4xx cho vi phạm nghiệp vụ.
- Gate in/out nhận `containerNumber`, `orderNumber`, vị trí `{blockCode, bay, row, tier}` hoặc `virtualBlockCode`, rồi server tự tra slot. Thêm trường tài xế. Gợi ý vị trí trả đủ thông tin để ghi nhận.
- Sửa chỉ cho phép phương tiện, tài xế, Grade, Condition, và ghi lại từng thay đổi.
- Hủy chỉ áp dụng cho giao dịch Active gần nhất của container. Hủy xuất phải trả container về đúng vị trí cũ, nếu ô đã bị chiếm thì trả 409 `G9`.

**Xong khi:** pass các kịch bản 5, 7, 8, 9, 14.

## T6. Sơ đồ bãi

**Việc cần làm:** danh sách block, layout giàu dữ liệu, tìm container, block ảo, mở/thu hẹp.

**Hướng giải quyết**

- `YardBlockDto` đã có nhưng thiếu query và endpoint liệt kê. Thêm `GET /yard/blocks`. Occupied/Total slots chính là TEU vì 40ft chiếm hai slot.
- Layout: thêm tham số `tier` để giảm kích thước phản hồi, và thêm mảng `containers[]` (số, loại, cỡ, hãng, Grade, tóm tắt Booking). Slot chỉ giữ `containerId` rồi tham chiếu sang mảng này. Cho phép truy cập theo cả `id` và `code`.
- `GET /yard/search?containerNumber=` trả trạng thái InBlock, Virtual, OutOfYard hoặc NotFound kèm vị trí.
- Thêm danh sách container của block ảo, và thao tác đặt từ block ảo vào ô thật.
- Lỗi thu hẹp nêu rõ container và vị trí vi phạm đầu tiên (Y6).
- Trả thêm số bay lẻ (`oddBays`) để UI không phải tự đổi từ `MaxBays`.

**Xong khi:** pass kịch bản 11, tìm container nhảy đúng ô.

## T7. Danh mục, đối tác, tổng quan

**Hướng giải quyết**

- `GET /lookups` sinh từ enum trong code, có nhãn tiếng Việt, cache.
- Customer và LineOperator: thêm `IsActive`, PUT, `deactivate`, và trả `orderCount`, `activeOrderCount`, `containersInYard`. Chặn ngưng hoạt động nếu còn lệnh hiệu lực (409). Thêm chỉ mục `(tenant_id, customer_tax_code)` vì lệnh giao đang liên kết với khách bằng chuỗi MST.
- Dashboard:
  - `summary`, `gate-traffic` (gom theo ngày UTC+7 bằng `AddHours(7).Date`), `block-capacity`.
  - Chỉ đếm giao dịch Active.
  - Cache 30 giây qua `ICacheService`, khớp chu kỳ polling của UI.
- Tên depot: thêm entity `Depot` nhỏ (id là tenantId), lấy từ cấu hình lúc seed.

## T8. Dữ liệu mẫu và nghiệm thu

- Thêm seed chỉ chạy ở Development (cờ `Seed:DemoData`). Có hàm sinh số container đúng chữ số kiểm tra ISO 6346.
- Dữ liệu theo quy ước mục 6 của `ui-requirements.md`: container qua nhiều lệnh của nhiều khách, container chưa từng gán, container xuất nhiều lần.
- Viết integration test cho 14 kịch bản nghiệm thu.
- Cập nhật số liệu test trong README.

## Thứ tự giao

1. Chạy T0 trước, vì mọi task sau đều phụ thuộc phần nền.
2. T1 và T2 chạy song song được, T3 cần cả hai.
3. Sau đó làm lần lượt T3 → T4 → T5 → T6.
4. T7 làm được song song từ sau T0.
5. T8 làm sau mỗi nhóm hoàn tất.
6. Mốc bắt đầu Angular: xong T0, T1, T3, T4.

Giao từng task trong một phiên riêng, kèm phần bối cảnh ở đầu và yêu cầu Gemini báo kết quả chạy test cuối phiên.

## Các lỗi và rủi ro đã tìm thấy khi review (tham chiếu)

1. `OwnerCode` do client gửi khi tạo container, có thể lách rule "đúng hãng" khi xuất (xử lý ở T1, T2).
2. Thiếu validate ISO khớp kích thước và ngày sản xuất không ở tương lai (T1).
3. Lệnh giao: hãng và khách là chuỗi tự do, MST không validate, hạn ở quá khứ vẫn tạo được, cho phép dòng trùng (T1).
4. `SlotBelowMustBeOccupiedRule` không xét kích thước container bên dưới, nên 20ft vẫn đặt được lên 40ft (T1).
5. Auth: một refresh token cho mỗi user, hạn 7 ngày gõ cứng, không có logout, không giới hạn đăng nhập sai (T0).
6. Môi trường dev: seeder chỉ tạo role và admin, docker-compose chưa có API, CORS mặc định rỗng (T0, T8).
