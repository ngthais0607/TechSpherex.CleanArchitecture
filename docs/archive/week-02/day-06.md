# Tuần 2 - Ngày 6: Rà Soát Application

## 1. Việc Đã Thực Hiện

- Rà soát toàn bộ 29 handlers và 17 validators.
- Kiểm tra CQRS, dependency direction, error handling, role và TimeProvider.
- Đối chiếu mô tả tài liệu với code thực tế.
- Xác định các phần cần Infrastructure triển khai và kiểm thử tiếp.

## 2. Yêu Cầu Cần Giải Quyết

- Không để Application phụ thuộc Infrastructure hoặc Api.
- Không gọi trực tiếp đồng hồ hệ thống trong business handler.
- Không khẳng định transaction, tenant isolation hoặc concurrency khi chưa có integration test.
- Giữ số liệu và chữ ký trong tài liệu đồng bộ source.

## 3. Phương Pháp Và Kết Quả

- Architecture tests bảo vệ dependency, tên class, `sealed` và việc handler không phụ thuộc validator.
- Gate-In/Gate-Out cùng các luồng thời hạn dùng `TimeProvider`.
- Xác nhận hiện còn warning nullable/xUnit analyzer và ghi rõ giới hạn này trong báo cáo.
- Infrastructure tiếp theo cần mapping EF Core, authentication, tenant filter và relational tests.
