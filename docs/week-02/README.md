# Tuần 2: Application Layer

Tuần 2 xây tầng điều phối use case trên Domain bằng CQRS in-house, FluentValidation và persistence abstraction.

## Nhật Ký

| Ngày | Nội dung | Tài liệu |
|---|---|---|
| 1 | Abstractions, CQRS và đăng ký dependency injection | [day-01.md](day-01.md) |
| 2 | Identity và master data | [day-02.md](day-02.md) |
| 3 | Container và cấu hình bãi | [day-03.md](day-03.md) |
| 4 | Đặt, dỡ, chuyển và tìm vị trí bãi | [day-04.md](day-04.md) |
| 5 | Delivery Order, Gate-In và Gate-Out | [day-05.md](day-05.md) |
| 6 | Rà soát Application và chuẩn bị Infrastructure | [day-06.md](day-06.md) |
| 7 | Application workflow và architecture tests | [day-07.md](day-07.md) |

## Tài Liệu Tham Chiếu

[Cấu trúc Application và giải thích kỹ thuật](codebase-structure-and-files.md) tổng hợp CQRS, generic variance, record, reflection DI, validator, TimeProvider, handlers và giới hạn kiểm thử.

## Kết Quả Hiện Tại

- 29 handlers và 17 validators.
- 131 Application tests và 7 Architecture tests.
- Toàn solution có 258 tests.
- `DepotEndToEndWorkflowTests` là application workflow test dùng `TestAppDbContext`, chưa phải E2E qua API/database thật.
- Một lần `SaveChangesAsync` là persistence boundary; ACID/rollback cần relational integration test.
