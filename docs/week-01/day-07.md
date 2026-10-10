# Tuần 1 - Ngày 7: Hoàn Thiện Kiểm Thử Domain

## 1. Việc Đã Thực Hiện

- Hoàn thiện test cho value objects, entities, aggregate và tám business rules.
- Bổ sung architecture test cho hướng phụ thuộc Domain.
- Chuẩn hóa cách đặt tên và bố cục Arrange-Act-Assert.
- Chuẩn bị hợp đồng Domain để Application sử dụng ở Tuần 2.

## 2. Yêu Cầu Cần Giải Quyết

- Bảo vệ invariant trước khi thêm tầng điều phối.
- Phủ các trường hợp biên của ISO 6346, bãi, quota và lifecycle.
- Phân biệt unit test nghiệp vụ với integration test database.
- Ghi số liệu kiểm thử theo đúng thời điểm thay vì coi là con số cố định.

## 3. Phương Pháp Và Kết Quả

- Tại thời điểm kết thúc Tuần 1, báo cáo ghi nhận 120 Domain unit tests và architecture coverage ban đầu.
- Bộ test hiện tại vẫn có 120 Domain tests; architecture suite đã được mở rộng trong Tuần 2.
- Unit test không chứng minh foreign key, transaction hoặc concurrency của database quan hệ.
- Domain sẵn sàng được sử dụng bởi các handler ở Application.

Ma trận kiểm thử hiện tại xem tại [tài liệu tham chiếu Tuần 1](codebase-structure-and-files.md).
