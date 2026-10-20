# Thiết kế demo Angular SNP Depot Management

Ngày: 20/10/2026. Trạng thái: chờ người dùng duyệt đặc tả viết.

## 1. Mục tiêu và quyết định đã thống nhất

Xây dựng ứng dụng Angular demo đầy đủ giao diện quản lý depot container vỏ rỗng bằng tiếng Việt. Người dùng vận hành có thể trải nghiệm các luồng liên thông với dữ liệu mẫu, đánh giá bố cục và thao tác trước khi nối backend .NET.

Người dùng đã chọn demo dữ liệu mẫu, yêu cầu đầy đủ màn hình, bổ sung Tồn bãi và chưa làm quản lý Người dùng. Giữ phong cách cyan/navy của ảnh tham chiếu; tối ưu tỷ lệ chữ, khoảng cách, form và bảng. Đăng nhập demo vẫn thuộc phạm vi.

Thành công khi các màn hình hoạt động cùng một nguồn dữ liệu, thay đổi ở một màn phản ánh ở các màn liên quan, bố cục cân đối trên laptop/tablet và dữ liệu được giữ sau khi tải lại trang.

Nguồn đối chiếu:

- `docs/design/ui-requirements.md`: yêu cầu chức năng và quy ước hiển thị.
- `docs/design/backend-requirements-from-ui.md`: thuật ngữ, mã quy tắc và quan hệ dữ liệu.
- `docs/design/App.approved.tsx`, `docs/design/figma-index.css` và ảnh người dùng gửi: tham chiếu hình thức, không sao chép số liệu gõ cứng hay lỗi nghiệp vụ của bản xem trước.
- Quyết định trong cuộc trao đổi và đặc tả này điều chỉnh nguồn dữ liệu sang demo local, bổ sung Tồn bãi và đưa Khách hàng/Báo cáo vào phạm vi ngay.

Đặc tả này chỉ dành cho demo; các lựa chọn nghiệp vụ tạm thời dưới đây không thay đổi hợp đồng backend hay xác nhận chính sách vận hành thật.

## 2. Phạm vi màn hình

| Màn hình | Chức năng bàn giao |
| --- | --- |
| Đăng nhập | Tài khoản mẫu công khai trên màn hình, báo nhập sai, đăng xuất, quay lại đường dẫn đang xem |
| Tổng quan | KPI, xu hướng, lưu lượng cổng 7 ngày, công suất block, cảnh báo lệnh, 5 hoạt động gần đây |
| Container | Tìm/lọc/sắp xếp/phân trang, tạo mới, chi tiết hai tab, lệnh hiện tại, gán lệnh và lịch sử |
| Lệnh giao | KPI và bộ lọc, danh sách, tạo lệnh nhiều dòng loại/cỡ, chi tiết, gán nhiều container và gỡ |
| Cổng nhập/xuất | Nhập, xuất, kiểm tra nghiệp vụ, gợi ý vị trí, lịch sử, chi tiết, sửa/hủy và lịch sử thay đổi |
| Sơ đồ bãi | Block/tier, bốn chế độ màu, tìm và chọn ô, di chuyển, block ảo, mở/thu hẹp block |
| Khách hàng | Danh sách khách hàng và hãng khai thác riêng theo tab, thêm/sửa/ngưng hoạt động, liên kết lệnh |
| Báo cáo | Khoảng ngày, loại báo cáo, KPI và xu hướng, sản lượng theo hãng, tải Excel |
| Tồn bãi | KPI lưu bãi, thống kê theo hãng, top 5 lưu lâu nhất, mở chi tiết container, tải Excel |

Không có màn quản lý Người dùng, phân quyền quản trị hay CRUD tài khoản. Không nối API, triển khai dịch vụ máy chủ, thiết kế 3D, tạo dự án Figma hoặc thay đổi backend trong phạm vi demo này. Figma không phải đầu ra bắt buộc.

## 3. Hệ thống hình thức

### 3.1 Chữ và màu

- Dùng Manrope với phông dự phòng hỗ trợ tiếng Việt; ưu tiên đóng gói phông cùng frontend để không phụ thuộc Google Fonts lúc chạy.
- Nội dung và dữ liệu 14–16px, nhãn 14px/medium, hướng dẫn 12–13px, tiêu đề nhóm 16–18px, tiêu đề trang 26–28px. Dòng chữ đủ cao để không cắt dấu tiếng Việt.
- Navy cho chữ chính và trạng thái tab chọn; cyan/teal cho hành động chính. Nền xám nhạt, thẻ trắng, viền mảnh, bóng nhẹ.
- Chữ phụ đủ tương phản; nhãn không nhạt đến mức khó đọc. Trạng thái có chữ và biểu tượng phù hợp, không chỉ dựa vào màu.
- Số container và vị trí dùng phông monospace khi giúp đọc mã. Số liệu dùng chữ số có độ rộng bằng nhau; số và ngày trong bảng không xuống dòng.

### 3.2 Khung và khoảng cách

- Sidebar desktop khoảng 240–256px, topbar khoảng 64px; phần nội dung có padding 24–32px, tránh khoảng đầu trang quá cao như ảnh mẫu.
- Khoảng cách theo nhịp 4/8px. Nhãn cách ô 6–8px; trường cách nhau 16–20px; nhóm cách nhau 24px.
- Input và nút cao 40–44px trên desktop, tối thiểu 44px trên tablet. Icon 16–20px, căn giữa theo chiều dọc.
- Form hai cột khi đủ chỗ; tablet hẹp chuyển một cột. Panel kiểm tra cổng chiếm khoảng 1/3, form khoảng 2/3; panel bám theo vùng nhìn khi chiều cao cho phép, chuyển xuống dưới trên màn hẹp.
- Block một hàng; Bay/Row/Tier cùng hàng khi đủ rộng. Trường tình trạng luôn là danh sách chọn cố định.
- Bảng cuộn ngang bên trong khung. Tên khách và MST tách hai dòng; số lệnh/container không bị bẻ giữa mã. Header bảng và phân trang thể hiện đúng tổng lọc.
- Hộp thoại giới hạn chiều cao theo viewport, cuộn nội dung riêng, giữ tiêu đề và footer hành động. Form container khoảng 880px, lệnh giao khoảng 1040px tối đa và luôn chừa lề màn hình.
- Nhãn “Hạn lệnh” ngắn gọn, giải thích “Ngày cuối được giao” ở helper text. Form container chia Nhận dạng, Đặc tính, Thông số kỹ thuật. Chi tiết container dùng nhóm thông tin gọn thay cho nhiều thẻ cao.

### 3.3 Trợ năng và responsive

- Mọi input có label; lỗi liên kết với input. Có focus rõ, thao tác bàn phím, focus trap trong dialog, Esc đóng và trả focus về nơi mở.
- Xác nhận trước gỡ booking, hủy giao dịch, ngưng hoạt động và khôi phục dữ liệu mẫu. Nếu form có thay đổi chưa lưu, hỏi trước khi đóng/bỏ nội dung.
- Kiểm tra ở 1440×900, 1366×768, 1024×768 và 768×1024; zoom 125% không làm mất nút hoặc cắt chữ. Sidebar thu thành drawer trên tablet hẹp.

## 4. Kiến trúc frontend và luồng dữ liệu

Ứng dụng độc lập tại `frontend/`, không tái cấu trúc backend. Dùng Angular standalone, Router với lazy routes theo nhóm chức năng, Reactive Forms và Signals cho trạng thái hiển thị. Phiên bản Angular cụ thể chọn lúc lập kế hoạch sau khi kiểm tra môi trường và phiên bản ổn định tương thích.

Các ranh giới:

- `core`: phiên demo, đồng hồ depot, định dạng, hợp đồng dữ liệu, lưu trữ, xử lý lỗi và lớp truy cập dữ liệu.
- `shared`: trường form, nút, badge, bảng/phân trang, dialog, xác nhận, toast, skeleton, trạng thái lỗi/trống.
- `features`: dashboard, containers, delivery-orders, gate, yard, partners, reports, inventory và login. Mỗi feature giữ trang, form và model hiển thị riêng.
- `demo`: seed, repository dùng chung, truy vấn, xử lý command và bộ quy tắc mô phỏng. Component không tự ghi localStorage hoặc thay đổi thực thể của feature khác.

Luồng đọc: component → service có kiểu rõ → repository demo → projection/view model. Luồng ghi: form → command service → kiểm tra → tạo snapshot mới nguyên tử → lưu → phát thay đổi → các projection liên quan tính lại. Lỗi không để lại trạng thái đã ghi một phần.

Hợp đồng service phân tách truy vấn và thao tác ghi, trả lỗi có `code`, `message`, lỗi trường và mã quy tắc. Khi nối API sau này có thể thay adapter; chưa cam kết không phải sửa gì vì DTO backend cần đối chiếu riêng.

URL thể hiện trang/tab, tìm kiếm và bộ lọc chính; liên kết xuyên màn hình chọn đúng container/lệnh/block/tier. Các giá trị query không hợp lệ trở về mặc định an toàn.

## 5. Dữ liệu mẫu, thời gian và lưu trữ

- Một kho chứa container, đối tác, lệnh/dòng lệnh, booking, lifecycle, vị trí, block, giao dịch cổng và audit. Trạng thái hiển thị/KPI là dữ liệu tính ra, không duy trì bộ số riêng từng màn.
- Seed dùng số container ISO 6346 hợp lệ; bao phủ 7 loại, hai cỡ, nhiều hãng, các tình trạng/grade, container mới/chờ nhập/đang lưu/đã xuất và block ảo.
- Có ít nhất 3 container từng đi qua nhiều lệnh của các khách khác nhau; có container chưa gán, xuất nhiều lần, lệnh hết hạn/sắp hết hạn/giao đủ, giao dịch đã hủy và audit sửa. Mọi lịch sử phải khớp lifecycle và booking.
- Seed theo thời điểm khởi tạo/khôi phục, không theo ngày trong ảnh. Dùng đồng hồ demo múi giờ `Asia/Bangkok` (UTC+7), ngày hết hạn có hiệu lực đến hết ngày depot; “sắp hết hạn” gồm hôm nay đến 3 ngày tới, chưa giao đủ.
- Lưu snapshot có phiên bản schema trong localStorage sau thao tác. Tải lại giữ dữ liệu, bộ lọc ở URL và phiên demo trong tab. Khi qua ngày, tính lại tuổi lưu bãi/trạng thái hết hạn từ đồng hồ hiện tại.
- Nếu lưu trữ hỏng/khác schema: hiện thông báo và hành động khôi phục có xác nhận. Nếu trình duyệt từ chối lưu hoặc hết dung lượng: giữ dữ liệu trong bộ nhớ và thông báo rõ thay đổi có thể mất khi tải lại.
- Nút “Khôi phục dữ liệu mẫu” thay snapshot sau xác nhận và cập nhật mọi màn. Có nhãn “Dữ liệu demo” ở khung ứng dụng.
- Phiên demo lưu trong sessionStorage, không lưu mật khẩu. Đây là điều hướng mô phỏng, không cung cấp bảo mật; người dùng mẫu và vai trò cố định, không có phân quyền chi tiết. Nếu mất phiên, chuyển login và quay lại đường dẫn sau đăng nhập.
- Đồng bộ giữa nhiều tab và dữ liệu nhiều người dùng không thuộc phạm vi; mỗi tab giữ snapshot đang làm việc và tải lại để nhận snapshot đã lưu mới nhất.

## 6. Hành vi màn hình và quy tắc demo

### 6.1 Tổng quan và container

KPI, biểu đồ, cảnh báo và hoạt động đọc từ cùng repository. Biểu đồ 7 ngày có chú thích và giá trị khi hover/focus. Công suất thật tính theo TEU: 20ft chiếm 1, 40ft chiếm 2; sức chứa bằng số slot 20ft × row × tier của block thật. Block ảo tính vào số container trong bãi nhưng không vào sức chứa hay tỷ lệ lấp đầy bãi thật. Trên 80% có cảnh báo.

Container có thẻ loại và số lượng, lọc gán lệnh/hãng/tình trạng, tìm số/ISO, sắp xếp và phân trang. Thẻ số theo loại áp dụng các bộ lọc khác nhưng không tự giới hạn bởi loại đang chọn. Đổi bộ lọc trở về trang đầu.

Form tạo chỉ có thuộc tính tĩnh, không có trường lệnh giao; chủ sở hữu lấy từ prefix số container, kiểm tra ISO/cỡ, kiểm tra số, trùng, trọng lượng và tháng sản xuất. Tạo xong là Chờ nhập. Tab lệnh hiển thị hiện tại, lịch sử và các lệnh đủ điều kiện để gán.

### 6.2 Lệnh giao và booking

Ba ô: lệnh còn hiệu lực chưa giao đủ, lệnh sắp hết hạn, container đã giao hôm nay. Hai ô đầu bật/tắt lọc. Tạo lệnh với khách/hãng đang hoạt động, hạn không quá khứ, số lệnh duy nhất, số lượng nguyên dương và cặp loại/cỡ không trùng.

Container được gán khi trong bãi, cùng hãng, đúng loại/cỡ còn chỗ và chưa có booking hiệu lực. Gán nhiều thực hiện toàn bộ hoặc không gán gì; khóa lựa chọn khi đủ số lượng. Gỡ chỉ với booking đang giữ chỗ, giữ lịch sử.

Tổng dòng lệnh = còn lại + đang giữ chỗ + đã xuất. Mỗi container tối đa một booking hiệu lực. Lệnh ưu tiên trạng thái Đã giao đủ → Hết hạn → Sắp hết hạn → Còn hạn. Khi lệnh hết hạn, booking giữ chỗ thành Hết hạn và giải phóng container trong lớp demo trước truy vấn/ghi tiếp theo.

### 6.3 Cổng

Panel kiểm tra có Chờ/Đạt/Lỗi và lý do/mã quy tắc; gọi service demo bất đồng bộ sau khoảng dừng nhập 300ms, bỏ phản hồi cũ. Chỉ bật Ghi nhận khi mọi kiểm tra đạt, đủ trường bắt buộc và không đang gửi. Khi ghi phải kiểm tra lại snapshot hiện tại.

Nhập bãi xác nhận container tồn tại và chưa ở bãi, phương tiện/tài xế, grade/condition, vị trí hợp lệ hoặc block ảo. Hãng là thông tin chỉ đọc. Gợi ý tìm vị trí đáp ứng toàn bộ quy tắc xếp bãi.

Xuất chọn lệnh trước, hiện khách/chuyến/số còn lại; container có booking tự nhận lệnh đó, chọn khác báo lỗi. Lựa chọn demo: cho xuất trực tiếp container chưa giữ chỗ nếu lệnh hợp lệ, cùng hãng, đúng loại/cỡ còn chỗ; tạo booking Đã xuất trong cùng thao tác. Đây là lựa chọn mô phỏng G5, không xác nhận cấu hình production. Tình trạng hư hỏng hiển thị cảnh báo, không tự thêm quy tắc cấm xuất ngoài tài liệu.

Lịch sử tìm/lọc/phân trang, bấm mã mở chi tiết; sửa chỉ phương tiện, tài xế, grade và condition, ghi giá trị cũ/mới/người/thời điểm. Sửa giao dịch cũ chỉ sửa bản ghi đó; thuộc tính hiện tại của container tính từ các sự kiện chưa hủy theo thời gian.

Hủy chỉ giao dịch chưa hủy gần nhất của container. Hủy nhập phục hồi lifecycle trước và bỏ vị trí; chặn nếu có booking đang giữ chỗ hoặc di chuyển sau giao dịch nhập, yêu cầu gỡ booking/xử lý chuyển bãi trước. Hủy xuất phục hồi vị trí và booking trước đó; vị trí phải còn hợp lệ, booking thành Hết hạn nếu lệnh đã hết hạn. Hủy thất bại không ghi dở trạng thái. Giao dịch đã hủy giữ lịch sử, không sửa/hủy lại.

### 6.4 Bãi

Block thật hiển thị bay lẻ 20ft, row, tier; 40ft nằm bay chẵn và chiếm hai slot liền nhau theo công thức trong tài liệu backend. Bốn chế độ màu và chú thích: kích thước, loại, hãng, booking. Chọn ô hiện thông tin container/lệnh/vị trí; tìm kiếm nhảy đúng block/tier/ô hoặc thông báo ngoài bãi/không tìm thấy.

Di chuyển bằng chọn container và vị trí đích qua dialog, không yêu cầu kéo thả. Chặn container bị đè, đích bị chiếm, ngoài giới hạn, thiếu đỡ hoặc xếp 20ft lên 40ft. 40ft có thể được đỡ bởi một 40ft cùng vị trí hoặc hai 20ft lấp đủ bên dưới. Áp dụng cho nhập, di chuyển, gợi ý và hoàn tác xuất.

Block ảo dùng danh sách và cho đặt sang block thật; vị trí không có bay/row/tier. Mở/thu hẹp điều chỉnh số bay lẻ, row, tier bằng số nguyên dương; thu hẹp nêu container/vị trí chặn. Di chuyển có nhật ký và cập nhật vị trí đồng bộ mọi màn; không đổi ngày nhập lifecycle.

### 6.5 Đối tác

Khách hàng và hãng có thêm/sửa, tìm kiếm, phân trang và ngưng hoạt động; giữ quan hệ lịch sử. Không cho ngưng khi còn lệnh chưa hết hạn/chưa giao đủ; hãng còn container trong bãi cũng bị chặn. Các form mới chỉ chọn đối tác đang hoạt động.

MST 10 chữ số duy nhất, mã hãng 3 chữ cái duy nhất, tên bắt buộc, email hợp lệ khi có. Sửa khóa định danh của đối tác đã có tham chiếu: MST khách/mã hãng chỉ đọc, vẫn sửa tên và thông tin liên hệ. Lệnh trỏ ID, không sao chép tên làm nguồn dữ liệu.

### 6.6 Báo cáo và tồn bãi

Báo cáo có lựa chọn Tổng hợp nhập/xuất và Sản lượng theo hãng; đều áp dụng từ/đến ngày (bao gồm hai đầu theo UTC+7), chặn ngày đảo. KPI nhập/xuất và sản lượng chỉ tính giao dịch chưa hủy trong khoảng. “Đang lưu” tính hiện tại và ghi nhãn rõ. Tổng lượt = nhập + xuất. Xu hướng so với khoảng ngay trước có cùng số ngày.

Thời gian lưu trung bình báo cáo tính các lượt xuất trong kỳ, lấy thời điểm xuất trừ nhập của lifecycle tương ứng. Tồn bãi tính các lifecycle đang hoạt động đến thời điểm xem, không dùng lịch sử đã xuất. Ngày lưu = số mili giây / 24 giờ; trung bình hiển thị 1 chữ số thập phân; “từ 3 ngày” nghĩa là >= 72 giờ. Thời gian dài nhất và top 5 hiển thị số ngày tròn xuống; hòa thì xếp theo ngày nhập cũ trước rồi số container.

Tồn bãi gồm 3 KPI, bảng theo hãng (số đang lưu, trung bình, lâu nhất, từ 3 ngày), top 5 kèm số container/hãng/vị trí/ngày nhập/số ngày và mở chi tiết. Block ảo nằm trong số tồn. Có lọc hãng, loại và kích thước; mọi bảng/KPI/export áp dụng cùng bộ lọc.

Excel là file `.xlsx` thật, chứa tiêu đề, thời điểm xuất, điều kiện lọc, nhãn tiếng Việt và giá trị số/ngày đúng kiểu. Báo cáo và Tồn bãi xuất toàn bộ tập dữ liệu lọc, không chỉ trang đang thấy. Trường hợp không có dữ liệu hiện trạng thái trống và khóa nút xuất.

Xu hướng với kỳ trước bằng 0: nếu cả hai bằng 0 hiển thị 0%; nếu hiện tại khác 0 hiển thị “Chưa có cơ sở so sánh”, không chia cho 0. Trung bình tập rỗng hiển thị “—”.

## 7. Loading, lỗi và trình diễn

- Mọi trang dữ liệu có skeleton, lỗi kèm Thử lại, trạng thái trống có giải thích và dữ liệu bình thường. Nút đang gửi có trạng thái bận, chặn nhấn lặp.
- Lỗi form ở dưới ô; lỗi nghiệp vụ ở panel/banner kèm mã đã quy định, lỗi hành động không làm mất dữ liệu đang nhập. Thành công hiện toast ngắn.
- Adapter demo có độ trễ ngắn cấu hình được; một khu vực “Công cụ demo” thu gọn cho phép mô phỏng lỗi lần đọc tiếp theo để kiểm tra Thử lại. Không làm lỗi ngẫu nhiên trong sử dụng thường.
- Không hiển thị “Hệ thống ổn định” như kết quả theo dõi backend thật; nhãn trạng thái nêu “Chế độ demo”. Không có link/nút trông hoạt động nhưng không làm gì.

## 8. Kiểm thử và nghiệm thu

Kiểm tra tự động tập trung ở quy tắc và luồng có rủi ro, không viết test chỉ lặp lại markup:

1. Seed nhất quán: số ISO hợp lệ, vị trí không trùng, sức đỡ hợp lệ, booking/giao dịch/lifecycle khớp.
2. Tạo container → nhập thật/ảo → gán lệnh → di chuyển hợp lệ → xuất; mọi màn/KPI/báo cáo/tồn bãi cập nhật.
3. Gán nhiều toàn bộ hoặc không gì, chặn trùng/khác hãng/sai loại/cỡ/quá số lượng/lệnh hết hạn; gỡ giữ lịch sử.
4. Xuất bị chặn khi bị đè hoặc sai booking; hủy xuất phục hồi đồng bộ, chặn khi vị trí cũ không còn hợp lệ; hủy nhập có ràng buộc ở mục 6.3.
5. Xếp 20ft/40ft, block ảo, tìm đúng tier, chuyển vị trí, mở/thu hẹp có kiểm tra.
6. Lưu/tải lại/reset, schema hỏng, localStorage không ghi được và ngày depot đổi.
7. KPI và thời gian lưu tính đúng, không chia cho 0; Excel mở được và khớp dữ liệu lọc.
8. Điều hướng xuyên màn hình, bộ lọc/phân trang, đăng nhập sai/đăng xuất/quay lại trang, loading/lỗi/thử lại/trống.

Chạy build production, test logic nghiệp vụ và kiểm tra trình duyệt các luồng chính. Kiểm tra trực quan form dài, dialog, bảng rộng, focus và viewport ở mục 3.3. Bàn giao lệnh chạy, tài khoản demo, cách reset, danh sách màn hình và giới hạn mô phỏng trong README frontend. Không coi demo là bằng chứng backend hoặc nghiệp vụ production đã được nghiệm thu.

## 9. Bước tiếp theo

Người dùng duyệt file đặc tả này trước. Sau khi duyệt, viết kế hoạch triển khai có đường dẫn file, thứ tự công việc và xác minh, rồi người dùng chọn cách thực hiện kế hoạch. Chưa viết mã sản phẩm hoặc cài dependency ở bước đặc tả.
