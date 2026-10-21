# Tuần 1: Domain Layer

Tuần 1 xây nền tảng nghiệp vụ độc lập với API và persistence: entity, value object, aggregate, enum và tám business rules.

## Nhật Ký

| Ngày | Nội dung | Tài liệu |
|---|---|---|
| 1 | Khởi tạo solution và nền tảng Domain | [day-01.md](day-01.md) |
| 2 | Container, master data và identity | [day-02.md](day-02.md) |
| 3 | Cấu trúc block và slot bãi | [day-03.md](day-03.md) |
| 4 | Bốn quy tắc xếp bãi | [day-04.md](day-04.md) |
| 5 | Delivery Order, lifecycle và bốn quy tắc xuất cổng | [day-05.md](day-05.md) |
| 6 | Rà soát Domain và chốt hướng Application | [day-06.md](day-06.md) |
| 7 | Hoàn thiện kiểm thử Domain | [day-07.md](day-07.md) |

## Kết Quả Hiện Tại

- 120 Domain unit tests.
- Tám business rules được kiểm thử độc lập.
- Domain không phụ thuộc Application, Infrastructure hoặc Api.
- Thiết kế database mới là đề xuất; cần EF Core mapping và relational integration test ở giai đoạn sau.
