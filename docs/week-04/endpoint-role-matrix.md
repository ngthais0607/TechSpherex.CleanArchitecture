# Ma trận endpoint tuần 4

Mọi quyền gắn với tenant trong JWT. `X-Tenant-Id` không chọn depot; khác tenant JWT trả 403. Danh mục hãng tàu, khách hàng, chuyến tàu và xe dùng chung toàn hệ thống.

| Nhóm | Admin | YardPlanner | OrderClerk | GateClerk |
|---|---|---|---|---|
| Đọc danh mục, đọc container | Có | Có | Có | Có |
| Tạo danh mục, đăng ký user, readiness | Có | Không | Không | Không |
| Đăng ký container, Gate-In, Gate-Out | Có | Không | Không | Có |
| Tạo/mở rộng/thu hẹp block, Place/Remove/Relocate, đánh dấu hỏng | Có | Có | Không | Không |
| Layout, tìm vị trí trống | Có | Có | Không | Có |
| Tạo lệnh giao hàng | Có | Không | Có | Không |
| Đọc lệnh giao hàng | Có | Không | Có | Có |
| Tồn bãi, lưu bãi, lịch sử (ngày 3) | Có | Có | Có | Có |
| Lệnh sắp hết hạn (ngày 3) | Có | Không | Có | Có |

Login, refresh và liveness cho phép anonymous. Các route nghiệp vụ yêu cầu token; thiếu token trả 401, thiếu quyền trả 403 dạng ProblemDetails. GET danh sách dùng `pageIndex` từ 1 và `pageSize` 1–100 (mặc định 20). Layout trả snapshot một block.

Các endpoint nghiệp vụ:

- `GET/POST /api/containers`, `GET /api/containers/by-number/{number}`, `POST /api/containers/{id}/damage`.
- `POST /api/yard/blocks`, `POST /api/yard/blocks/{id}/expand`, `POST /api/yard/blocks/{id}/shrink`.
- `GET /api/yard/blocks/{id}/layout`, `GET /api/yard/free-slots?blockId={id}&size=20`.
- `POST /api/yard/placements`, `/api/yard/removals`, `/api/yard/relocations`.
- `GET/POST /api/delivery-orders`, `GET /api/delivery-orders/{id}`, `GET /api/delivery-orders/by-number/{number}`.
- `POST /api/gate/in`, `POST /api/gate/out`.

Test `AuthorizationMatrixTests` gửi request hợp lệ bằng JWT thực của từng vai trò; `MasterDataTests` kiểm tra đọc/ghi danh mục. Hai kiểm thử concurrency dùng PostgreSQL thật và hai request HTTP riêng, đồng bộ ở trước SaveChanges để cả hai cùng đọc phiên bản cũ.
