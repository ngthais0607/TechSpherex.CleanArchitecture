# Kết quả UAT tuần 4

Trạng thái: **Awaiting UAT**. Người nghiệm thu/ngày chạy chưa được cung cấp. Không có chữ ký hoặc pass được tự tạo.

Bằng chứng tự động sau audit: **415 passed/0 failed/0 skipped**, build0 lỗi; image test mới và upgrade6 migration pass; collection Postman **85 requests/135 assertions,0 failed**. Production startup và backup/restore đã pass trên DB dùng một lần ở ngày4. Kết quả tự động được ghi riêng với chữ ký UAT của người dùng. Người dùng xác nhận sẽ chạy UAT và báo kết quả sau.

Thực hiện và ghi actual theo [UAT-CHECKLIST](../UAT-CHECKLIST.md). Tự động hóa hỗ trợ không thay thế quyết định nghiệm thu của người dùng.

| Nhóm | Expected | Actual bởi người dùng | Trạng thái |
|---|---|---|---|
| Roles/auth/tenant | Đúng matrix, không chọn depot bằng header | Chưa cung cấp | Awaiting |
| Gate/yard/quota | Full flow và rollback/safety đúng | Chưa cung cấp | Awaiting |
| 40ft/reentry/history | Hai slot/một event; giữ lượt cũ | Chưa cung cấp | Awaiting |
| Reports/timezone | Counts, paging và ngày UTC+7 đúng | Chưa cung cấp | Awaiting |
| Deployment/restore | Môi trường thử và evidence được chấp nhận | Chưa cung cấp | Awaiting |

Known issue từ UAT: chưa có báo cáo. Điều này không có nghĩa đã nghiệm thu hoặc không có lỗi. Owner/due date của lỗi chưa phát sinh để trống, không tự bịa.
