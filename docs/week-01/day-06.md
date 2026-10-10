# Tuần 1 - Ngày 6: Rà Soát Domain

## 1. Việc Đã Thực Hiện

- Rà soát invariant, constructor, setter và quyền sở hữu collection.
- Kiểm tra hướng phụ thuộc Clean Architecture.
- Đánh giá CQRS, MediatR, repository, Unit of Work và Domain Events cho Tuần 2.
- Chốt hướng CQRS in-house và persistence boundary bằng `IAppDbContext`.

## 2. Yêu Cầu Cần Giải Quyết

- Loại bỏ mô tả hoặc API không khớp code thực tế.
- Giữ Domain độc lập với framework và persistence.
- Chọn cách điều phối đủ đơn giản cho phạm vi hiện tại.
- Xác định rõ phần đã làm và phần chỉ là thiết kế cho giai đoạn sau.

## 3. Phương Pháp Và Kết Quả

- Giữ entity đóng gói bằng constructor hạn chế, `private set` và factory.
- Không thêm MediatR, repository hay Domain Events khi chưa có nhu cầu thực tế.
- Chọn CQRS interfaces tự định nghĩa; Infrastructure sẽ hiện thực persistence.
- Các nhận định về transaction, tenant và database được giữ ở mức thiết kế chưa kiểm chứng.
