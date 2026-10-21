# API tích hợp FE–BE

Base path `/api`. Minimal API gọi handler qua interface Application; tenant nghiệp vụ lấy từ JWT của tài khoản, không nhận depot do client chọn. Tham khảo collection đã chạy thử trong `docs/week-04/postman` và [hướng dẫn Postman](week-04/POSTMAN-GUIDE.md).

## Auth và phân quyền

POST `/api/auth/login` anonymous:

```json
{ "userName": "<ADMIN_HOAC_USER_CUA_BAN>", "password": "<MAT_KHAU_CUC_BO>" }
```

Response 200 gồm `accessToken`, `refreshToken`, `userName`, `role`, `expiresAt`, `accessTokenExpiresAt`. Gửi `Authorization: Bearer <accessToken>`. POST `/api/auth/refresh` anonymous nhận `userName` và `refreshToken`, xoay token; token refresh cũ bị từ chối. Login không phân biệt user lạ/sai password trong lỗi public.

POST `/api/auth/register` chỉ Admin, body có UserName, Email, FirstName, LastName, Password, Role. Role là Admin/YardPlanner/OrderClerk/GateClerk; tenant được gán từ Admin đang gọi. Không gửi TenantId để chuyển depot. UserName/email duy nhất toàn hệ thống. Tài khoản một depot; `X-Tenant-Id` nếu có phải khớp JWT.

| Quyền | Admin | YardPlanner | OrderClerk | GateClerk |
|---|---|---|---|---|
| Đọc danh mục/container | Có | Có | Có | Có |
| Ghi danh mục, tạo user, readiness | Có | — | — | — |
| Đăng ký container, Gate-In/Out | Có | — | — | Có |
| Ghi bãi/damage | Có | Có | — | — |
| Đọc layout/free slots | Có | Có | — | Có |
| Ghi DO | Có | — | Có | — |
| Đọc DO/expiry report | Có | — | Có | Có |
| Inventory/dwell/history | Có | Có | Có | Có |

Hãng tàu/chuyến tàu/xe dùng chung. Khách hàng và ánh xạ owner-code thuộc tenant; container, block, DO, lifecycle và movement cô lập theo depot.

## Endpoint và response thành công

| Method / route | Thành công |
|---|---|
| POST `/api/auth/login`, `/api/auth/refresh` | 200 token DTO |
| POST `/api/auth/register` | 201, `userId` |
| GET/POST `/api/line-operators`, `/api/customers`, `/api/voyages`, `/api/vehicles` | GET 200 paged; POST 201 `id` |
| GET `/api/containers` | 200 paged; filters `search`, `ownerCode`, `size`, `condition`, `type`, `lineOperator`, `displayStatus`, `booking`; sort `number|size`, `descending` |
| POST `/api/containers` | 201 `id`, Location dẫn tới by-number |
| GET `/api/containers/by-number/{number}` | 200 container DTO |
| POST `/api/containers/{id}/damage` | 204; body `condition` |
| POST `/api/yard/blocks` | 201 `id` |
| POST `/api/yard/blocks/{id}/expand`, `/shrink` | 204; body `newBays`, `newRows`, `newTiers` |
| GET `/api/yard/blocks/{id}/layout` | 200 snapshot block và slots |
| GET `/api/yard/free-slots?blockId={id}&size=20` | 200 paged vị trí logic |
| POST `/api/yard/placements`, `/removals`, `/relocations` | 204 |
| POST `/api/delivery-orders` | 201 `id` |
| GET `/api/delivery-orders` | 200 paged; `search`, `status`, `lineOperator`, `customerTaxCode`, `isCompleted`; sort `createdAt|orderNumber|expiryDate`, `descending` |
| GET `/api/delivery-orders/{id}`, `/by-number/{number}` | 200 DO cùng items/quota |
| POST `/api/gate/in` | 201 `id` lifecycle |
| POST `/api/gate/out` | 204 |
| GET `/api/reports/yard-inventory` | 200 groups paged + totalContainers |
| GET `/api/reports/dwell-times` | 200 paged |
| GET `/api/reports/containers/{number}/history` | 200 paged timeline |
| GET `/api/reports/expiring-orders` | 200 paged |
| GET `/health/live` | Anonymous 200 process; ngoài `/api` |
| GET `/health/ready` | Admin, 200 Healthy/Degraded hoặc 503 Unhealthy |

Endpoint path id là nguồn id chính cho expand/shrink/damage; không thêm id body để ghi đè path. OpenAPI/Scalar chỉ Development. API không có endpoint đọc user theo id; không dựa vào URL user không được liệt kê ở bảng này.

## Body nghiệp vụ

Đăng ký container: `containerNumber`, `isoCode`, `size`, `type`, `tareWeight`, `maxGrossWeight`, `dateOfManufacture` (nullable UTC), `grade`, `condition`. OwnerCode suy ra từ số container. Number phải có ISO check digit đúng. Dữ liệu trong collection là fixture, không phải hồ sơ container thật.

DO không chỉ có order number và quota:

```json
{
  "orderNumber": "<SO_LENH_MOI>",
  "lineOperator": "<HANG_TAU>",
  "customerTaxCode": "<MA_SO_THUE>",
  "customerName": "<TEN_KHACH_HANG>",
  "outboundVessel": "<TEN_TAU>",
  "expiryDate": "2026-10-23T12:00:00Z",
  "items": [{ "size": "Ft20", "type": "Dry", "quantity": 3 }]
}
```

Đổi expiryDate cho phù hợp ngày chạy thật. Gửi enum code theo GET /api/lookups: size Ft20/Ft40, type Dry/Reefer…, condition Normal/Damaged/Dented/Bent/Warped, grade A/B/C, vehicleType Truck/Train/Barge/Other. Không gửi nhãn tiếng Việt thay code.

Gate-In nhận `containerId`, `inVehicleNumber`, `slotId`, tùy chọn `classification`, `condition`, `vehicleType`. Gate-In **chiếm vị trí ngay**. Không gọi Place ngay sau Gate-In; chỉ Place sau Remove hoặc container active chưa có vị trí. Gate-Out nhận `containerId`, `deliveryOrderId`, `outVehicleNumber`, tùy chọn `vehicleType`.

Place body: containerId, blockId, bay, row, tier. Remove: blockId, bay, row, tier. Relocate: blockId, sourceBay/sourceRow/sourceTier, targetBay/targetRow/targetTier. Bay vật lý là số lẻ cho slot 20 feet; 40 feet dùng bay logic chẵn, chiếm hai slot lẻ kề nhau. Gate-In 40 feet nhận id của slot lẻ thấp hơn; history ghi bay chẵn. Không thể thao tác một bay chẵn bằng hai container 20 feet khác nhau.

Không Remove/Gate-Out container đang đỡ container khác. Gate-Out kiểm tra owner, ngày hiệu lực, quota và vị trí trước khi commit. Thất bại không giảm quota hoặc ghi movement. Hai request cạnh tranh thật có thể một 204/một 409; reload trạng thái trước khi retry, không retry mù khi request trước có thể đã thành công.

## Paging và báo cáo

Danh sách dùng `pageIndex` từ 1, `pageSize` mặc định 20, tối đa 100. Ngoài miền/binding sai trả 400. Response paged gồm items, pageIndex, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage. Layout là snapshot một block, không phân trang các slot.

- Inventory: lineOperator, size, type, condition, blockId. Container active nhưng chưa có vị trí thuộc Unplaced (`blockId=null`); 40 feet đếm một lần. totalContainers là tổng sau filter, trước page nhóm.
- Dwell: lineOperator, minDays không âm, includeGatedOut mặc định false; mỗi lượt tái nhập riêng. Lượt đã xuất dùng OutDate.
- History: thứ tự tăng dần theo UTC, source, id, event kind; EventId ổn định. Lượt mới version 1 chỉ movement. Lượt legacy version 0 có mốc vào/ra thực và movement phát sinh sau rollout; snapshot/actor không có dữ liệu trả null, không suy đoán lịch sử cũ.
- Expiry: days 0–365 (mặc định 7), lineOperator; chỉ lệnh còn quota, trong khoảng từ hôm nay đến ngày cuối bao gồm cả hai ngày.

Timestamp lưu/trả UTC. Ngày nghiệp vụ Asia/Ho_Chi_Minh (UTC+7); cùng ngày lưu bãi 0. DO hết hạn hôm qua bị loại sau nửa đêm địa phương dù UTC còn ngày trước. Không thay đổi đồng hồ Windows để kiểm thử; dùng các test TimeProvider cố định.

## Hợp đồng lỗi

ProblemDetails: type, title, status, code, traceId; validation body có errors theo field. 400 input/binding, 401 credentials/token, 403 quyền/tenant, 404 resource, 409 conflict/concurrency/unique, 422 business rule. Mã không được map trả 500 chung và log nội bộ. Không có SQL, stack trace hoặc thông tin credential trong response. Health readiness dùng JSON trạng thái dependency riêng, không trả chi tiết exception.

Request body tối đa 1 MiB tại Kestrel. CORS chỉ origin được cấu hình; request máy chủ/CLI vẫn phải qua JWT/role/tenant guard. TLS/reverse proxy và backup/restore xem [RUNBOOK](RUNBOOK.md).

## Booking và hợp đồng hiện tại

- GET /api/delivery-orders/{orderNumber}/bookings?pageIndex=1&pageSize=8 trả bookings có ID thật và itemCounts: reservedQuantity, shippedQuantity, availableQuantity.
- POST /api/delivery-orders/{orderNumber}/assign-containers nhận containerNumbers; response có assignedCount/requestedCount và failedContainerNumbers. HTTP200 có thể là thành công một phần.
- DELETE /api/delivery-orders/bookings/{id} giải phóng giữ chỗ. Lần lặp lại trả409 Booking.NotReserved.
- Gate-In hỗ trợ containerNumber + vehicleNumber + virtualBlockCode hoặc blockCode/bay/row/tier. Gate-Out hỗ trợ containerNumber + orderNumber + vehicleNumber. GET /api/gate/transactions có Changes; PUT sửa và POST /transactions/{id}/cancel hủy.
- GET /api/auth/me và POST /api/auth/logout dành cho phiên đã đăng nhập. expiresAt là hạn refresh token; accessTokenExpiresAt mới là hạn access token. Giới hạn login trả ProblemDetails429 code RateLimit.TooManyRequests.
- Alias /api/master-data/customers, /api/master-data/line-operators và /assign được giữ nhưng deprecated trong OpenAPI. Frontend dùng route canonical.
- Chạy/config/seed/kiểm thử mới nhất: [README](../README.md), [ma trận quyền](week-04/endpoint-role-matrix.md).
