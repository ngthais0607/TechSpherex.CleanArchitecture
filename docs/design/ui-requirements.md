# Yêu cầu giao diện SnpDepotManagement

**Tài liệu này là nguồn yêu cầu UI chính thức.** `snp-depot-preview.html` và `docs/design/App.approved.tsx` chỉ là bản xem trước mô phỏng: dữ liệu giả, một vài chỗ còn số gõ cứng. Khi bản xem trước và tài liệu này khác nhau, **theo tài liệu này**. Các điểm khác nhau nằm ở mục 7.

Phần API tương ứng nằm trong `backend-requirements-from-ui.md` (mục "Ma trận Giao diện → API").

## 1. Nguyên tắc chung

- Giao diện tiếng Việt. Ngày hiển thị `dd/MM/yyyy`, giờ `HH:mm`, số có dấu chấm ngăn cách hàng nghìn (1.248).
- **Không số gõ cứng.** Mọi số liệu (số container, số lệnh, tỷ lệ lấp đầy, các ô tóm tắt, số container theo loại) lấy từ API. Ngày "hôm nay" lấy từ máy chủ.
- Mỗi màn hình có đủ 4 trạng thái: **đang tải** (khung xương), **lỗi** (thông báo và nút Thử lại), **trống** (nói rõ vì sao trống, có nút hành động nếu hợp lý), **có dữ liệu**.
- Sau mỗi thao tác ghi thành công: hiện thông báo ngắn, và **tải lại dữ liệu bị ảnh hưởng** (danh sách, chi tiết, số liệu tóm tắt ở các màn hình khác). Không tự sửa dữ liệu cục bộ rồi để lệch với máy chủ.
- Lỗi từ API (ProblemDetails) hiển thị thông điệp tiếng Việt và mã quy tắc (ví dụ `D3`). Lỗi 409 do xung đột đồng thời: gợi ý tải lại.
- Hộp thoại: có tiêu đề, `role="dialog"`, đóng bằng phím Esc và nút Đóng, trả focus về nút đã mở nó. Mọi ô nhập có nhãn gắn với ô.
- Dùng được trên laptop và máy tính bảng. Bảng rộng cuộn ngang trong khung, không làm trang cuộn ngang.
- Nút nguy hiểm (Gỡ, Hủy giao dịch) luôn có bước xác nhận.
- Màu theo trạng thái, nhất quán toàn ứng dụng (mục 2). Không dùng màu làm cách duy nhất để phân biệt, luôn kèm chữ.

## 2. Từ điển hiển thị

**Loại container (7):** Dry, Reefer, Open Top, Flat Rack, Bunker, Ventilated, Specialized. Mỗi loại một màu cố định, dùng ở chú thích và sơ đồ bãi.

**Kích thước:** 20ft, 40ft.

**Phân loại:** A, B, C.

**Tình trạng container (danh sách cố định):** Bình thường, Móp, Méo, Cong, Hư hỏng khác. Dùng chung ở form tạo container, bộ lọc, cổng nhập/xuất và chi tiết. Không cho nhập tự do.

**Loại phương tiện:** Xe đầu kéo, Sà lan.

**Trạng thái đặt chỗ (Booking):** Đang giữ chỗ (cyan), Đã xuất (xanh lá), Đã hủy (xám), Hết hạn (đỏ). Nhãn bổ sung **Sắp hết hạn** (vàng) cho đặt chỗ đang giữ chỗ của lệnh còn 3 ngày trở xuống.

**Trạng thái lệnh giao:** Còn hạn, Sắp hết hạn (còn 3 ngày trở xuống, chưa giao đủ), Hết hạn, Đã giao đủ.

**Trạng thái container (`displayStatus`, do API trả về, giao diện không tự suy ra):** Trong bãi, Chờ nhập, Chờ xuất (đã có đặt chỗ), Chờ phân vị trí (ở block ảo), Đang cấp điện (Reefer), Đang kiểm tra, Đã xuất. Hai trạng thái "Đang cấp điện" và "Đang kiểm tra" đang chờ khách xác nhận có giữ không (xem `backend-requirements-from-ui.md` mục 10).

**Giao dịch cổng:** Nhập bãi, Xuất bãi; trạng thái Hoàn tất, Đã hủy.

**Số container** hiển thị `MSKU 482190 4` (4 chữ cái, 6 chữ số, 1 số kiểm tra), lưu không dấu cách.

**Vị trí** hiển thị `A-04-B-1` (Block-Bay-Row-Tier). Block ảo hiển thị mã block (VB-01). Ngoài bãi hiển thị "Ngoài bãi".

## 3. Điều hướng và khung

- Thanh bên trái: Tổng quan, Container, Cổng nhập/xuất, Lệnh giao, Sơ đồ bãi, Khách hàng, Báo cáo, và mục Đăng xuất. Các mục **Khách hàng** và **Báo cáo** gắn nhãn "Mở rộng" (không có trong requirement gốc, ưu tiên sau).
- Thanh trên cùng: tên depot, người dùng hiện tại và vai trò.
- Liên kết giữa màn hình (bấm là chuyển và áp bộ lọc/chọn đúng đối tượng):
  - Chi tiết container → "Xem lệnh" mở chi tiết lệnh tương ứng.
  - Khách hàng → số DO của khách mở danh sách lệnh giao đã lọc theo khách.
  - Ô "Sắp hết hạn" ở Lệnh giao → lọc danh sách lệnh sắp hết hạn.
  - Bản ghi giao dịch cổng → số container mở chi tiết container; số lệnh mở chi tiết lệnh.
  - Kết quả tìm trong Sơ đồ bãi → nhảy đến đúng block, tier, ô.

## 4. Màn hình

### 4.1 Tổng quan
- Bốn ô số: Container trong bãi, Nhập bãi hôm nay, Xuất bãi hôm nay, Tỷ lệ lấp đầy; mỗi ô có xu hướng (% so với hôm qua).
- Biểu đồ **lưu lượng qua cổng 7 ngày**: cột nhập và xuất theo ngày, có chú thích và giá trị khi rê chuột.
- **Công suất bãi theo block**: thanh tiến độ, số đã dùng / sức chứa; trên 80% đổi sang màu cảnh báo.
- **Hoạt động gần đây**: 5 giao dịch cổng mới nhất, bấm mở chi tiết.
- Thêm ô cảnh báo: số **lệnh sắp hết hạn** (bấm mở danh sách lọc) và số **container đã giữ chỗ cho lệnh sắp hết hạn**.

### 4.2 Container
- **Thẻ lọc theo loại** (Tất cả và 7 loại) kèm số lượng, số lượng lấy từ API.
- Thanh công cụ: tìm theo số container hoặc mã ISO; bộ lọc **Tất cả / Đã gán lệnh / Chưa gán lệnh**.
- Bảng: Số container, Loại · ISO, Kích thước, Hãng khai thác, Tình trạng, Phân loại, Vị trí, **Lệnh giao hiện tại** (số lệnh, khách hàng, hạn lệnh, nhãn trạng thái; "Chưa gán" màu nhạt), Trạng thái, Ngày nhập. Phân trang, sắp xếp theo cột chính.
- Bấm một dòng mở **chi tiết** (hộp thoại rộng, 2 tab):
  - **Thông tin:** loại, ISO, kích thước, hãng khai thác, chủ sở hữu, phân loại, tình trạng, trọng lượng tối đa, trọng lượng vỏ, ngày sản xuất, vị trí hiện tại.
  - **Lệnh giao:** khối *Lệnh hiện tại* (số lệnh, khách hàng MST – tên, hãng, hạn lệnh, chuyến tàu, ngày giữ chỗ, nhãn trạng thái, nút **Xem lệnh**). Chưa gán thì hiện "Container chưa được đặt chỗ" và chọn lệnh để **Gán vào lệnh giao** (chỉ liệt kê lệnh hợp lệ; nếu không có thì nêu lý do). Dòng tóm tắt: "Đã qua N khách hàng · Đã xuất M lần · Khách đặt gần nhất: tên". Bảng **Lịch sử lệnh giao** (mới nhất ở trên, đủ các trạng thái).
- **Form tạo container** chỉ có thông tin tĩnh: số container, loại, mã ISO, kích thước, hãng khai thác, chủ sở hữu (tự điền từ số container, chỉ đọc), phân loại, tình trạng, trọng lượng tối đa, trọng lượng vỏ, ngày sản xuất (MM/YYYY). **Tuyệt đối không có trường về lệnh giao.** Ghi chú: "Lệnh giao được gán sau, ở màn hình Lệnh giao hoặc chi tiết container."
- Kiểm tra form: số container đúng định dạng và số kiểm tra; không trùng; ISO dạng 2 số + 1 chữ + 1 số và khớp kích thước; trọng lượng vỏ nhỏ hơn trọng lượng tối đa; ngày sản xuất không ở tương lai. Mỗi lỗi hiện ngay dưới ô.

### 4.3 Lệnh giao
- Ba ô tóm tắt: **Lệnh còn hiệu lực**, **Sắp hết hạn**, và một ô thống kê thứ ba theo API. Bấm ô 1 và ô 2 lọc danh sách; có thanh "Đang lọc: … · N lệnh" và nút **Bỏ lọc**; bấm lại ô cũng bỏ lọc. Số trên các ô lấy từ API.
- Bảng: Số lệnh, Khách hàng (MST – tên), Hãng khai thác, Hạn lệnh, Chuyến tàu xuất, Số lượng theo loại, Đã giao / Tổng (thanh tiến độ), Trạng thái. Bấm một dòng mở chi tiết.
- **Chi tiết lệnh:** thông tin lệnh; bảng số lượng theo loại (Tổng, Đã giữ chỗ, Đã xuất, Còn lại); bảng **Container đã gán** (số container, loại · ISO, cỡ, vị trí, trạng thái, nút **Gỡ** khi đang giữ chỗ, có xác nhận); nút **Gán container**.
- **Gán container:** hộp thoại chọn nhiều, chỉ hiện container hợp lệ (đang trong bãi, cùng hãng, đúng loại và cỡ của một dòng còn chỗ, chưa gán lệnh khác); ô chọn bị khóa khi đã đủ số lượng của dòng; lệnh hết hạn hoặc giao đủ thì khóa nút.
- **Form thêm lệnh giao:** số lệnh (không trùng), hãng khai thác, khách hàng **chọn từ danh sách** (MST – tên), hạn lệnh (không ở quá khứ), chuyến tàu xuất, các dòng số lượng theo loại (loại container, cỡ, số lượng; nút Thêm dòng / Xóa dòng; không trùng cặp loại-cỡ).

### 4.4 Cổng nhập / xuất
- Ba tab: Nhập bãi, Xuất bãi, Lịch sử.
- Mỗi form có **panel Kiểm tra nghiệp vụ** chạy theo dữ liệu, mỗi mục ở trạng thái Chờ / Đạt / Lỗi kèm lý do và mã quy tắc; nút Ghi nhận chỉ bật khi tất cả Đạt. Kiểm tra gọi API validate (không tự tính trên giao diện), có chờ ngắn sau khi người dùng ngừng gõ.
- **Nhập bãi:** số container; loại phương tiện (Xe đầu kéo / Sà lan), biển số hoặc số hiệu, tài xế; phân loại; tình trạng; Block (gồm block ảo), Bay, Row, Tier (ẩn khi chọn block ảo); nút **Gợi ý vị trí trống**.
- **Xuất bãi:** **mục đầu tiên là chọn lệnh giao**; sau đó số container, phương tiện, tài xế, phân loại, tình trạng. Chọn lệnh hiện khách hàng, chuyến tàu, số lượng còn lại. Container đã giữ chỗ cho lệnh X thì tự chọn lệnh X kèm ghi chú "Container đã giữ chỗ cho lệnh X (khách: …)"; chọn lệnh khác báo lỗi.
- **Lịch sử:** thời gian, container, loại giao dịch, phương tiện, vị trí, lệnh giao, nhân viên, trạng thái. Hành động **Sửa** và **Hủy**.
  - **Sửa:** chỉ phương tiện, tài xế, phân loại, tình trạng. Không sửa số container hay loại giao dịch (ghi chú: sai thì hủy rồi tạo lại). Có bảng **Lịch sử thay đổi** (thời gian, người sửa, trường, giá trị cũ, giá trị mới).
  - **Hủy:** có xác nhận, chỉ giao dịch gần nhất của container. Giao dịch đã hủy hiện nhãn "Đã hủy" và không còn nút Sửa/Hủy.

### 4.5 Sơ đồ bãi
- **Tổng quan:** thẻ cho mỗi block (% lấp đầy, số container, số bay lẻ · row · tier) và thẻ **Block ảo** (số container, ghi chú không quản lý bay/row/tier).
- **Ô tìm container** (mới): gõ số container, nhảy đến đúng block, tier và ô, ô được tô nổi bật; nếu ở block ảo thì mở block ảo; nếu ngoài bãi thì báo rõ.
- **Chi tiết block:**
  - Tab **Tier** (1 đến tier tối đa của block).
  - Lưới theo bay lẻ ở cột, mỗi row một hàng; container 40ft chiếm hai cột và hiện bay chẵn; ô trống gộp lại; dải ghi chú quy tắc bay lẻ/chẵn.
  - Bốn chế độ màu kèm chú thích: **Kích thước** (20ft, 40ft, Reefer), **Loại cont** (7 loại), **Hãng khai thác** (mỗi hãng một màu cố định, lấy từ dữ liệu), **Theo đặt chỗ** (đã giữ chỗ, chưa gán, sắp hết hạn).
  - Bấm ô: khung chi tiết đủ trường kèm dòng **Lệnh giao** ("số lệnh · khách hàng" hoặc "Chưa gán lệnh") và cảnh báo nếu bị container khác đè.
  - **Mở rộng / thu hẹp block** bằng +/−: số bay lẻ (slot 20ft), số row, số tier tối đa. Thu hẹp bị chặn nếu còn container ngoài vùng mới, thông báo nêu container và vị trí.
- **Block ảo:** danh sách container (số, loại · ISO, kích thước, hãng, ghi chú), không có sơ đồ.
- Khung **Quy tắc xếp bãi** hiển thị cố định: bay lẻ 20ft, bay chẵn 40ft; tầng trên phải được đỡ hết; 40ft lên 2 con 20ft hoặc 1 con 40ft cùng vị trí; 20ft không lên 40ft.

### 4.6 Khách hàng và hãng khai thác
- Hai danh sách độc lập:
  - **Hãng khai thác:** tên, mã, liên hệ, trạng thái, số container đang lưu bãi.
  - **Khách hàng:** MST, tên, liên hệ, trạng thái, **Lệnh giao** ("N DO" và "x còn hiệu lực"; bấm mở danh sách lệnh đã lọc theo khách; vô hiệu hóa khi 0).
- **Form thêm / sửa khách hàng:** MST (10 chữ số, không trùng), tên, email, số điện thoại, địa chỉ.
- **Form thêm / sửa hãng khai thác** (mới): mã (3 chữ cái, không trùng), tên, liên hệ.
- Không xóa cứng: dùng **ngưng hoạt động**. Đối tác đã có lệnh giao thì không cho xóa.

### 4.7 Báo cáo (Mở rộng)
- Bộ lọc: loại báo cáo, từ ngày, đến ngày; nút Xem báo cáo.
- Ba ô số (tổng lượt nhập, tổng lượt xuất, thời gian lưu bãi trung bình) kèm xu hướng so với kỳ trước.
- Bảng sản lượng theo hãng khai thác (nhập, xuất, đang lưu bãi, tổng lượt).
- Nút **Xuất báo cáo** tải file Excel.

### 4.8 Đăng nhập (Mở rộng)
- Tên đăng nhập, mật khẩu, báo lỗi đăng nhập sai. Phiên hết hạn thì chuyển về đăng nhập và quay lại trang đang xem. Phân quyền chi tiết chưa nằm trong phạm vi.

## 5. Quy tắc nghiệp vụ hiển thị trên giao diện

Giao diện **báo sớm** nhưng không là nơi quyết định. Mọi quy tắc nằm ở backend (mã quy tắc xem `backend-requirements-from-ui.md`). Giao diện chỉ được tự kiểm tra những thứ không cần dữ liệu: định dạng ô nhập, trường bắt buộc, khớp ISO và kích thước.

## 6. Quy ước dữ liệu mẫu (khi chưa có API)

- Số container mẫu phải có số kiểm tra ISO 6346 hợp lệ.
- Mỗi container tối đa một đặt chỗ "Đang giữ chỗ". Số "Đã xuất" của dòng lệnh bằng số đặt chỗ "Đã xuất" của dòng đó. Container gán cho lệnh cùng hãng, đúng loại và cỡ.
- Lịch sử cổng phải khớp đặt chỗ: giao dịch xuất theo lệnh X thì container đó có đặt chỗ "Đã xuất" của lệnh X.
- Có container qua từ 2 lệnh của các khách khác nhau (ít nhất 3), có container chưa từng gán lệnh, có container đã xuất nhiều lần.
- Ngày mẫu tính theo ngày hiện tại, không gõ cứng.

## 7. Khác biệt so với bản xem trước (bắt buộc áp dụng)

| # | Chỗ | Bản xem trước | Yêu cầu |
|---|---|---|---|
| 1 | Lịch sử cổng và đặt chỗ | Giao dịch xuất MSCU 4821905 theo DO-2610-001 không khớp đặt chỗ "Đã xuất" | Dữ liệu và hiển thị phải khớp nhau (mục 6) |
| 2 | Tình trạng container | Giá trị tự do ("Móp nhẹ", "Cong nhẹ", "Trầy sơn") | Danh sách cố định: Bình thường, Móp, Méo, Cong, Hư hỏng khác |
| 3 | Số liệu | "1.248", số theo loại, số ở Tổng quan là số gõ cứng | Lấy từ API |
| 4 | Dữ liệu xuyên màn hình | Mỗi màn hình có dữ liệu riêng | Một nguồn dữ liệu từ API; sau thao tác ghi thì tải lại |
| 5 | Hộp thoại | Không đóng bằng Esc, thiếu thuộc tính trợ năng | Theo mục 1 |
| 6 | Trạng thái tải, lỗi, trống | Chưa có | Theo mục 1 |
| 7 | Tìm container trên sơ đồ bãi | Chưa có | Theo mục 4.5 |
| 8 | Form hãng khai thác, sửa đối tác | Chỉ có form thêm khách hàng | Theo mục 4.6 |
| 9 | Tab Tier ở sơ đồ bãi và chi tiết ô | Có | Giữ, vị trí hiển thị theo ô chọn, không còn giá trị cứng |
| 10 | Ô cảnh báo lệnh sắp hết hạn ở Tổng quan | Chưa có | Theo mục 4.1 |
| 11 | Trạng thái container | Nhập tay: MSKU 4821904 hiện "Trong bãi" dù đã có đặt chỗ; HLXF 8843619 hiện "Chờ xuất" dù chưa có đặt chỗ | Lấy `displayStatus` từ API (mục 2); dữ liệu mẫu phải theo quy tắc dẫn xuất |
| 12 | Ô "Hãng khai thác" ở form Cổng | Chỉ đọc, tự điền theo danh mục | Giữ; dữ liệu lấy từ phản hồi của `POST /gate/validate` |

## 8. Câu hỏi cần xác nhận với khách

- Mục **Khách hàng** và **Báo cáo** có nằm trong phạm vi bàn giao không?
- Có cần đăng nhập và phân quyền (ai sửa, hủy giao dịch, gán lệnh)?
- Depot có một hay nhiều (tên depot ở thanh trên cùng lấy từ đâu)?
- Cách tính "Tỷ lệ lấp đầy" và "Công suất bãi": theo TEU hay số container?
- Các mục trong tài liệu `backend-requirements-from-ui.md` mục 10 (số kiểm tra, hết hạn lệnh, xuất thẳng không giữ chỗ…).
