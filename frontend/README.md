# Angular frontend

Angular 21 dùng API .NET của repository. Xem [README gốc](../README.md) để dựng DB/API, seed, cấu hình và chạy nghiệm thu.

```powershell
npm.cmd ci
npm.cmd start
# API host profile HTTP 5187:
npm.cmd run start:host
npm.cmd test
npm.cmd run build
npm.cmd run check:api-runtime
# Chuẩn bị stack riêng theo README trước:
npm.cmd run e2e:api
```

Frontend mở http://localhost:4200. Proxy mặc định /api → http://localhost:5000; production dùng cùng-origin /api, cần reverse proxy đến API. Thông tin đăng nhập lấy từ cấu hình backend, không có tài khoản demo gõ cứng.

Mã runtime ở src/app/core/api, core/auth và features. Session token dùng sessionStorage; dữ liệu nghiệp vụ luôn đọc/ghi BE. UI tách quyền đọc/ghi theo policy, refresh một request dùng chung, giữ form khi BE trả lỗi. Các màn đang dùng API thật; E2E riêng ở e2e-api không mock nghiệp vụ.

[Báo cáo kiểm chứng](../docs/reports/2026-10-21-fe-be-integration.md) và [bản demo lịch sử](../docs/reports/2026-10-20-angular-demo.md).
