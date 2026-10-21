# Ngày 5 — Postman, cấu hình và UAT

Trạng thái: tài liệu/collection sẵn sàng, **Awaiting UAT**; người dùng báo sẽ chạy và gửi kết quả sau, chưa commit nghiệm thu ngày5.

Đã chuẩn bị hướng dẫn tự sinh JWT/DB/Admin secrets, giải thích không cần API key bên thứ ba, API guide, collection/environment placeholder và checklist vai trò. File codebase inventory sẽ chỉ lưu cục bộ theo yêu cầu, không đưa lên Git.

Collection v2.1 dùng token từng vai trò, lưu Id từ response, tạo suffix/number check digit hợp lệ, test quota/reports/ProblemDetails/tenant/refresh. Bản mở rộng đã chạy trên Docker thật: **85 requests, 135 assertions, 0 failed**, gồm 40ft, footprint, stack và reentry. Newman 6.2.3 được cài trong scratch ignored, không phải dependency của API. Secret runner giữ trong memory, không xuất report raw headers/token. Một lượt sau khi Docker tắt chỉ lỗi kết nối; đã khởi động lại engine và chạy đầy đủ lại, không tính lượt lỗi môi trường là pass.

Tài liệu:

- [Bạn cần làm gì theo thứ tự](../../README.md)
- [Cấu hình/key](CONFIGURATION-GUIDE.md)
- [Postman](POSTMAN-GUIDE.md)
- [API guide](../API-GUIDE.md)
- [UAT checklist](../UAT-CHECKLIST.md)
- [UAT results](uat-results.md)

Người dùng cần chạy/xác nhận UAT thật. Có thể tiếp tục audit ngày 6 trong lúc chờ; không tự ký pass hoặc commit mốc complete ngày 5 khi chưa có xác nhận nghiệm thu.
