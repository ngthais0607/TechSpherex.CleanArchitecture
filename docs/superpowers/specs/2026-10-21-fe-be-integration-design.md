# Thiết kế sửa kết nối Angular – .NET và dữ liệu mẫu

Ngày: 21/10/2026.
Trạng thái: dự thảo chờ người dùng duyệt bản viết; chưa cho phép triển khai.

## 1. Mục tiêu và cơ sở quyết định

Chuyển ứng dụng Angular hiện có từ snapshot demo sang dữ liệu API .NET, sửa các lỗi hạ tầng và seed cản trở kết nối, đồng thời tối ưu đúng các luồng bị ảnh hưởng. Giữ giao diện navy/cyan và các màn vận hành hiện tại.

Người dùng yêu cầu lập kế hoạch dựa trên `review-ket-noi-FE-BE-va-seed-data.md` và trả lời “ok” sau đề xuất hướng A: nối theo từng luồng, giữ bản demo riêng. Bản này cụ thể hóa hướng A để duyệt trước khi viết kế hoạch triển khai. Các lựa chọn kỹ thuật bên dưới là đề xuất của thiết kế, chưa phải những yêu cầu riêng do người dùng xác nhận từng mục.

Nguồn: báo cáo tại `C:/Users/Thinkpad/Downloads/review-ket-noi-FE-BE-va-seed-data.md`, code repository ở commit nền `794fa35`, đặc tả demo ngày 20/10/2026 và các endpoint/DTO/policy hiện có. Báo cáo là tài liệu tham khảo cần kiểm chứng, không phải lệnh thực thi. Bản thiết kế này ưu tiên hơn các quyết định chỉ dành cho demo trong đặc tả cũ khi chạy chế độ API.

Thành công khi thay đổi thực hiện từ FE được BE lưu và phản ánh sau tải lại; quyền và lỗi nghiệp vụ đến từ BE; dữ liệu mẫu nhất quán; dashboard, danh sách và lịch sử đối chiếu được với nhau. Chưa triển khai hoặc chạy build/test trong giai đoạn thiết kế; số 503 test trong README là kết quả lịch sử.

## 2. Phạm vi và phân rã

Đây là một đợt tích hợp có các mốc phụ thuộc, không viết lại hệ thống. Giữ Clean Architecture, Angular standalone, Signals, Reactive Forms, lazy routes và thư viện xuất Excel hiện có.

Bao gồm: compose dev/migration/config; seed an toàn và có lịch sử; hợp đồng API phục vụ UI hiện tại; transport/auth/error của FE; chuyển từng feature sang API; quyền theo policy; kiểm thử qua API thật và tài liệu chạy máy mới.

Không bao gồm: quản lý tài khoản trên UI, cơ chế offline đồng bộ dữ liệu máy chủ, thiết kế lại giao diện, đổi framework, cookie auth mới, triển khai production, xóa DB hiện có hoặc sửa kiến trúc đa tenant toàn hệ thống. Không thêm thông tin liên hệ khách hàng/hãng tàu vào schema trong đợt này.

## 3. Kiến trúc FE và bản demo riêng

Luồng đọc: page → facade theo feature → API service → DTO → mapper thuần → view model và Signals. Luồng ghi: form → facade → command API → cập nhật dữ liệu liên quan sau thành công. Component không sửa entity máy chủ trong snapshot hoặc gọi rule demo.

Ranh giới dự kiến:

- `frontend/src/app/core/api/`: config URL, DTO, phân trang, ProblemDetails và client theo nhóm auth/lookups/partners/containers/orders/gate/yard/reports.
- `frontend/src/app/core/auth/`: AuthService, token storage, interceptor xác thực, route guard và permission helpers.
- Mapper và facade đặc thù ở feature tương ứng; không tạo một repository toàn cục chứa toàn bộ DB.
- `core/models/` chỉ giữ model dùng chung thật sự; model snapshot cũ tiếp tục thuộc đường chạy demo trong quá trình chuyển đổi.
- `shared/` tiếp tục cung cấp form, lỗi, toast, bảng và trạng thái loading/error/empty.

Trước lần đổi feature sang API đầu tiên, giữ bản demo hiện tại bằng một Git tag có tên và commit rõ ràng, ghi hướng dẫn checkout/chạy trong README. Bản chính chuyển dần sang API. Không thêm hai implementation runtime cho mọi service hoặc tự fallback sang localStorage khi API lỗi. Trong giai đoạn chuyển đổi, các màn chưa chuyển vẫn được ghi nhận là demo và chưa được tính là đã tích hợp; chỉ nghiệm thu các chuỗi đã chuyển hoàn chỉnh.

Bản API hoàn chỉnh không có menu reset/demo, mật khẩu mẫu gõ cứng hoặc import runtime từ `demo/rules`, `demo/commands`, `demo/queries`, `DemoRepository`, `DemoSession`. Chuyển helper định dạng/normalize thuần cần dùng sang nơi dùng chung, giữ hành vi qua test sẵn có. Test của bản demo không được coi là bằng chứng API hoạt động.

## 4. Hạ tầng, cấu hình và migration

- Compose dev dùng lại target `migrate` và chuỗi postgres healthy → migrate thành công → API. Migration chạy efbundle, không xóa dữ liệu. Mode `fresh` hiện trong script chỉ chạy bundle; nâng cấp DB cũ cần ánh xạ tenant vẫn theo runbook `upgrade` hiện có.
- Truyền thống nhất DB name/user/password, JWT, admin tenant/username/password, cờ demo, mật khẩu tài khoản demo, CORS và `ASPNETCORE_ENVIRONMENT=Development`. `.env.example` chỉ chứa tên biến/hướng dẫn và giá trị không nhạy cảm; thiếu secret phải báo cấu hình rõ ràng.
- Chuẩn dev mặc định: FE `http://localhost:4200`, Docker API `http://localhost:5000`. API chạy host bằng profile http ở `http://localhost:5187` là lựa chọn thay thế có tài liệu; không trộn URL của hai cách chạy.
- FE gọi `/api` tương đối qua Angular proxy trong dev; proxy mặc định trỏ Docker API và có cấu hình thay thế cho host. Production dùng `/api` cùng origin sau reverse proxy; không đóng gói localhost vào production build.
- Cấu hình rõ khi bật HTTPS redirection; profile http phải hoạt động mà không redirect sang HTTPS. Khi dùng HTTPS phải gọi trực tiếp origin HTTPS hợp lệ.
- Liveness chỉ chứng minh tiến trình sống. Smoke test phải kiểm tra thêm login và truy vấn DB qua API; readiness có authorization được gọi bằng admin.
- Không đưa demo seed vào compose production. Không thay đổi yêu cầu Redis của production trong đợt này.

## 5. Seed nhất quán, có thể chạy lại

### 5.1 Cách thực hiện

Sửa seeder trong tiến trình cho đợt này. Seed qua API là hướng tiếp nối, không làm đồng thời hai nguồn dữ liệu mẫu. API vận hành từ chối tạo đơn hết hạn nên không thể dùng chuỗi HTTP thuần để dựng toàn bộ kịch bản lịch sử hiện tại.

Bootstrap depot/roles/admin giữ độc lập với demo. Demo chỉ chạy khi môi trường Development và `Seed:DemoData=true`; ở môi trường khác ghi log bỏ qua, không tạo user demo hoặc dữ liệu demo. Mật khẩu admin và user demo đọc config, không ghi log mật khẩu/token.

Thêm bản ghi hạ tầng `DemoSeedRun` với khóa duy nhất `(TenantId, DatasetVersion)` và thời điểm hoàn tất, kèm EF migration. Toàn bộ dữ liệu demo và bản ghi hoàn tất được ghi trong một transaction PostgreSQL. Dùng transaction advisory lock theo dataset/tenant trước khi kiểm tra bản ghi để tuần tự hóa khởi động trùng. Nếu thất bại, rollback và không có bản ghi hoàn tất. Seed version đầu tiên là `fe-be-v1`.

Đọc tenant trong seeder bằng `IgnoreQueryFilters()` và điều kiện tenant cụ thể. Không bỏ filter trong request nghiệp vụ. Dữ liệu master dùng chung như LineOperator được tìm/tái sử dụng theo code toàn cục, không chèn lại cho tenant khác; owner mapping vẫn theo tenant.

Dữ liệu có mã xác định từ manifest gồm container number hợp lệ, order number, block code và customer tax code. Nếu chưa có marker nhưng phát hiện các mã demo cũ/đang dở, dừng demo seed với thông báo xung đột và hướng dẫn dùng DB dev riêng. Không tự nhận là hoàn tất, xóa hoặc sửa dữ liệu người dùng. Khi có marker, khởi động lại bỏ qua dataset; không tự “sửa chữa” dữ liệu đã được người dùng vận hành. Phiên bản khác cũng phải kiểm tra xung đột mã, không tự nâng cấp dataset cũ.

Tạo `yard`, `order`, `gate` cùng tenant, role tương ứng bằng API domain hiện có. Tài khoản đã tồn tại phải đúng tenant/role, nếu khác thì báo xung đột; không âm thầm đổi mật khẩu hay quyền. Bootstrap admin vẫn hoạt động khi tắt demo.

### 5.2 Manifest dữ liệu mẫu

50 container, 5 đơn, 2 hãng, 3 khách hàng, block thật A/B và block ảo VB-01. Chỉ số dưới đây là khóa manifest để test, không dùng index ngầm để suy ra hãng/loại trong code.

| Nhóm | Hãng/loại và trạng thái | Booking/kịch bản |
| --- | --- | --- |
| 0–9, 13–14 | MSC, 20ft Dry, trong A | 0–2 Reserved cho DO-001; còn lại chưa giữ chỗ |
| 10–12 | MAEU, 20ft Dry, trong A | Reserved từ trước khi DO-004 hết hạn; test chặn xuất và gán lại |
| 15–19 | MSC, 20ft Reefer, trong VB-01 | 15–16 Reserved cho DO-003 sắp hết hạn |
| 20–22 | MSC, 20ft Dry, đã xuất | Shipped đúng 3 cho DO-005; đơn hoàn tất |
| 23–34 | 20ft Dry, chưa nhập | Hãng được khai báo rõ trong manifest; dùng test gate-in |
| 35–36 | MAEU, 40ft Dry, chưa nhập | Reserved cho DO-002; test nhập rồi xuất |
| 37–49 | MAEU, 40ft Dry, chưa nhập | Dữ liệu nền |

DO-001 có 5 suất, DO-002 có 4, DO-003 có 3, DO-004 có 4, DO-005 có 3. Ngày dựng theo TimeProvider: đơn sắp hết hạn +2 ngày, đơn hết hạn -5 ngày, lịch sử reservation của đơn hết hạn phải trước hạn. Thời điểm gate-in/place/gate-out, booking và transaction phải theo thứ tự; không dùng ngày hiện tại mặc định của factory để giả lịch sử quá khứ.

Seed phải có movement và gate-in cho mọi lifecycle, gate-out cho lifecycle đã xuất, vị trí physical/virtual tương ứng và booking Shipped đúng đơn. Chọn slot theo rule hỗ trợ tầng/footprint, không lấy 15 ô đầu theo thứ tự rồi mặc định hợp lệ. Dữ liệu hết hạn biểu diễn lịch sử hợp lệ, không nới rule command vận hành để tạo nó.

Số nền kỳ vọng trước thao tác: 50 đăng ký, 20 trong bãi (15 thật, 5 ảo), 3 đã xuất, 27 chưa nhập. Tổng gate-in 23, gate-out 3. Quota/remaining dùng cùng định nghĩa handler hiện có. Không có Reserved trên container đã xuất; mọi shipped khớp hãng/size/type; số shipped không vượt quota.

## 6. Hợp đồng API và mapping

Chuẩn routes: `/api/customers`, `/api/line-operators`, `/api/delivery-orders/{orderNumber}/assign-containers`. Giữ alias hiện tại để tương thích và ghi deprecated trong OpenAPI/tài liệu; FE mới không gọi alias. Cập nhật endpoint inventory, role matrix và Postman theo cùng thay đổi.

DTO TypeScript bám response thật, không ép DTO vào `DepotSnapshot`. Mapper xử lý rõ `Ft20/Ft40`, enum loại/tình trạng, `classification`, `containerNumber`, `blockCode`, row số và ID GUID. Nhãn tiếng Việt từ `/api/lookups`, không dùng nhãn làm giá trị gửi API. Số gửi dưới dạng number, ngày theo hợp đồng UTC/DateTime thực tế; hiển thị theo timezone depot từ `/auth/me` nếu có hoặc cấu hình depot được tài liệu hóa, không tự đổi chính sách hết hạn ở FE.

Phân trang bắt đầu 1, pageSize không quá 100, dùng totalCount thật. Customer dùng taxCode/name/isActive; Order dùng customerTaxCode và mã hãng theo DTO. Màu hãng tính xác định từ code ở FE. Bỏ các ô email/phone/address/contact không được BE hỗ trợ trong bản API; không giả lưu thành công. Tình trạng container không ánh xạ theo suy đoán từ chuỗi tiếng Việt cũ.

Các thiếu hụt hợp đồng cần giải quyết trước khi chuyển màn tương ứng:

- Container list hiện chỉ có search/ownerCode/size/condition. Bổ sung filter type, lineOperator, displayStatus, booking state và sort phục vụ bộ lọc đang có. Dùng mapping owner-code trên server cho filter hãng. Summary nhận cùng filter (loại trừ filter type khi đếm tab loại) và dùng chung biểu thức lọc để tổng khớp danh sách. Sort có danh sách cho phép và khóa phụ ổn định.
- Orders bổ sung search số đơn và sort cần cho màn hiện tại; dùng status, lineOperator, customerTaxCode đã có. Không tải toàn bộ orders rồi phân trang tại FE.
- `DeliveryOrderDto` trả `status`, container trả `displayStatus`; facade không tự chạy hết hạn hoặc suy ra trạng thái nghiệp vụ.
- Gate validate trả object có `checks`, `container`, `order`, không phải một mảng trực tiếp. Gate transaction đã có `changes`; nối lịch sử sửa theo DTO này.
- Reports inventory trả `groups` có phân trang và `totalContainers`; không coi group là từng container. Top lưu lâu dùng dwell-times, công suất dùng block-capacity, traffic dùng gate-traffic. Dashboard không lấy KPI từ danh sách một trang.
- Thống nhất tập báo cáo tích hợp gồm inventory hiện tại, dwell-times và traffic trong cửa sổ `days` API hỗ trợ. Bỏ bộ lọc ngày tùy ý/biểu đồ tổng hợp không có dữ liệu tương ứng trong bản API đầu tiên; ghi rõ giới hạn. Không bổ sung kho báo cáo lịch sử mới trong đợt này.
- Khu hoạt động gần đây trên dashboard chỉ dùng lịch sử có API và quyền cho phép; nếu chưa có nguồn chung, bỏ khối đó ở bản API, không tạo số liệu giả. Màn orders phải truy cập được quyền read cần cho thao tác của OrderClerk; không phụ thuộc yard endpoint vốn không cho role này đọc.

Xuất Excel dùng đúng bộ lọc dữ liệu server, lần lượt đọc các trang cần thiết, có trạng thái tiến trình/hủy/lỗi; không âm thầm xuất riêng trang đang xem hoặc giả là snapshot transaction khi có thay đổi đồng thời. Không thêm export backend riêng trong đợt này.

## 7. Auth, phân quyền và lỗi

AuthService gọi login, lưu token trong sessionStorage cho môi trường tích hợp, gọi `/auth/me` để lấy roles/tenant/depot. Chỉ render khu bảo vệ sau khi phục hồi phiên và me thành công; lỗi mạng phục hồi phiên có retry, không coi là sai mật khẩu. Không lưu password hoặc lấy user demo làm dự phòng.

Interceptor chỉ gắn bearer vào origin/path API đã cấu hình. Không gửi `X-Tenant-Id`. Dùng `accessTokenExpiresAt` để refresh sớm với khoảng đệm 30 giây. Nhiều request cùng hết hạn dùng chung một refresh đang chạy; cập nhật cả access/refresh token sau rotation rồi mới phát lại request chờ. Không refresh login/refresh, không lặp retry quá một lần. Request mới đang chờ refresh phải được hủy khi logout; kết quả refresh cũ không được phục hồi phiên đã logout.

401 từ tài nguyên được refresh một lần; refresh bị từ chối thì xóa phiên và về login với returnUrl nội bộ an toàn. Refresh lỗi mạng/5xx thì giữ thông tin phiên, báo lỗi và cho retry có chủ đích, không tự gửi lại command vô hạn. Logout gọi BE thu hồi token, xóa dữ liệu phiên/cache phía FE; nếu gọi BE thất bại vẫn kết thúc phiên local và thông báo không xác nhận được thu hồi.

Quyền lấy đúng từ `AuthorizationExtensions` và endpoint role matrix: Admin toàn quyền; YardPlanner quản lý bãi; OrderClerk quản lý lệnh; GateClerk cổng/đăng ký container. Quyền đọc khác quyền ghi. Partners đọc cho các role, ghi chỉ Admin. OrderClerk không tự gọi yard; YardPlanner không tự gọi expiring-orders. Guard chặn URL trực tiếp, menu/nút ẩn theo capability, 403 vẫn do BE quyết định và hiển thị đúng quyền thiếu.

Error normalizer giữ status/code/ruleCode/errors/traceId, lỗi trường đặt ở form, thông báo tiếng Việt có fallback và traceId để tra lỗi. 400 validation; 401 auth; 403 quyền; 404 không còn tồn tại; 409 xung đột cần tải lại; 422 nghiệp vụ; 429 giới hạn; lỗi mạng/5xx cho thử lại phù hợp. Không tự replay ghi dữ liệu khi timeout hoặc 5xx vì chưa biết server đã commit hay chưa.

Hiện rate limiter trả 429 nhưng chưa cấu hình OnRejected để bảo đảm `RateLimit.TooManyRequests`; bổ sung ProblemDetails chuẩn và Retry-After khi lấy được thời gian từ lease. Không giả thời gian đếm lùi nếu server không trả header.

## 8. Trạng thái bất đồng bộ và tối ưu có mục đích

Tách initial loading, refreshing, empty, error và pending mutation. Giữ form khi request lỗi; khóa nút gửi trong lúc pending, chỉ đóng dialog/toast thành công sau response hợp lệ. Khi filter đổi reset trang về 1, debounce tìm kiếm 300ms và hủy request đọc cũ; response đến muộn không ghi đè bộ lọc mới.

Cache lookups/reference data trong phiên với TTL tối đa 1 giờ; xóa cache khi đổi user/tenant/logout và invalidate sau thay đổi master data. Không giữ cache toàn bộ container/order snapshot. Sau mutation, facade invalidate các query liên quan: container/order/booking, gate history, yard layout và báo cáo; feature tải lại khi đang hiển thị hoặc lần mở tiếp theo.

Mọi quyết định gate/slot/quota đến từ BE. Gate validate phục vụ hướng dẫn trước thao tác; command phải kiểm tra lại rule và concurrency, vì kết quả validate không giữ chỗ. Khi 409/422 giữ input, hiện thông báo và tải lại dữ liệu liên quan. Không lạc quan thay đổi occupancy/booking trước khi BE xác nhận.

Không thêm state library, generic repository framework, index DB hoặc cache máy chủ mới nếu chưa có bằng chứng query cần tối ưu. Kiểm tra query mới chạy lọc/sort trước Skip/Take và không gây N+1.

## 9. Trình tự bàn giao và tiêu chí nghiệm thu

| Mốc | Đầu ra | Bằng chứng cần có khi triển khai |
| --- | --- | --- |
| 1. Nền BE | Compose/config/migrate/URL và contract inventory | DB dev trống lên được, login/query hoạt động |
| 2. Seed | Dataset version, roles, lịch sử, invariant | PostgreSQL: tenant null, chạy hai lần với context mới, rollback/seed dở và xung đột mã |
| 3. Nền FE | Config/proxy, auth, lỗi, DTO và permission | Login thật, refresh đồng thời, logout race, 403/429, API origin giới hạn |
| 4. Container/Orders | Lookup/master data đọc, container/detail, tạo/gán/gỡ lệnh | Dữ liệu qua reload, filter/tổng/sort đúng, quota và role đúng |
| 5. Gate/Yard | Validate/nhập/xuất/sửa/hủy; block ảo, place/move/resize | Chuỗi nhập → gán → đặt/di chuyển → xuất, hết hạn/gán lại, xung đột thao tác |
| 6. Partners/Reports | CRUD đúng quyền, dashboard/tồn bãi/report/export từ BE | Tổng/bộ lọc/export khớp nguồn, lịch sử có dữ liệu và không gọi alias |
| 7. Hoàn tất | Loại phụ thuộc demo khỏi runtime API, README và E2E | Build, test liên quan, E2E thật bốn role và smoke từ DB riêng |

Lookups và danh sách master data cần cho form được nối sớm ở mốc 4; CRUD Partners đầy đủ ở mốc 6. Seed cũ không được reset trong quá trình kiểm thử; toàn bộ smoke/E2E dùng DB hoặc compose project riêng.

Test có trọng tâm: mapper enum/row/pagination/null; facade hủy request và invalidation; auth single refresh/rotation/logout; policy; filter-summary; seed PostgreSQL; HTTP ProblemDetails; concurrency của gate/yard/order. Tái dùng acceptance tests backend đang có, bổ sung trường hợp thiếu. Không dùng InMemory để chứng minh unique index, rollback hoặc PostgreSQL lock.

Playwright có cấu hình API riêng và bootstrap xác định, không reuse nhầm server/demo. Kiểm tra chuỗi UI với network thật, không mock endpoint nghiệp vụ. Không chạy reset seed giữa các bước của cùng workflow. Test write được tuần tự hóa hoặc có dataset riêng để không phụ thuộc thứ tự ngoài ý muốn.

Nghiệm thu cuối: khởi động mới và khởi động lại thành công; 4 tài khoản đúng quyền; số nền seed khớp; history có gate-in/place/gate-out; token refresh đúng; các lệnh thành công tồn tại sau reload; không có request alias hoặc ghi snapshot local; build/test/E2E có kết quả mới và ghi rõ phần chưa chạy nếu môi trường thiếu.

## 10. Rà soát thiết kế và bước tiếp theo

Đã tự rà phạm vi, tính nhất quán seed/quota/tenant, tên endpoint/DTO, thiếu hụt filter/report, quyền đọc phụ thuộc và giới hạn của API hiện tại. Đây là đặc tả để lập một kế hoạch triển khai theo các mốc trên; chưa phải kế hoạch task/file/test chi tiết và chưa xác nhận bất kỳ lỗi runtime nào đã được sửa.

Người dùng duyệt bản viết này trước. Sau khi duyệt, dùng skill writing-plans để viết kế hoạch triển khai có đường dẫn file, test cần thêm/chạy, phụ thuộc và điểm nghiệm thu; trình người dùng chọn cách thực hiện trước khi sửa code sản phẩm.
