# SnpDepotManagement

Hệ thống quản lý depot container vỏ rỗng được xây dựng theo Clean Architecture trên .NET 10.

## Tài Liệu

- [Tuần 1 - Domain](docs/week-01/README.md): entity, value object, aggregate và business rules.
- [Tuần 2 - Application](docs/week-02/README.md): CQRS in-house, validation và điều phối use case.

## Trạng Thái Kiểm Thử

- Domain: 120 tests.
- Application: 131 tests.
- Architecture: 7 tests.
- Tổng: 258 tests.

Các Application tests dùng EF Core InMemory. Transaction, tenant isolation và luồng E2E qua API/database thật cần được xác minh ở giai đoạn Infrastructure/API.
