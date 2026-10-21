# Tuần 2: Cấu Trúc Application Và Giải Thích Kỹ Thuật

## 1. Phạm Vi

Tuần 2 xây dựng tầng `Application`: nhận command/query, kiểm tra dữ liệu đầu vào, điều phối Domain và persistence abstraction, rồi trả `Result`. Tầng này không chứa HTTP endpoint và không hiện thực database, token hay password hashing.

```text
src/Application/
├── Abstractions/
│   ├── Caching/             # Hợp đồng cache, chưa có use case sử dụng
│   ├── Data/                # IAppDbContext
│   ├── Identity/            # Current user, password hasher, token service
│   ├── Messaging/           # CQRS in-house
│   ├── Rules/               # IRuleEngine và RuleResult
│   └── Tenancy/             # Tenant provider và TenantInfo
├── Common/
│   ├── Roles.cs             # Tên role dùng thống nhất
│   └── Rules/RuleEngine.cs  # Chạy tập business rules
├── Features/
│   ├── Containers/
│   ├── Identity/
│   ├── MasterData/
│   ├── Operations/
│   └── Yard/
└── DependencyInjection.cs   # Đăng ký handler, validator, rule engine, TimeProvider
```

Mô hình Domain được giải thích tại [tài liệu tham chiếu Tuần 1](../week-01/codebase-structure-and-files.md).

## 2. Nhiệm Vụ Từng Khu Vực

### 2.1. Abstractions

| Hợp đồng | Nhiệm vụ |
|---|---|
| `IAppDbContext` | Cung cấp `DbSet` cho handler và một `SaveChangesAsync`. Đây là persistence boundary, không tự bảo đảm tenant filter hoặc transaction cho mọi provider. |
| `ICacheService` | Chuẩn bị giao diện cache; Tuần 2 chưa có handler sử dụng. |
| `ICurrentUser` | Cung cấp danh tính và role của người dùng hiện tại. |
| `IPasswordHasher` | Hash và verify mật khẩu/refresh token mà không đưa thư viện cụ thể vào Application. |
| `ITokenService` | Tạo access token và refresh token thông qua Infrastructure. |
| `ITenantProvider` | Cung cấp tenant hiện tại; isolation chỉ có hiệu lực khi Infrastructure áp dụng filter và gán tenant. |
| `IRuleEngine` | Đánh giá tập `IBusinessRule` của Domain. |
| Messaging interfaces | Định nghĩa command, query và handler mà không phụ thuộc MediatR. |

### 2.2. Common

`Roles` tập trung các role `Admin`, `YardPlanner`, `OrderClerk`, `GateClerk`. `RuleEngine` chạy rule và trả lỗi đầu tiên bị vi phạm. Việc tập trung tên role tránh chuỗi rải rác, nhưng authorization cuối cùng vẫn phải được nối vào API/policy.

### 2.3. Features

| Feature | Use case chính |
|---|---|
| `Identity` | Login, đăng ký tài khoản bởi Admin, refresh-token rotation. |
| `MasterData` | Tạo và truy vấn Customer, LineOperator, Vehicle, Voyage. |
| `Containers` | Đăng ký, ghi nhận hư hỏng, tra cứu theo số và phân trang. |
| `Yard` | Tạo/mở rộng/thu hẹp block; đặt, dỡ, chuyển container; tìm slot và xem layout. |
| `Operations` | Tạo/truy vấn Delivery Order; Gate-In, Gate-Out và lịch sử lifecycle. |

Mỗi use case đặt command/query, handler, validator và DTO gần nhau. Handler điều phối; entity/rule của Domain giữ invariant nghiệp vụ.

## 3. CQRS In-House

### Command và Query

- `ICommand`: thao tác không cần giá trị trả về ngoài `Result`.
- `ICommand<TResponse>`: thao tác trả dữ liệu như `Guid` hoặc DTO.
- `IQuery<TResponse>`: yêu cầu đọc dữ liệu.
- `ICommandHandler<...>` và `IQueryHandler<...>`: hợp đồng xử lý bất đồng bộ với `CancellationToken`.

CQRS ở đây là tách kiểu request và handler, không đồng nghĩa dự án có hai database đọc/ghi hoặc event bus.

### Generic variance `in`

```csharp
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
```

`in TCommand` khai báo contravariance vì `TCommand` chỉ được nhận làm đầu vào. `where` đặt generic constraint để compiler chỉ cho phép command đúng hợp đồng.

### `record` cho request và DTO

Command, query và nhiều DTO dùng `record` để có cú pháp gọn và so sánh theo giá trị. Record có thể vẫn chứa object mutable; chỉ nên gọi là bất biến khi các property/thành phần bên trong cũng không cho sửa trực tiếp.

### `sealed` cho handler và validator

Handler/validator là điểm kết thúc của một use case nên được đánh dấu `sealed`. Architecture test kiểm tra quy ước này cho handler. Đây chủ yếu là ranh giới thiết kế; không nên xem nó như một cam kết tăng hiệu năng đáng kể nếu chưa benchmark.

## 4. Dependency Injection Bằng Reflection

`DependencyInjection.AddApplication()` đăng ký:

1. `IRuleEngine` dưới dạng singleton.
2. `TimeProvider.System` dưới dạng singleton mặc định.
3. Tất cả implementation của `IValidator<T>` dưới dạng transient.
4. Tất cả implementation của `ICommandHandler<>`, `ICommandHandler<,>` và `IQueryHandler<,>` dưới dạng transient.

Reflection đọc type từ Application assembly lúc khởi động. `GetGenericTypeDefinition()` đưa closed generic như `ICommandHandler<LoginCommand, AuthResponseDto>` về generic definition để so sánh. Khi thêm handler mới đúng interface, không cần đăng ký từng dòng.

## 5. Validation Và Result

FluentValidation kiểm tra hình thức request: trường bắt buộc, độ dài, số dương và định dạng. Handler không tự tiêm validator; API/pipeline của giai đoạn tích hợp chịu trách nhiệm gọi validator trước handler.

Domain/Application trả `Result` cho lỗi nghiệp vụ dự kiến. Handler thường trả:

- `Validation`: dữ liệu hoặc trạng thái không hợp lệ.
- `NotFound`: không tìm thấy entity cần thao tác.
- `Conflict`: xung đột trạng thái như slot đã chiếm hoặc lifecycle đã Gate-Out.
- `Forbidden`: người dùng hiện tại không đủ quyền.

Không nên diễn giải cơ chế này là “không có exception”. Lỗi kết nối database, cấu hình hoặc bug lập trình vẫn có thể phát sinh exception và cần middleware xử lý.

## 6. TimeProvider

Handler cần thời gian hiện tại nhận `TimeProvider` qua constructor. Ví dụ constructor thực tế của Gate-Out:

```csharp
public GateOutCommandHandler(
    IAppDbContext context,
    IRuleEngine ruleEngine,
    TimeProvider timeProvider)
```

`GateInCommandHandler` cũng nhận `TimeProvider`; thời gian được truyền xuống `ContainerLifecycle.GateIn/GateOut`. Unit test dùng fixed provider nội bộ để có kết quả tất định.

`FakeTimeProvider` thuộc package `Microsoft.Extensions.TimeProvider.Testing`; project hiện không cài package đó. Không dùng ví dụ `FakeTimeProvider` như thể nó có sẵn nếu chưa thêm dependency.

## 7. Handler Và Validator Hiện Có

Source hiện có **29 handlers** và **17 validators**.

| Nhóm | Handlers | Validators |
|---|---:|---:|
| Identity | 3 | 3 |
| Master Data | 8 | 4 |
| Containers | 4 | 1 |
| Yard | 8 | 6 |
| Operations | 6 | 3 |
| **Tổng** | **29** | **17** |

Các query đọc hiện không có FluentValidation riêng khi không mang đầu vào cần kiểm tra. `MarkContainerDamaged` có handler nhưng không có validator; handler tự xử lý trường hợp ID không tồn tại.

## 8. Luồng Điều Phối Chính

### Đăng ký container

1. Query kiểm tra trùng số trong `IAppDbContext`.
2. `ContainerNumber.Create` kiểm tra ISO 6346.
3. `Container.Create` kiểm tra trọng lượng và tạo entity.
4. Handler thêm entity và gọi `SaveChangesAsync`.

### Đặt container vào bãi

1. Tải container và block.
2. Tạo bốn yard rules từ Domain.
3. `IRuleEngine` đánh giá rule.
4. Chiếm một slot cho 20ft hoặc hai slot đỡ cho 40ft.
5. Lưu thay đổi.

### Gate-In

1. Tải container và slot, kiểm tra slot trống và container chưa có lifecycle đang ở bãi.
2. Chiếm slot.
3. Tạo `ContainerLifecycle` với thời gian từ `TimeProvider`.
4. Lưu lifecycle và slot trong một lần `SaveChangesAsync`.

### Gate-Out

1. Tải container, lifecycle đang hoạt động và Delivery Order kèm items.
2. Đánh giá bốn dispatch rules.
3. Phân bổ quota và đánh dấu hoàn thành lệnh khi đủ số lượng.
4. Giải phóng slot và cập nhật lifecycle với thời gian từ `TimeProvider`.
5. Gọi một lần `SaveChangesAsync`.

Một lần `SaveChangesAsync` là một ranh giới lưu thay đổi ở Application. Tính atomic/ACID và rollback phải được kiểm chứng bằng relational integration test với implementation EF Core thực tế; EF Core InMemory không chứng minh đặc tính này.

## 9. Kiểm Thử Tuần 2

| Suite | Số test | Phạm vi |
|---|---:|---|
| `Domain.UnitTests` | 120 | Invariant, value object và business rules của Tuần 1. |
| `Application.UnitTests` | 131 | Handler, validator, query và application workflow dùng `TestAppDbContext`. |
| `Architecture.Tests` | 7 | Hướng phụ thuộc và quy ước Domain/Application. |
| **Toàn solution** | **258** | Kết quả hiện tại, không bao gồm relational/API end-to-end test. |

`DepotEndToEndWorkflowTests` là **application workflow test**: nó gọi handler trực tiếp và dùng `TestAppDbContext`. Test này chứng minh luồng phối hợp use case trong bộ nhớ, không phải E2E qua HTTP, authentication, middleware và database thật.

### Giới hạn của InMemory

- Không phản ánh đầy đủ transaction/rollback của PostgreSQL hoặc SQL Server.
- Không bảo đảm foreign key, unique index và check constraint giống relational provider.
- Không chứng minh global tenant query filter, migration hoặc optimistic concurrency.
- ChangeTracker có thể trả entity đang theo dõi thay vì mô phỏng một truy vấn database hoàn toàn mới.

Các điểm này thuộc integration test của Infrastructure/API ở giai đoạn tiếp theo.

## 10. Architecture Tests

Các kiểm tra hiện có xác nhận:

- Application không tham chiếu Infrastructure hoặc Api.
- Command/query handler tuân theo hậu tố tên.
- Validator tuân theo hậu tố `Validator`.
- Handler không phụ thuộc trực tiếp FluentValidation.
- Handler là `sealed`.
- Domain giữ hướng phụ thuộc độc lập qua test kiến trúc tương ứng.

Architecture test bảo vệ cấu trúc assembly; nó không thay thế unit test nghiệp vụ hoặc integration test persistence.
