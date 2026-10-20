# SNP Depot — Angular demo

Demo tiếng Việt của hệ thống quản lý depot: Đăng nhập, Tổng quan, Container, Lệnh giao, Cổng nhập/xuất, Sơ đồ bãi, Khách hàng/Hãng khai thác, Báo cáo và Tồn bãi. Chưa có quản lý Người dùng.

## Chạy trên Windows

Yêu cầu Node 20.19+ thuộc nhánh 20 (hoặc phiên bản được Angular 21 hỗ trợ), npm và trình duyệt hiện đại.

```powershell
cd D:\SnpDepotManagement\frontend
npm.cmd ci
npm.cmd start
```

Mở `http://localhost:4200`. Tài khoản **demo**, mật khẩu **Demo@123**. Phiên được giữ trong tab; đăng xuất không xóa dữ liệu.

Nếu npm 11 báo lỗi nội bộ `Cannot read properties of null (reading 'edgesOut')`, dùng npm 10 tạm thời, không cần thay npm toàn máy:

```powershell
npx.cmd --yes npm@10 ci --legacy-peer-deps
```

## Dữ liệu mẫu và dữ liệu trống

- Dữ liệu nghiệp vụ dùng chung toàn frontend và được giữ ở localStorage key `snp-depot-demo:v1`, schema version 1.
- Bấm biểu tượng **Công cụ demo** trên topbar để **Khôi phục mẫu**, chuyển sang **Dữ liệu trống**, hoặc **Mô phỏng lỗi đọc**. Các thao tác xóa/khôi phục cần xác nhận.
- Dữ liệu trống xóa container, lệnh, booking và lịch sử; giữ cấu hình block cùng danh mục khách/hãng để có thể nhập thử. Seed mẫu tạo theo ngày khởi tạo/khôi phục, có số kiểm tra container hợp lệ.
- Lỗi đọc có nút Thử lại. Nếu localStorage không ghi được, thao tác vẫn giữ trong RAM và có cảnh báo mất dữ liệu sau reload.
- Nhiều tab không đồng bộ trực tiếp: reload để nhận snapshot lưu mới nhất. Tài khoản demo chỉ là mô phỏng điều hướng, không cung cấp xác thực hay phân quyền thật.

**Bản này chưa gọi API .NET.** Chạy seed backend chưa làm thay đổi dữ liệu của demo trong trình duyệt. Khi chuyển sang API, cần thay lớp truy cập dữ liệu, đối chiếu DTO và chạy seed backend; backend hiện tại không bị sửa bởi frontend này.

## Quy ước mô phỏng

- Múi giờ depot UTC+7; lệnh có hiệu lực đến hết ngày hạn. Booking của lệnh hết hạn được giải phóng.
- Sức chứa bãi thật theo TEU: 20ft = 1, 40ft = 2; block ảo tính số tồn nhưng không tính tỷ lệ lấp đầy.
- Demo cho xuất trực tiếp container chưa giữ chỗ khi thỏa điều kiện lệnh. Đây chưa phải xác nhận cấu hình vận hành thật.
- Di chuyển bằng hộp thoại, không có kéo thả/3D. Xếp tầng có kiểm tra sức đỡ, bị đè, trùng vị trí và giới hạn block.
- Thời gian lưu hiện tại tính theo lifecycle đang mở; báo cáo tính các lượt xuất trong kỳ. Mốc “từ 3 ngày” là 72 giờ. Hủy giao dịch có kiểm tra khả năng phục hồi và giữ audit.
- Excel là `.xlsx` thật, xuất toàn bộ tập lọc, không chỉ trang hiện tại. ExcelJS chỉ được tải khi xuất.

## Kiểm tra

```powershell
npm.cmd test
npx.cmd playwright install chromium
npm.cmd run e2e
npm.cmd run build
```

Playwright tự chạy server localhost; tài khoản và dữ liệu độc lập giữa các test. Kiểm tra trình duyệt gồm luồng container đầy đủ, phiên, empty/retry/reset và các viewport 1440×900, 1366×768, 1024×768, 768×1024. Ảnh tại `docs/screenshots/`; kết quả tại `docs/demo-verification.md`.

Build xuất ở `dist/snp-depot-demo/browser`. Khi host SPA, cấu hình fallback về `index.html` cho các route nội bộ.

## Cấu trúc

- `src/app/core`: model, phiên demo, xuất Excel.
- `src/app/demo`: snapshot, lưu trữ, seed, truy vấn và quy tắc/command theo nghiệp vụ.
- `src/app/features`: lazy pages và form/dialog từng màn.
- `src/app/shared`: icon, badge, modal có focus native, form Reactive Forms và phân trang.
- `src/styles.scss`, `src/styles/extras.scss`: cỡ chữ, spacing, form, bảng và khung responsive.

Đây là bản demo tương tác; các test frontend không thay thế kiểm thử API, transaction hay phân quyền của backend.
