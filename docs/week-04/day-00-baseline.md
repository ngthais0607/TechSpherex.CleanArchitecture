# Tiền kiểm tuần 4

Ngày: 2026-10-16. SDK 10.0.401; branch hiện tại Thai-DMS.

- Build `.sln --no-restore -m:1`: thành công, 0 lỗi, 2 cảnh báo NU1903 từ dependency gián tiếp SSH.NET 2024.1.0 trong Testcontainers. Không ẩn cảnh báo hoặc tự nâng dependency ngoài phạm vi.
- Baseline test thật: Domain 135 pass; Application 142 pass; Architecture 7 pass; Infrastructure Integration 7 pass; Infrastructure Unit 23 pass/1 fail; không skipped.
- Failure hiện hữu: TokenServiceTests.GenerateAccessToken_ShouldCreateValidJwtWithClaims, IDX12401 vì Expires dùng TimeProvider còn NotBefore dùng giờ hệ thống. Đã báo người dùng trước khi sửa; nằm trong phạm vi Identity được duyệt. Sau sửa NotBefore/IssuedAt và middleware tenant: Infrastructure Unit 28/28 pass (gồm 4 tests mới).
- Đã thêm hai Infrastructure test projects còn thiếu vào `.slnx`. Dùng `.sln` làm solution chuẩn, giữ parity.
- Docker Desktop đã chạy; sandbox chặn named pipe. Các test Docker phải chạy với quyền ngoài sandbox đã được xét duyệt.
- Native worktree đã tạo nhưng sandbox từ chối ghi; dùng fallback thư mục hiện tại. Giữ nguyên docs/week-02/day-02.md và docs/testing-guide.md của người dùng.
- Chưa hoàn tất ngày 1. Không lấy số pass baseline làm kết quả mã sản phẩm cuối ngày.
