# Tổng Hợp Logic Nghiệp Vụ & Giải Pháp Kiến Trúc: Tuần 1 (Domain Layer)

Tài liệu này tổng hợp toàn bộ các **bài toán logic cốt lõi**, **thách thức kỹ thuật** và **giải pháp thiết kế** đã được áp dụng trong Tuần 1 (xây dựng lớp Domain cho hệ thống quản lý Depot container rỗng - SnpDepotManagement), tập trung vào phân tích bản chất nghiệp vụ và tư duy kiến trúc mà không lạm dụng mã nguồn.

---

## 1. Bảng Ma Trận Bài Toán & Giải Pháp Tổng Thể

| Phân Hệ / Nhóm Nghiệp Vụ | Bài Toán & Thách Thức Kỹ Thuật | Giải Pháp Kiến Trúc & Nghiệp Vụ Đã Chọn |
|---|---|---|
| **Xử lý luồng thực thi & Mã lỗi** | Ném Exception gây suy giảm hiệu năng và khó kiểm soát luồng; thông báo lỗi bằng chuỗi tự do gây khó khăn khi đa ngôn ngữ (i18n). | Áp dụng **Result Pattern** (`Result<T>`) và danh mục **Static Error Codes** phân tầng rõ rệt (`Validation`, `NotFound`, `Conflict`). |
| **Đa chi nhánh / Đa Depot** | Dữ liệu giữa các bãi khác nhau (Cát Lái, Hiệp Phước,...) có nguy cơ bị lẫn lộn nếu không kiểm soát từ gốc Domain. | Áp dụng interface đánh dấu **`ITenantEntity`** trên mọi Entity thuộc bãi để phục vụ Global Query Filter tại tầng Data. |
| **Nhận dạng số Container** | Lỗi gõ nhầm số container khi làm thủ tục tại cổng; định dạng ký tự lộn xộn (chữ thường, khoảng trắng). | Xây dựng Value Object **`ContainerNumber`** tự chuẩn hóa và triển khai thuật toán **ISO 6346 Modulo 11** với 7 mã lỗi chuyên biệt. |
| **Không gian bãi lưu trữ (Yard)** | Container 40ft dài gấp đôi 20ft; nếu lưu riêng cả slot 20ft và slot 40ft trong DB sẽ gây trùng lặp dữ liệu và xung đột không gian. | Áp dụng **Quy ước Bay Lẻ (Odd-Bay Convention)**: DB chỉ lưu ô 20ft ở bay lẻ. Container 40ft được định danh bằng bay chẵn và chiếm dụng 2 ô bay lẻ liền kề. |
| **Quy hoạch & Thay đổi quy mô bãi** | Bãi thay đổi diện tích (mở rộng/thu hẹp); rủi ro thu hẹp vùng bãi khi container vẫn đang nằm trên các ô bị xóa. | Sử dụng **Aggregate Root (`YardBlock`)** điều phối `YardSlot`. Thuật toán `Shrink` thực hiện **Conflict Detection nguyên tử**, từ chối cắt bỏ nếu còn container. |
| **An toàn vật lý khi xếp chồng** | Nguy cơ container bị lơ lửng, container 40ft bị chìa ra ngoài đường nội bộ hoặc đặt lệch lên 1 cột 20ft gây lật đổ bãi. | Bộ 4 **Yard Business Rules** (Specification Pattern): Chặn mép bãi cho cont 40ft; bắt buộc có 2 chân đế đỡ bên dưới (**Dual Supporting Stack Physics**). |
| **Hạn mức cấp vỏ lệnh giao (DO)** | Khách hàng lấy vượt số lượng được hãng tàu duyệt; chỉnh sửa đơn hàng khi lệnh đã hoàn tất. | Đóng gói biến tính toán `RemainingQuantity` trong Aggregate `DeliveryOrder`. Phương thức `Allocate` trừ quota nguyên tử và khóa lệnh khi hoàn thành. |
| **Gian lận & Thất thoát khi Gate-Out** | Container đã ra khỏi cổng nhưng bị quét xuất lần 2; xuất vỏ của hãng tàu này cho khách hàng cầm lệnh hãng tàu khác; xuất cont ảo. | **Finite State Machine (FSM)** trong `ContainerLifecycle` chặn xuất 2 lần, tự động thu hồi slot bãi. Pipeline **4 Dispatch Rules** kiểm tra chéo trước khi mở barrier. |
| **Bảo vệ ranh giới Clean Architecture** | Theo thời gian, lập trình viên có thể vô tình tham chiếu Application/Infrastructure vào tầng Domain. | Thiết lập **Architecture Fitness Function** bằng `NetArchTest.Rules`, tự động phân tích binary assembly trong pipeline kiểm thử. |

---

## 2. Phân Tích Chuyên Sâu Từng Nhóm Logic & Giải Pháp

### 2.1. Quản Lý Luồng Dữ Liệu & Mã Lỗi (Result Pattern & Static Errors)

* **Vấn đề thực tế**:
  Trong các hệ thống quản lý logistics với tần suất thao tác cao, việc sử dụng cơ chế `try-catch / throw Exception` cho các lỗi kiểm tra nghiệp vụ thông thường (như nhập sai số container, số lượng cấp vượt hạn mức) là một anti-pattern. Exception trong .NET tiêu tốn chi phí rất lớn để khởi tạo Stack Trace, làm chậm hệ thống và khiến luồng code bị nhảy cóc (GOTO ngầm định).
* **Giải pháp kiến trúc**:
  - Triển khai mô hình **Railway-Oriented Programming** thông qua lớp `Result` và `Result<T>`. Mọi hàm nghiệp vụ đều trả về đối tượng kết quả chứa cờ `IsSuccess` và đối tượng `Error`.
  - Thay vì trả về chuỗi thông báo tự do, hệ thống định nghĩa **Mã lỗi tĩnh (Static Error Code)** có cấu trúc: `DomainScope.Reason` (Ví dụ: `Container.InvalidTareWeight`, `Customer.InvalidTaxCodeLength`).
  - Phân loại lỗi thành các nhóm ngữ nghĩa:
    - `Validation`: Dữ liệu đầu vào sai quy chuẩn.
    - `NotFound`: Không tìm thấy tài nguyên yêu cầu.
    - `Conflict`: Xung đột trạng thái nghiệp vụ (ví dụ: ô bãi đã có người chiếm, container đã xuất cổng).

---

### 2.2. Nhận Dạng Container & Chuẩn Hóa Quốc Tế ISO 6346

```
        4 Ký tự chữ           6 Ký tự số      1 Số KT
      ┌───┬───┬───┬───┐   ┌───┬───┬───┬───┬───┬───┐   ┌───┐
      │ C │ S │ Q │ U │   │ 3 │ 0 │ 5 │ 4 │ 3 │ 8 │   │ 3 │
      └───┴───┴───┴───┘   └───┴───┴───┴───┴───┴───┘   └───┘
        Owner   Category        Serial Number       Check Digit
```

* **Vấn đề thực tế**:
  Container là tài sản lưu chuyển toàn cầu. Việc gõ nhầm một ký tự hoặc số kiểm tra có thể dẫn đến việc giao nhầm hàng, thất lạc tài sản hoặc sai lệch cước lưu bãi hàng triệu USD.
* **Giải pháp kiến trúc**:
  - Xây dựng [ContainerNumber](file:///d:/SnpDepotManagement/src/Domain/ValueObjects/ContainerNumber.cs) dưới dạng **Value Object bất biến (Immutable)**. Một khi đối tượng được tạo ra, nó được bảo đảm hợp lệ $100\%$.
  - Tự động chuẩn hóa: Cắt bỏ khoảng trắng thừa hai đầu và chuyển toàn bộ sang chữ in hoa (`ToUpperInvariant`).
  - Triển khai thuật toán **Modulo 11**:
    1. Gán giá trị số cho 4 chữ cái đầu theo bảng chuẩn ISO (bỏ qua các giá trị 11, 22, 33 để tránh chia hết cho 11).
    2. Nhân mỗi chữ số và chữ cái với trọng số lũy thừa của 2 tương ứng với vị trí: $w_i = 2^i$ với $i \in [0, 9]$.
    3. Tính tổng có trọng số: $\Sigma = \sum_{i=0}^9 (v_i \times 2^i)$.
    4. Số kiểm tra hợp lệ được tính bằng: $d = (\Sigma \pmod{11}) \pmod{10}$.
  - Nếu kết quả tính toán khác với chữ số thứ 11, Value Object từ chối khởi tạo và trả về lỗi `ContainerNumber.CheckDigitMismatch`.

---

### 2.3. Không Gian Bãi & Giải Pháp Quy Hoạch Bay Lẻ (Odd-Bay Convention)

* **Vấn đề thực tế**:
  Bãi container là không gian 3 chiều (Dài - Rộng - Cao) tương ứng với Bay - Row - Tier. Thách thức lớn nhất là sự khác biệt về kích thước: Container 20ft dài $\approx 6\text{m}$, container 40ft dài $\approx 12\text{m}$. Nếu tạo 2 bảng dữ liệu riêng cho slot 20ft và slot 40ft thì khi một container 40ft hạ xuống, hệ thống sẽ rất khó khóa hai ô 20ft bên dưới, dễ gây lỗi xếp đè lên nhau.
* **Giải pháp kiến trúc**:
  - Áp dụng **Quy ước Bay Lẻ**: Trong Database, hệ thống **CHỈ** sinh và quản lý các ô bãi cơ sở tại các **Bay lẻ** ($01, 03, 05, \dots$).
  - Khi tiếp nhận container 20ft: Container chiếm đúng 1 ô tại bay lẻ chỉ định.
  - Khi tiếp nhận container 40ft: Vị trí của nó được định danh bằng **Bay chẵn** ở giữa hai bay lẻ tương ứng (Ví dụ: cont 40ft tại Bay 02 sẽ chiếm dụng vật lý cả 2 slot Bay 01 và Bay 03).
  - Áp dụng mẫu **Aggregate Root (`YardBlock`)**: Toàn bộ danh sách `YardSlot` được bao bọc bên trong block. Bên ngoài không thể thêm, xóa, sửa slot tùy tiện mà phải thông qua các phương thức nghiệp vụ của `YardBlock`.
  - **Thu hẹp bãi an toàn (`Shrink`)**: Khi bãi cần giảm quy mô, thuật toán kiểm tra vùng cắt $\mathcal{S}_{\text{remove}}$. Nếu phát hiện bất kỳ slot nào trong vùng giải tỏa đang có container (`IsOccupied == true`), lệnh thu hẹp lập tức bị hủy bỏ nguyên tử để bảo vệ an toàn vật lý.
  - **Khái niệm "Block Ảo" (Virtual Block)**: Đối với các phân khu đặc thù không có tọa độ cố định 3 chiều (như khu sửa chữa container hư hỏng M&R, khu xịt rửa vệ sinh, khu cắm điện chạy thử PTI cont lạnh, khu đệm chờ xuất), hệ thống hỗ trợ tạo `YardBlock.CreateVirtual`. Block ảo có `MaxBays = MaxRows = MaxTiers = 0`, không sinh ra slot vật lý và bị khóa các thao tác `Expand`/`Shrink`, giúp theo dõi trạng thái vị trí logic của container mà không bị ràng buộc bởi ma trận hình học cảng.

---

### 2.4. Động Lực Học Trọng Trường & Quy Tắc Xếp Chồng (Yard Safety Rules)

```
   TRƯỜNG HỢP HỢP LỆ (Đủ 2 chân đỡ):         TRƯỜNG HỢP NGUY HIỂM (Bị Chặn):
   ┌───────────────────────────────┐         ┌───────────────────────────────┐
   │         Container 40ft        │         │         Container 40ft        │
   ├───────────────┬───────────────┤         ├───────────────┬───────────────┤
   │ Cont 20ft (L) │ Cont 20ft (R) │         │ Cont 20ft (L) │    TRỐNG!     │ ──> SẬP BÃI!
   └───────────────┴───────────────┘         └───────────────┴───────────────┘
```

* **Vấn đề thực tế**:
  Trong depot vỏ rỗng, container được xếp chồng cao từ 5 đến 7 tầng. Nếu hệ thống điều xe cho phép hạ một container lơ lửng, hoặc đặt container 40ft lên một vị trí mà bên dưới chỉ có 1 thùng 20ft đỡ một đầu, trọng lực và gió bão sẽ bẻ gãy cấu trúc và gây sập cả dãy bãi.
* **Giải pháp kiến trúc**:
  Triển khai **Specification Pattern** qua giao diện `IBusinessRule`, biến các định luật vật lý thành 4 quy tắc độc lập:
  1. **Tương thích kích thước và biên mép (`ValidBayContainerSizeRule`)**:
     - 20ft bắt buộc bay lẻ, 40ft bắt buộc bay chẵn.
     - Kiểm tra mép block: Nếu cont 40ft ở bay chẵn mà $\text{bay} + 1 > \text{MaxBays}$ thì từ chối ngay vì cont sẽ bị thò ra ngoài đường nội bộ.
  2. **Tĩnh không tối đa (`TierWithinBlockLimitRule`)**:
     - Ngăn ngừa việc xếp vượt quá chiều cao nâng của xe nâng Reach Stacker hoặc tải trọng nền móng của block.
  3. **Chống lơ lửng & Chịu lực kép (`SlotBelowMustBeOccupiedRule`)**:
     - Tầng 1: Đặt trên mặt sàn bê tông nên luôn hợp lệ.
     - Tầng $n > 1$: Đối với cont 20ft, ô bên dưới tại tầng $n-1$ phải có container. Đối với cont 40ft, **CẢ HAI** ô phía trước $(\text{bay}-1)$ và phía sau $(\text{bay}+1)$ ở tầng $n-1$ đều bắt buộc phải có container đỡ.
  4. **Chống đè lấp (`SlotIsFreeRule`)**:
     - Đảm bảo các ô dự kiến hạ container chưa bị bất kỳ thùng nào khác chiếm chỗ.

---

### 2.5. Phân Bổ Hạn Mức & Vòng Đời Vỏ Rỗng (Operations & FSM)

* **Vấn đề thực tế**:
  Khi xuất vỏ rỗng cho khách hàng đóng hàng xuất khẩu, depot đối mặt với 3 rủi ro lớn:
  1. Cấp vượt số lượng cho phép của hãng tàu trên lệnh giao (DO).
  2. Gian lận cổng: Một container đã ra khỏi bãi nhưng bảo vệ/nhân viên cổng vẫn xác nhận cho một xe khác xuất container đó lần thứ hai (Double Gate-Out).
  3. Cấp nhầm vỏ của hãng tàu khác gây tranh chấp pháp lý.
* **Giải pháp kiến trúc**:
  - **Quản trị hạn mức (`DeliveryOrder` - Aggregate Root)**:
    - Quản lý các dòng hàng `DeliveryOrderItem` theo từng kích thước/loại vỏ (20ft Dry, 40ft Reefer,...).
    - Biến tính toán $\text{RemainingQuantity} = \text{Required} - \text{Delivered}$. Phương thức `Allocate` kiểm tra số lượng khả dụng trước khi trừ hạn mức và tự động khóa lệnh khi hoàn tất.
  - **Máy trạng thái hữu hạn (FSM) trong `ContainerLifecycle`**:
    - Chu trình trạng thái: `[Initial]` $\rightarrow$ `GateIn` (`InYard`) $\rightarrow$ `GateOut` (`GatedOut`).
    - Ngăn chặn Double Gate-Out: Phương thức `GateOut` kiểm tra nghiêm ngặt `Status == InYard && OutDate == null`. Nếu vi phạm sẽ trả về lỗi `Conflict`.
    - Dọn dẹp tài nguyên nguyên tử: Khi Gate-Out thành công, `CurrentSlotId` được gán về `null`, tự động giải phóng vị trí trên bãi để tiếp nhận thùng khác.
  - **Pipeline 4 Quy Tắc Thẩm Định Điều Động (Gate-Out Rules)**:
    - Lệnh giao phải còn hạn sử dụng (`DeliveryOrderNotExpiredRule`, so sánh loại trừ lệch giờ).
    - Lệnh giao phải còn số lượng tồn theo đúng Size & Type (`DeliveryOrderHasRemainingQuantityRule`).
    - Container xuất phải thuộc sở hữu của đúng hãng tàu cấp lệnh (`ContainerBelongsToLineOperatorRule`).
    - Container phải đang hiện diện vật lý trong bãi (`ContainerIsInYardRule`).

---

### 2.6. Tự Động Hóa Kiểm Soát Ranh Giới Kiến Trúc (Architecture Testing)

* **Vấn đề thực tế**:
  Kiến trúc Clean Architecture quy định nghiêm ngặt: Tầng Domain là vùng lõi trung tâm, tuyệt đối không được phụ thuộc vào Application, Infrastructure hay API. Tuy nhiên, trong quá trình phát triển dài hạn, các lập trình viên thường vô tình thêm câu lệnh `using` hoặc tham chiếu project sai chiều, làm xói mòn kiến trúc.
* **Giải pháp kiến trúc**:
  - Sử dụng thư viện `NetArchTest.Rules` trong project kiểm thử [tests/Architecture.Tests](file:///d:/SnpDepotManagement/tests/Architecture.Tests/DomainArchitectureTests.cs).
  - Tự động dùng Reflection quét toàn bộ các kiểu dữ liệu trong Assembly của Domain:
    - Khẳng định: `Types.InAssembly(domainAssembly).ShouldNot().HaveDependencyOnAny(...)`.
  - Nếu bất kỳ ai vô tình thêm reference sai chiều, lệnh `dotnet test` trong quy trình CI/CD sẽ lập tức báo đỏ, chặn đứng lỗi từ cấp độ biên dịch trước khi merge code.

---

## 3. Tổng Kết Giá Trị Kỹ Thuật Đạt Được Sau Tuần 1

1. **Tính Toàn Vẹn Dữ Liệu**: Loại bỏ hoàn toàn tình trạng container ma, tọa độ ảo, xếp lơ lửng, hoặc xuất khống container.
2. **Hiệu Năng Vượt Trội**: Sử dụng `Result Pattern` và loại bỏ Exception khỏi luồng xử lý chính giúp hệ thống phản hồi cực nhanh dưới tải cao.
3. **Tuân Thủ Tuyệt Đối DDD & Clean Architecture**: Tách bạch ranh giới rõ ràng, không phụ thuộc framework ngoài, dễ dàng bảo trì và mở rộng trong các tuần tiếp theo (Application & Infrastructure).
