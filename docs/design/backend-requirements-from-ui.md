# Yêu cầu backend rút ra từ giao diện đã duyệt (SnpDepotManagement)

Nguồn: `ui-requirements.md` (yêu cầu giao diện, là nguồn chính) và `docs/design/App.approved.tsx` (bản xem trước đã duyệt, chỉ mang tính tham chiếu). Tài liệu này mô tả những gì backend phải cung cấp để giao diện chạy với dữ liệu thật: dữ liệu, quy tắc nghiệp vụ, use case, endpoint, lỗi, và ảnh hưởng đến kế hoạch 4 tuần.

Nguyên tắc:
- **Mọi quy tắc nghiệp vụ nằm ở Domain/Application.** Giao diện chỉ báo sớm.
- **Mọi con số hiển thị trên giao diện (số lượng, tỷ lệ, số theo loại, các ô tóm tắt) do API trả về**, giao diện không tự cộng hay tự tính.
- Mỗi thao tác của giao diện phải có endpoint tương ứng. Mục 5b liệt kê đối chiếu từng màn hình.

## 1. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| Block / Bay / Row / Tier | Khối bãi / chiều dài / hàng ngang / tầng chồng |
| Slot | Một vị trí 20ft theo bay lẻ (slot 1 = bay 01, slot 2 = bay 03, ...) |
| Hãng khai thác (Operator) | Hãng tàu ra lệnh giao container, không phải khách hàng |
| Khách hàng (Customer) | Bên nhận container, có MST và tên, xuất hiện trên lệnh giao |
| DO / Lệnh giao | Lệnh của hãng khai thác cho phép giao container rỗng ra ngoài |
| Booking / Đặt chỗ | Việc giữ một container cụ thể cho một dòng của lệnh giao |
| Block ảo | Block không quản lý theo bay/row/tier, dùng điều hành đặc biệt |

## 2. Mô hình dữ liệu

### 2.1 Danh mục

**Operator** (hãng khai thác): `Id`, `Code` (3 chữ cái, duy nhất), `Name`, `Contact` (email liên hệ), `IsActive` (giao diện hiện "Hoạt động" / "Ngưng hoạt động"). **`Code` của hãng không phải tiền tố số container**: tiền tố (`OwnerCode`) là chủ sở hữu container và lưu riêng trên Container (ví dụ hãng Evergreen có `Code` EGL nhưng container mang tiền tố EGH).

**Customer** (khách hàng): `Id`, `TaxCode` (MST, 10 chữ số, duy nhất), `Name`, `Email`, `Phone`, `Address`, `IsActive`.

Operator và Customer **không xóa cứng**, chỉ ngưng hoạt động (`IsActive = false`). Không cho ngưng hoạt động hoặc xóa đối tác đang có lệnh giao còn hiệu lực.

**Depot**: `Id`, `Name`. Giao diện hiển thị tên depot ở thanh trên cùng.

**YardBlock**: `Id`, `Code` (A, B, C, D...), `Kind` (`Grid` | `Virtual`), `Bays` (số slot 20ft, tức số bay lẻ), `Rows`, `MaxTiers`. Block `Virtual` không dùng ba số cuối, nhưng có `Name` và `Description` (ví dụ block ảo VB-01 dùng làm danh sách container chờ phân vị trí).

### 2.2 Container

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `Number` | string(11), duy nhất | 3 chữ cái chủ sở hữu + 1 chữ cái loại + 6 chữ số + 1 số kiểm tra |
| `Type` | enum | Dry, Reefer, OpenTop, FlatRack, Bunker, Ventilated, Specialized |
| `IsoCode` | string(4) | Dạng `2 số + 1 chữ + 1 số`, ví dụ 45G1 |
| `Size` | enum 20 / 40 | Phải khớp chữ số đầu của ISO (2 = 20ft, 4 = 40ft) |
| `OperatorId` | FK | Hãng khai thác |
| `OwnerCode` | string(3) | Tính từ số container, không nhập tay |
| `Grade` | enum A / B / C | Phân loại |
| `Condition` | enum | Bình thường, Móp, Méo, Cong, Hư hỏng khác |
| `MaxWeightKg`, `TareWeightKg` | int | Tare nhỏ hơn Max |
| `ManufactureMonth` | yyyy-MM | Không ở tương lai |
| `Status` | enum | `AwaitingEntry` (Chờ nhập: có trong danh mục, chưa qua cổng nhập), `InYard` (đang trong bãi, kể cả block ảo), `Exited` (Đã xuất: đã ra khỏi bãi). "Ngoài bãi" = không `InYard` |
| `ReeferPowered` | bool | Chỉ áp dụng cho Reefer: đang cấp điện |
| `UnderInspection` | bool | Đang kiểm tra (kèm `InspectionNote`) |
| `Position` | value object, nullable | `BlockId`, `Bay`, `Row`, `Tier`. Rỗng khi ngoài bãi hoặc ở block ảo |
| `VirtualBlockId` | FK, nullable | Khi container ở block ảo |
| `Note`, `GateInAt` | | |
| `RowVersion` | concurrency token | |

Container **không** chứa thông tin lệnh giao. Quan hệ với lệnh giao chỉ qua Booking.

**Trạng thái hiển thị (`DisplayStatus`)** do API tính và trả về trong danh sách, chi tiết và sơ đồ bãi. Giao diện không tự suy ra. Thứ tự ưu tiên:

| Điều kiện | Hiển thị |
|---|---|
| `Status = AwaitingEntry` | Chờ nhập |
| `Status = Exited` | Đã xuất |
| `InYard` và `UnderInspection` | Đang kiểm tra |
| `InYard`, Reefer và `ReeferPowered` | Đang cấp điện |
| `InYard` và đang ở block ảo | Chờ phân vị trí |
| `InYard` và có Booking `Reserved` | Chờ xuất |
| Còn lại | Trong bãi |

`ReeferPowered` và `UnderInspection` hiện giao diện chỉ hiển thị, chưa có nút bật tắt. Cần quyết định: thêm thao tác ở chi tiết container (endpoint `PUT /containers/{number}/flags`) hoặc bỏ hai trạng thái này khỏi giao diện (xem mục 10).

### 2.3 Lệnh giao và đặt chỗ

**DeliveryOrder**: `Id`, `Number` (duy nhất), `CustomerId`, `OperatorId`, `ExpiryDate` (ngày cuối được giao, hiệu lực hết ngày đó), `Vessel` (chuyến tàu xuất), `CreatedAt`.

**DeliveryOrderLine**: `Id`, `DeliveryOrderId`, `Type`, `Size`, `Quantity`. Mỗi lệnh có không quá một dòng cho một cặp (Type, Size).

**Booking** (đặt chỗ):

| Trường | Ghi chú |
|---|---|
| `Id` | |
| `DeliveryOrderLineId` | FK |
| `ContainerId` | FK |
| `Status` | `Reserved` (đang giữ chỗ), `Shipped` (đã xuất), `Cancelled` (đã hủy), `Expired` (hết hạn) |
| `ReservedAt`, `ShippedAt`, `CancelledAt` | |
| `CreatedBy` | |
| `RowVersion` | |

Ràng buộc: chỉ mục duy nhất có điều kiện `(ContainerId) WHERE Status = 'Reserved'`, đảm bảo một container chỉ có một đặt chỗ đang hiệu lực.

Số liệu của dòng lệnh **không lưu trùng**, tính từ Booking:
- `Reserved` = số Booking `Reserved`
- `Shipped` (đã giao) = số Booking `Shipped`
- `Remaining` = `Quantity − Reserved − Shipped`

Lịch sử lệnh của một container chính là các Booking của nó. Không cần bảng riêng.

### 2.4 Giao dịch cổng

**GateTransaction**: `Id`, `Direction` (`In` | `Out`), `OccurredAt`, `ContainerId`, `OperatorId` (chụp lại lúc giao dịch), `VehicleType` (`Truck` | `Barge`), `VehicleNo`, `Driver`, `Grade`, `Condition`, `Position` (vị trí gán khi nhập, `Out` thì lưu vị trí cũ để hoàn lại), `VirtualBlockId`, `BookingId` (khi xuất), `CreatedBy`, `Status` (`Active` | `Cancelled`), `PreviousStatus` (trạng thái container trước giao dịch, để hủy giao dịch hoàn lại đúng), `RowVersion`.

**GateTransactionChange** (lịch sử sửa): `Id`, `GateTransactionId`, `At`, `By`, `Field`, `OldValue`, `NewValue`.

## 3. Quy tắc nghiệp vụ (Domain)

Mỗi quy tắc có mã để dùng trong lỗi trả về, test, và tham chiếu giữa các lớp.

### Container
- **C1** Số container đúng định dạng 4 chữ cái + 7 chữ số và đúng số kiểm tra ISO 6346 (dư 10 tính là 0). *Cần xác nhận với khách: tài liệu ghi "dư 10 là X".*
- **C2** Số container duy nhất.
- **C3** ISO đúng dạng và khớp kích thước.
- **C4** Trọng lượng vỏ > 0 và < trọng lượng tối đa. Ngày sản xuất không ở tương lai.
- **C5** Thông tin tĩnh (số, loại, ISO, cỡ, trọng lượng, ngày sản xuất) không bị thay đổi bởi lệnh giao hay giao dịch cổng.

### Bãi
- **Y1** Bay lẻ chứa 20ft; bay chẵn chứa 40ft. Container 20ft ở bay `b` chiếm slot `(b+1)/2`. Container 40ft ở bay chẵn `b` chiếm slot `b/2` và `b/2 + 1`.
- **Y2** Vị trí nằm trong block: slot ≤ `Bays`, row < `Rows`, tier ≤ `MaxTiers`.
- **Y3** Mỗi (block, row, tier, slot) chỉ có một container.
- **Y4** Tầng trên phải được tầng dưới đỡ hết. 40ft đặt lên đúng 1 container 40ft cùng vị trí hoặc 2 container 20ft. 20ft không đặt lên 40ft.
- **Y5** Khi xuất, container không được bị container khác nằm đè ở tier ngay trên.
- **Y6** Thu hẹp block (giảm `Bays`, `Rows`, `MaxTiers`) bị chặn nếu còn container nằm ngoài kích thước mới. Lỗi nêu rõ container và vị trí đầu tiên vi phạm.
- **Y7** Block ảo không có vị trí bay/row/tier. Container ở block ảo vẫn tính là `InYard`.

### Lệnh giao và đặt chỗ
- **D1** Trạng thái lệnh tính ra, không lưu: `Đã giao đủ` (tổng giao = tổng số lượng), `Hết hạn` (qua hết ngày `ExpiryDate`), `Sắp hết hạn` (còn 3 ngày trở xuống), `Còn hạn`. Thứ tự ưu tiên: Đã giao đủ, Hết hạn, Sắp hết hạn, Còn hạn.
- **D2** Chỉ gán container vào lệnh khi: container `InYard`, cùng `OperatorId` với lệnh, đúng (Type, Size) của một dòng còn `Remaining > 0`, container chưa có Booking `Reserved`, lệnh chưa hết hạn và chưa giao đủ.
- **D3** Một container tối đa một Booking `Reserved` (đảm bảo cả bằng chỉ mục duy nhất).
- **D4** Gỡ container khỏi lệnh chỉ khi Booking đang `Reserved`; chuyển sang `Cancelled`, giữ lịch sử.
- **D5** Lệnh hết hạn: Booking `Reserved` coi như `Expired` và giải phóng container. Cách thực hiện: tính khi đọc (Booking `Reserved` mà lệnh đã hết hạn thì hiển thị `Expired`), kèm tác vụ nền đổi trạng thái thật theo lịch. Các truy vấn "đang giữ chỗ" phải loại trừ Booking thuộc lệnh đã hết hạn.
- **D6** Không giảm `Quantity` của dòng lệnh xuống dưới `Reserved + Shipped`.

### Cổng
- **G1** Nhập: container tồn tại, không `InYard` (`AwaitingEntry` hoặc `Exited`), số container hợp lệ (C1). Gán vị trí hợp lệ (Y1 đến Y4) hoặc block ảo.
- **G2** Xuất: container tồn tại, `InYard`, không bị đè (Y5).
- **G3** Xuất cần lệnh giao: lệnh còn hạn, cùng hãng với container, còn số lượng đúng loại/cỡ.
- **G4** Container đã có Booking `Reserved` cho lệnh X thì chỉ xuất được cho lệnh X. Xuất cho lệnh khác bị từ chối.
- **G5** Container chưa giữ chỗ mà được xuất bằng một lệnh hợp lệ: hệ thống tạo Booking `Shipped` cho lệnh đó. *Cần xác nhận với khách: có cho xuất thẳng không, hay bắt buộc gán trước.*
- **G6** Xuất thành công (một giao dịch duy nhất): Booking → `Shipped` + `ShippedAt`; container → `Exited`, xóa vị trí; ghi `GateTransaction`.
- **G7** Nhập thành công: container → `InYard`, đặt vị trí, cập nhật Grade/Condition, `GateInAt`.
- **G8** Sửa bản ghi: chỉ được sửa `VehicleNo`, `Driver`, `Grade`, `Condition`, ghi ghi chú. Không sửa số container, loại giao dịch. Mỗi thay đổi ghi `GateTransactionChange`.
- **G9** Hủy giao dịch: chỉ giao dịch `Active` gần nhất của container. Hủy `In`: container về `PreviousStatus` (`AwaitingEntry` hoặc `Exited`), xóa vị trí. Hủy `Out`: hoàn container về vị trí cũ (phải còn hợp lệ theo Y3/Y4, nếu vị trí đã bị chiếm thì lỗi), Booking về `Reserved` (nếu lệnh hết hạn thì về `Expired`).

## 4. Use case (lớp Application)

### Truy vấn

| Use case | Dùng ở màn hình | Dữ liệu trả về |
|---|---|---|
| `ListContainers` | Container | Phân trang; lọc theo loại, từ khóa (số/ISO), gán lệnh (tất cả / đã gán / chưa gán). Mỗi dòng kèm **lệnh hiện tại** (số lệnh, khách, hạn lệnh, nhãn trạng thái) |
| `GetContainer` | Chi tiết, tab Thông tin | Đủ trường tĩnh và vị trí hiện tại |
| `GetContainerOrders` | Chi tiết, tab Lệnh giao | Lệnh hiện tại, tóm tắt (số khách đã qua, số lần xuất, khách đặt gần nhất), lịch sử Booking mới nhất trước |
| `ListEligibleOrdersForContainer` | Gán vào lệnh giao (từ container) | Các lệnh thỏa D2 |
| `ListDeliveryOrders` | Lệnh giao | Phân trang; lọc `status` (`active`, `expiring`), `customerTaxCode`. Kèm số tổng theo trạng thái để hiện các ô tóm tắt |
| `GetDeliveryOrder` | Chi tiết lệnh | Thông tin lệnh, từng dòng kèm Reserved/Shipped/Remaining, danh sách container đã gán kèm vị trí và trạng thái Booking |
| `ListEligibleContainersForOrder` | Gán container | Container thỏa D2 |
| `ValidateGate` | Panel Kiểm tra nghiệp vụ và các ô tự điền | `checks[]` (mỗi mục `state` = `ok`/`error`/`pending`, `label`, `detail`, `ruleCode`), cộng thêm `container` (loại, ISO, cỡ, hãng khai thác, `DisplayStatus`, vị trí hiện tại, đặt chỗ hiện tại) để giao diện điền ô "Hãng khai thác" chỉ đọc, và `order` (khách hàng, chuyến tàu, số lượng còn lại theo từng dòng) cho thẻ thông tin lệnh. Khi chưa nhập số container, giao diện tự hiện "Chờ nhập…" và không gọi API |
| `ListGateTransactions` | Lịch sử cổng | Phân trang, kèm vị trí, lệnh, nhân viên, trạng thái |
| `SuggestPosition` | Gợi ý vị trí khi nhập | Vị trí hợp lệ đầu tiên (hoặc rỗng) |
| `ListYardBlocks` | Tổng quan bãi | Mỗi block: cấu hình, số container, % lấp đầy (theo TEU); block ảo: số container |
| `GetYardBlockTier` | Sơ đồ bãi theo tier | Cấu hình block và danh sách vị trí của tier, mỗi vị trí kèm tóm tắt container (số, loại, ISO, cỡ, hãng) và tóm tắt Booking |
| `GetYardBlockContainer` | Chi tiết ô được chọn | Container, vị trí, lệnh giao, có bị đè không |
| `ListVirtualBlockContainers` | Block ảo | Danh sách container kèm ghi chú |
| `ListCustomers` | Khách hàng | Kèm `OrderCount` và `ActiveOrderCount` mỗi khách |
| `ListOperators` | Hãng khai thác | Kèm số container đang lưu bãi |
| `GetContainerSummary` | Container (thẻ lọc theo loại) | Tổng số container và số theo từng loại, theo bộ lọc hiện tại; số đã gán / chưa gán |
| `SearchYardContainer` | Sơ đồ bãi (ô tìm container) | Từ số container trả về vị trí (block, bay, row, tier) hoặc block ảo hoặc "ngoài bãi" |
| `GetDashboardSummary` | Tổng quan (4 ô số) | Container trong bãi, nhập hôm nay, xuất hôm nay, tỷ lệ lấp đầy, mỗi số kèm xu hướng so với hôm qua; số lệnh sắp hết hạn; số container giữ chỗ cho lệnh sắp hết hạn |
| `GetGateTraffic` | Tổng quan (biểu đồ 7 ngày) | Mỗi ngày: số nhập, số xuất |
| `GetBlockCapacity` | Tổng quan (công suất bãi) | Mỗi block: đã dùng, sức chứa, % |
| `GetOperatorOutputReport` | Báo cáo | Theo khoảng ngày: tổng lượt nhập, tổng lượt xuất, thời gian lưu bãi trung bình, và bảng theo hãng (nhập, xuất, đang lưu bãi, tổng) kèm xu hướng so với kỳ trước |
| `ExportOperatorOutputReport` | Báo cáo (Xuất báo cáo) | File Excel của báo cáo trên |
| `GetLookups` | Mọi form và bộ lọc | Danh sách giá trị cố định: loại container, kích thước, phân loại, tình trạng, loại phương tiện, trạng thái lệnh, trạng thái đặt chỗ |
| `Login`, `GetCurrentUser` | Đăng nhập, thanh trên cùng | Token, tên người dùng, vai trò, depot |

### Lệnh (thay đổi dữ liệu)

`CreateContainer`, `UpdateContainer` (chỉ nếu giao diện có sửa), `CreateDeliveryOrder`, `UpdateDeliveryOrder` (nếu cho sửa lệnh), `DeactivateCustomer`, `DeactivateOperator`, `AssignContainersToOrder`, `ReleaseBooking`, `RecordGateIn`, `RecordGateOut`, `UpdateGateTransaction`, `CancelGateTransaction`, `ResizeYardBlock`, `CreateCustomer`, `UpdateCustomer`, `CreateOperator`, `UpdateOperator`.

`ValidateGate` dùng **chung hàm quy tắc** với `RecordGateIn`/`RecordGateOut`, để giao diện và lệnh ghi không bao giờ lệch nhau.

## 5. Api (REST)

Đường dẫn gợi ý, tên có thể đổi theo quy ước repo. Tất cả trả JSON; lỗi dạng ProblemDetails (mục 6).

| Màn hình | Method và đường dẫn | Ghi chú |
|---|---|---|
| Container | `GET /containers?search=&type=&status=&assigned=all\|yes\|no&page=&pageSize=` | Mỗi dòng có `displayStatus` và `currentOrder`. `status` lọc theo `DisplayStatus` |
| | `PUT /containers/{number}/flags` | `reeferPowered`, `underInspection`, `note`. Chỉ khi quyết định giữ hai trạng thái này |
| | `POST /containers` | Không nhận trường lệnh giao |
| | `GET /containers/summary` (cùng bộ lọc như danh sách) | Số theo loại cho các thẻ lọc, tổng, số đã/chưa gán lệnh |
| | `GET /containers/{number}` | |
| | `GET /containers/{number}/orders` | Lệnh hiện tại + tóm tắt + lịch sử |
| | `GET /containers/{number}/eligible-orders` | |
| | `POST /containers/{number}/bookings` | Body: `deliveryOrderNumber`. Gán một container |
| Lệnh giao | `GET /delivery-orders?status=active\|expiring&customerTaxCode=&page=` | Trả thêm `summary: { active, expiring }` |
| | `POST /delivery-orders` | Body có customer, operator, expiry, vessel, lines |
| | `GET /delivery-orders/{number}` | |
| | `GET /delivery-orders/{number}/eligible-containers` | |
| | `POST /delivery-orders/{number}/bookings` | Body: `containerNumbers[]`. Gán nhiều |
| | `DELETE /bookings/{id}` | Gỡ (chuyển `Cancelled`) |
| Cổng | `POST /gate/validate` | Body như phiếu nhập/xuất, trả danh sách kiểm tra |
| | `POST /gate/in`, `POST /gate/out` | |
| | `GET /gate/transactions?page=` | |
| | `PATCH /gate/transactions/{id}` | Chỉ các trường được phép sửa |
| | `POST /gate/transactions/{id}/cancel` | |
| | `GET /gate/suggest-position?containerNumber=` | |
| Bãi | `GET /yard/blocks` | |
| | `GET /yard/blocks/{code}/tiers/{tier}` | |
| | `GET /yard/blocks/{code}/containers/{number}` | |
| | `PUT /yard/blocks/{code}` | Mở rộng/thu hẹp (`bays`, `rows`, `maxTiers`) |
| | `GET /yard/virtual-blocks/{code}/containers` | |
| | `GET /yard/search?containerNumber=` | Ô tìm container; trả vị trí và đường dẫn nhảy tới |
| Đối tác | `GET /customers`, `POST /customers`, `PUT /customers/{taxCode}`, `POST /customers/{taxCode}/deactivate` | Có `orderCount`, `activeOrderCount` |
| | `GET /operators`, `POST /operators`, `PUT /operators/{code}`, `POST /operators/{code}/deactivate` | Có `containersInYard` |
| Tổng quan | `GET /dashboard/summary` | 4 ô số + xu hướng + số lệnh sắp hết hạn |
| | `GET /dashboard/gate-traffic?days=7` | Chuỗi theo ngày |
| | `GET /dashboard/block-capacity` | Mỗi block: đã dùng, sức chứa, % |
| | `GET /gate/transactions?page=1&pageSize=5` | Hoạt động gần đây (dùng lại API lịch sử) |
| Báo cáo | `GET /reports/gate-output?from=&to=` | Ô số + bảng theo hãng. Ô "Loại báo cáo" của giao diện hiện chỉ có một loại (`gate-output`); thêm tham số `type` khi có báo cáo khác |
| | `GET /reports/gate-output/export?from=&to=&format=xlsx` | Tải file |
| Dùng chung | `GET /lookups` | Danh sách giá trị cố định |
| | `POST /auth/login`, `POST /auth/logout`, `GET /auth/me` | Nếu giữ màn hình đăng nhập |

Yêu cầu chung:
- **Phân trang** thống nhất: `page`, `pageSize`, trả `total`.
- **Ngày** trả ISO (`yyyy-MM-dd` cho hạn lệnh, ISO 8601 UTC cho thời điểm). Giao diện hiển thị dd/MM/yyyy.
- **Số container** trả không dấu cách; giao diện tự định dạng.
- `ValidateGate` **không thay đổi dữ liệu** và không phát sinh lỗi 4xx cho vi phạm nghiệp vụ; vi phạm nằm trong danh sách kiểm tra (`state: error`).

### 5a. Định nghĩa chỉ số

| Chỉ số | Định nghĩa |
|---|---|
| Container trong bãi | Số container `InYard` (gồm cả block ảo) |
| Nhập / Xuất hôm nay | Số giao dịch `Active` `In` / `Out` có `OccurredAt` trong ngày theo múi giờ của depot (UTC+7) |
| Xu hướng | Thay đổi % so với hôm qua (Tổng quan) hoặc so với kỳ liền trước cùng độ dài (Báo cáo) |
| Tỷ lệ lấp đầy | TEU đang chiếm / tổng sức chứa các block lưới. TEU: 20ft = 1, 40ft = 2 |
| Sức chứa block | `Bays × Rows × MaxTiers` (đơn vị TEU). Đã dùng = TEU đang chiếm trong block |
| Lệnh sắp hết hạn | Lệnh chưa giao đủ, còn từ 0 đến 3 ngày tới hết `ExpiryDate` |
| Container giữ chỗ cho lệnh sắp hết hạn | Số Booking `Reserved` thuộc các lệnh trên |
| Thời gian lưu bãi trung bình | Trung bình `ShippedAt - GateInAt` của container xuất trong kỳ báo cáo, tính theo ngày |
| Đang lưu bãi (theo hãng) | Số container `InYard` của hãng |

### 5b. Ma trận Giao diện → API

Mỗi thao tác trên giao diện phải có endpoint tương ứng. Nếu thiếu mục nào ở đây thì giao diện không chạy được.

| Màn hình | Thao tác / dữ liệu | Endpoint | Quy tắc / lỗi |
|---|---|---|---|
| Tổng quan | 4 ô số, cảnh báo lệnh sắp hết hạn | `GET /dashboard/summary` | |
| | Biểu đồ 7 ngày | `GET /dashboard/gate-traffic` | |
| | Công suất bãi | `GET /dashboard/block-capacity` | |
| | Hoạt động gần đây, bấm mở chi tiết | `GET /gate/transactions`, `GET /containers/{number}` | |
| Container | Danh sách, tìm, lọc loại, lọc gán lệnh | `GET /containers` | |
| | Số theo loại ở thẻ lọc | `GET /containers/summary` | |
| | Tạo mới | `POST /containers` | C1 đến C4 |
| | Chi tiết, tab Thông tin | `GET /containers/{number}` | |
| | Tab Lệnh giao (hiện tại, tóm tắt, lịch sử) | `GET /containers/{number}/orders` | |
| | Danh sách lệnh để gán | `GET /containers/{number}/eligible-orders` | D2 |
| | Gán vào lệnh | `POST /containers/{number}/bookings` | D2, D3 |
| Lệnh giao | Danh sách, ô tóm tắt, lọc sắp hết hạn / còn hiệu lực / theo khách | `GET /delivery-orders` | |
| | Thêm lệnh | `POST /delivery-orders` | |
| | Chi tiết lệnh | `GET /delivery-orders/{number}` | |
| | Danh sách container để gán | `GET /delivery-orders/{number}/eligible-containers` | D2 |
| | Gán nhiều container | `POST /delivery-orders/{number}/bookings` | D2, D3 |
| | Gỡ container | `DELETE /bookings/{id}` | D4 |
| Cổng | Kiểm tra nghiệp vụ | `POST /gate/validate` | G1 đến G4, Y1 đến Y5 |
| | Nhập bãi | `POST /gate/in` | G1, G7 |
| | Xuất bãi | `POST /gate/out` | G2 đến G6 |
| | Gợi ý vị trí | `GET /gate/suggest-position` | |
| | Lịch sử cổng | `GET /gate/transactions` | |
| | Sửa bản ghi, xem lịch sử thay đổi | `PATCH /gate/transactions/{id}`, `GET /gate/transactions/{id}` | G8 |
| | Hủy giao dịch | `POST /gate/transactions/{id}/cancel` | G9 |
| Sơ đồ bãi | Tổng quan block | `GET /yard/blocks` | |
| | Lưới theo tier | `GET /yard/blocks/{code}/tiers/{tier}` | |
| | Chi tiết ô | `GET /yard/blocks/{code}/containers/{number}` | |
| | Tìm container | `GET /yard/search` | |
| | Mở rộng / thu hẹp | `PUT /yard/blocks/{code}` | Y6 |
| | Block ảo | `GET /yard/virtual-blocks/{code}/containers` | |
| Khách hàng, Hãng khai thác | Danh sách (kèm số DO, container lưu bãi) | `GET /customers`, `GET /operators` | |
| | Thêm, sửa, ngưng hoạt động | `POST`/`PUT`/`POST …/deactivate` | MST và mã không trùng |
| Báo cáo | Xem, xuất file | `GET /reports/gate-output`, `GET …/export` | |
| Đăng nhập | Đăng nhập, người dùng hiện tại | `POST /auth/login`, `GET /auth/me` | |
| Mọi form | Danh sách giá trị cố định | `GET /lookups` | |

Yêu cầu bổ sung để giao diện nhất quán:
- Mỗi lệnh ghi (`POST`, `PUT`, `PATCH`, `DELETE`) trả về **bản ghi sau thay đổi**, để giao diện cập nhật ngay không phải đoán.
- Màn hình Tổng quan ghi "Cập nhật theo thời gian thực" ở khung Hoạt động gần đây: mặc định giao diện **gọi lại sau mỗi 30 giây** (polling). Đẩy sự kiện (SignalR) là phương án sau, chỉ làm nếu khách yêu cầu.
- Danh sách trả `total` và `page`; các endpoint có bộ lọc dùng cùng tên tham số ở mọi nơi (`search`, `status`, `page`, `pageSize`).
- Giá trị cố định (tình trạng, loại container...) luôn là danh sách từ `GET /lookups` hoặc enum tài liệu hóa trong OpenAPI; giao diện không tự thêm giá trị.
- `GET /yard/blocks/{code}/tiers/{tier}` trả sẵn từng vị trí kèm bay, hàng, kích thước, và tóm tắt Booking để giao diện tô màu "Theo đặt chỗ" mà không gọi thêm.

### 5c. Quy ước tên hiển thị giao diện ↔ API

Bản xem trước dùng một số tên trường khác backend. Khi nối API, ánh xạ như sau (backend giữ tên bên phải).

| Giao diện (bản xem trước) | API |
|---|---|
| Container `status` (Trong bãi, Chờ nhập, Chờ xuất, Đang cấp điện, Đang kiểm tra, Chờ phân vị trí, Đã xuất) | `displayStatus` (xem bảng ở mục 2.2) |
| Container `owner` | `ownerCode` |
| Container `manufactured` (MM/YYYY) | `manufactureMonth` (`yyyy-MM`) |
| Container `date` (Ngày nhập) | `gateInAt` |
| Container `location` (`A-04-B-1`, `VB-01`, `Ngoài bãi`) | `position` hoặc `virtualBlock`, giao diện tự định dạng chuỗi |
| Giao dịch cổng `operator` (tên nhân viên) | `createdBy.displayName` (không phải hãng khai thác) |
| Giao dịch cổng `direction` (Nhập bãi / Xuất bãi) | `direction` (`In` / `Out`) |
| Giao dịch cổng `status` (Hoàn tất / Đã hủy) | `status` (`Active` / `Cancelled`) |
| Giao dịch cổng `vehicle`, `vehicleType` | `vehicleNo`, `vehicleType` (`Truck` / `Barge`) |
| Giao dịch cổng `location` | `position` khi nhập; vị trí cũ (`previousPosition`) khi xuất |
| Giao dịch cổng `order` ("—" nếu không có) | `orderNumber` (null nếu không có) |
| Lệnh giao `customer` ("MST – tên") | `customer.taxCode` và `customer.name` |
| Lệnh giao `status` | tính ra (`Còn hạn`, `Sắp hết hạn`, `Hết hạn`, `Đã giao đủ`), không lưu |
| Hãng khai thác `status` (Hoạt động) | `isActive` |
| Hãng khai thác `containers` | `containersInYard` |
| Hãng khai thác `contact` | `contact` (email) |

Giao diện nên có lớp ánh xạ riêng (mapper) giữa DTO của API và model hiển thị, để đổi tên không phải sửa các màn hình.

## 6. Lỗi

ProblemDetails kèm `code` (mã quy tắc) và `errors[]` nếu có nhiều lỗi.

| HTTP | Khi nào | Ví dụ `code` |
|---|---|---|
| 400 | Sai định dạng dữ liệu | `C1`, `C3`, `C4` |
| 404 | Không tìm thấy container, lệnh, giao dịch, block | |
| 409 | Xung đột nghiệp vụ hoặc xung đột đồng thời | `D3` (đã giữ chỗ), `G4` (container giữ chỗ cho lệnh khác), `Y3`, `Y5`, `Y6`, `G9`, RowVersion không khớp |
| 422 | Vi phạm quy tắc nghiệp vụ khác | `D2`, `G3`, `Y1`, `Y2`, `Y4` |

Thông điệp tiếng Việt, nêu rõ vi phạm cụ thể (ví dụ: "Container đã giữ chỗ cho lệnh DO-2610-001").

## 7. Infrastructure

- **Giao dịch**: `RecordGateOut`, `RecordGateIn`, `CancelGateTransaction`, `AssignContainersToOrder` mỗi lệnh là một giao dịch DB (container, vị trí, Booking, GateTransaction cùng thành công hoặc cùng thất bại).
- **Chỉ mục duy nhất**:
  - `Container.Number`
  - `(Booking.ContainerId) WHERE Status = Reserved`
  - Vị trí: không trùng (block, row, tier, slot đầu). Với 40ft cần kiểm tra thêm slot thứ hai ở tầng ứng dụng trong cùng giao dịch.
  - `DeliveryOrder.Number`, `Customer.TaxCode`, `Operator.Code`
- **Chống ghi đè đồng thời**: `RowVersion` trên Container, Booking, GateTransaction, DeliveryOrder.
- **Truy vấn bãi** theo (block, tier): chỉ mục trên (BlockId, Tier, Row).
- **Lịch sử**: `GateTransactionChange`; thêm cột `CreatedBy`/`UpdatedBy`, thời điểm tạo/sửa trên các bảng chính.
- **Tác vụ nền** (D5): đổi Booking `Reserved` của lệnh hết hạn sang `Expired` hằng ngày.
- **Dữ liệu mẫu (seed)**: giữ tính nhất quán như thiết kế: mỗi container tối đa một Booking `Reserved`; số `Shipped` của dòng lệnh bằng số Booking `Shipped` tương ứng; container gán cho lệnh cùng hãng, đúng loại và cỡ; số container mẫu có số kiểm tra hợp lệ; có container qua nhiều lệnh của nhiều khách, có container chưa từng gán lệnh, có container đã xuất nhiều lần.

## 8. Ảnh hưởng đến kế hoạch 4 tuần

| Tuần | Bổ sung cần làm |
|---|---|
| Tuần 1 Domain | Entity Operator, Customer, YardBlock, Container (value object Position, số container + số kiểm tra, trạng thái và `DisplayStatus`), DeliveryOrder + Line (trạng thái tính ra), **Booking**, GateTransaction + Change. Hàm quy tắc C, Y, D, G kèm unit test |
| Tuần 2 Application | Các use case ở mục 4, validator, DTO, `ValidateGate` dùng chung quy tắc, danh sách "eligible" |
| Tuần 3 Infrastructure | Cấu hình EF, chỉ mục duy nhất, RowVersion, giao dịch, truy vấn sơ đồ bãi, migration, seed, tác vụ nền hết hạn |
| Tuần 4 Api | Endpoint mục 5 và ma trận 5b, ProblemDetails mục 6, Swagger/OpenAPI, integration test cho các mã 400/404/409/422 |

Nếu thiếu thời gian, thứ tự ưu tiên: (1) Container + Booking + Lệnh giao, (2) Cổng nhập/xuất + `ValidateGate`, (3) Bãi (kèm tìm container), (4) Sửa/hủy giao dịch, (5) Đối tác (khách hàng, hãng khai thác), (6) Tổng quan, (7) `SuggestPosition`, (8) tác vụ nền hết hạn. **Mở rộng, làm sau:** Báo cáo và xuất file, Đăng nhập (`/auth`).

## 9. Kịch bản nghiệm thu chính

1. Tạo container với số sai số kiểm tra → 400 `C1`.
2. Gán container Maersk Dry 20ft vào lệnh của hãng khác → 422 `D2`.
3. Gán cùng một container cho hai lệnh → lần hai 409 `D3`.
4. Gán vượt số lượng dòng lệnh → 422 `D2`.
5. Nhập container 20ft vào bay chẵn → 422 `Y1`; đặt tier 2 khi tier 1 trống → 422 `Y4`.
6. Xuất container đã giữ chỗ cho DO-A bằng DO-B → 409 `G4`.
7. Xuất container đang bị container khác đè → 409 `Y5`.
8. Xuất thành công → Booking `Shipped`, container `Exited`, số đã giao của dòng tăng 1, vị trí được giải phóng.
9. Hủy giao dịch xuất → Booking về `Reserved`, container về đúng vị trí cũ; nếu vị trí bị chiếm → 409 `G9`.
10. Lệnh qua hạn → Booking `Reserved` hiển thị `Expired`, container có thể gán lệnh khác.
11. Thu hẹp block khi còn container ngoài vùng mới → 409 `Y6`.
12. `GET /delivery-orders?status=expiring` chỉ trả lệnh còn 3 ngày trở xuống và chưa giao đủ.
13. Container có Booking `Reserved` có `displayStatus` là "Chờ xuất"; container ở block ảo là "Chờ phân vị trí"; sau khi xuất là "Đã xuất".
14. `POST /gate/validate` trả kèm `container` (hãng khai thác, loại, ISO, cỡ, vị trí hiện tại) và `order` khi đã nhập đủ số container và số lệnh.

## 10. Câu hỏi cần xác nhận với khách

- Số kiểm tra: dư 10 tính là 0 (ISO 6346) hay "X" như tài liệu?
- Lệnh hết hạn có tự giải phóng container đang giữ chỗ không?
- Container chưa giữ chỗ có được xuất thẳng bằng một lệnh còn hạn không (G5)?
- 40ft có được đặt ở mọi bay chẵn không, hay theo cặp bay cố định?
- Container hư hỏng có được xuất không, và ai quyết định?
- Hạn lệnh tính hết ngày hay đến giờ cụ thể, theo múi giờ nào?
- Số tier tối đa và có tách khu 20ft/40ft/reefer riêng không?
- Có cần phân quyền (ai được sửa, hủy giao dịch)?
- Hai trạng thái "Đang cấp điện" và "Đang kiểm tra" có thực sự cần quản lý không? Nếu có, ai bật/tắt và ở màn hình nào? Nếu không, bỏ khỏi giao diện và bỏ hai cờ `ReeferPowered`, `UnderInspection`.
- "Chờ xuất" có đúng nghĩa là container đã có đặt chỗ (Booking `Reserved`) không?
