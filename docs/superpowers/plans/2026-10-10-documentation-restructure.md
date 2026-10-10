# Documentation Restructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tái cấu trúc tài liệu Tuần 1 và Tuần 2 thành nhật ký ngày ngắn gọn cùng một tài liệu tham chiếu kỹ thuật chính xác cho mỗi tuần.

**Architecture:** Mỗi thư mục tuần giữ một README, bảy báo cáo ngày và một `codebase-structure-and-files.md`. Nội dung giải thích kỹ thuật, cấu trúc source, database và kiểm thử được hợp nhất vào tài liệu tham chiếu; báo cáo ngày chỉ giữ việc đã làm, yêu cầu và phương pháp/kết quả.

**Tech Stack:** Markdown, PowerShell validation, .NET 10 test suite.

**Spec:** `docs/superpowers/specs/2026-10-10-documentation-restructure-design.md`

## Global Constraints

- Dùng liên kết Markdown tương đối; không còn `file:///` hoặc đường dẫn máy `D:/...`.
- Không tạo `business-decisions.md`.
- Không thay đổi mã nguồn nghiệp vụ hoặc logic ISO 6346.
- Số liệu hiện tại là 29 handlers, 17 validators, 120 Domain tests, 131 Application tests và 7 Architecture tests.
- Không gọi workflow test dùng `TestAppDbContext` là E2E thực sự.
- Không tuyên bố ACID/rollback đã được chứng minh trước relational integration test.
- Không tuyên bố build `0 Warning` khi warning analyzer vẫn tồn tại.
- `Condition` và `Classification` là hai dữ liệu độc lập.

## Review Focus

- Link tương đối từ README gốc, README tuần và tài liệu tham chiếu phải trỏ tới file tồn tại.
- Nội dung duy nhất từ các file bị xóa phải được giữ lại trong tài liệu tham chiếu tương ứng.
- Chữ ký và enum minh họa phải khớp mã nguồn hiện tại.
- Số liệu lịch sử theo ngày phải được ghi rõ là kết quả tại thời điểm đó, không gây nhầm với số liệu hiện tại.
- Tài liệu database phải được gắn nhãn thiết kế đề xuất và khớp mô hình Domain hiện tại.

---

### Task 1: Tài liệu tham chiếu Tuần 1

**Files:**
- Create: `docs/week-01/codebase-structure-and-files.md`
- Read: `docs/week-01/database-design.md`
- Read: `docs/week-01/tests-explanation.md`
- Read: `docs/week-01/week-01-logic-and-solutions.md`
- Read: `src/Domain/**`
- Read: `tests/Domain.UnitTests/**`

**Interfaces:**
- Consumes: Domain entities, enums, value objects, rules và test hiện tại.
- Produces: Một tài liệu tham chiếu Tuần 1 để README và các báo cáo ngày liên kết tới.

- [ ] **Step 1: Chạy kiểm tra tiền điều kiện**

Run: `Test-Path docs/week-01/codebase-structure-and-files.md`
Expected: `False`.

- [ ] **Step 2: Viết tài liệu tham chiếu Tuần 1**

Nội dung bắt buộc: sơ đồ `src/Domain`, nhiệm vụ từng nhóm file, entity/value object/enum, tám business rules, giải thích cú pháp C# đặc biệt, mô hình dữ liệu đề xuất đã đồng bộ, chiến lược test và giới hạn kiểm thử.

- [ ] **Step 3: Kiểm tra nội dung Tuần 1**

Run: `rg -n "sealed|record|enum|Factory|private set|IReadOnlyCollection|120|thiết kế đề xuất" docs/week-01/codebase-structure-and-files.md`
Expected: Có kết quả cho mọi chủ đề bắt buộc và không có mô tả tự động hạ Grade C.

- [ ] **Step 4: Commit Task 1**

```bash
git add docs/week-01/codebase-structure-and-files.md
git commit -m "docs: consolidate week 1 technical reference"
```

### Task 2: Tài liệu tham chiếu Tuần 2

**Files:**
- Create: `docs/week-02/codebase-structure-and-files.md`
- Read: `docs/week-02/tests-explanation.md`
- Read: `docs/week-02/week-02-logic-and-solutions.md`
- Read: `src/Application/**`
- Read: `tests/Application.UnitTests/**`
- Read: `tests/Architecture.Tests/ApplicationArchitectureTests.cs`

**Interfaces:**
- Consumes: Tài liệu Tuần 1 cho thuật ngữ Domain và source Application hiện tại.
- Produces: Một tài liệu tham chiếu Tuần 2 để README và các báo cáo ngày liên kết tới.

- [ ] **Step 1: Chạy kiểm tra tiền điều kiện**

Run: `Test-Path docs/week-02/codebase-structure-and-files.md`
Expected: `False`.

- [ ] **Step 2: Viết tài liệu tham chiếu Tuần 2**

Nội dung bắt buộc: sơ đồ `src/Application`, trách nhiệm abstraction/common/features, CQRS in-house, validator, DI reflection, `TimeProvider`, 29 handlers, 17 validators, workflow test, test matrix và giới hạn InMemory/transaction.

- [ ] **Step 3: Kiểm tra nội dung Tuần 2**

Run: `rg -n "29|17|TimeProvider|IRuleEngine|reflection|InMemory|workflow|131|7" docs/week-02/codebase-structure-and-files.md`
Expected: Có kết quả cho mọi chủ đề bắt buộc; không gọi workflow test là E2E thực sự.

- [ ] **Step 4: Commit Task 2**

```bash
git add docs/week-02/codebase-structure-and-files.md
git commit -m "docs: consolidate week 2 technical reference"
```

### Task 3: Chuẩn hóa 14 báo cáo ngày

**Files:**
- Modify: `docs/week-01/day-01.md` đến `docs/week-01/day-07.md`
- Modify: `docs/week-02/day-01.md` đến `docs/week-02/day-07.md`

**Interfaces:**
- Consumes: Hai tài liệu tham chiếu từ Task 1 và Task 2.
- Produces: Mười bốn nhật ký ngày cùng một cấu trúc ba phần.

- [ ] **Step 1: Ghi nhận kiểm tra đang thất bại**

Run: PowerShell kiểm tra mỗi file có đúng các heading `## 1. Việc Đã Thực Hiện`, `## 2. Yêu Cầu Cần Giải Quyết`, `## 3. Phương Pháp Và Kết Quả`.
Expected: FAIL trên cấu trúc tài liệu cũ.

- [ ] **Step 2: Viết lại các báo cáo Tuần 1**

Giữ nội dung theo thời điểm từng ngày nhưng bỏ cây thư mục dài, syntax deep-dive, test source đầy đủ, ERD/DDL và thiết kế Tuần 2. Kết quả test lịch sử phải ghi rõ “tại thời điểm kết thúc ngày”.

- [ ] **Step 3: Viết lại các báo cáo Tuần 2**

Giữ artifact, yêu cầu, cách điều phối và kết quả theo ngày; chuyển phần giải thích CQRS, reflection, variance, record, TimeProvider và test internals sang tài liệu tham chiếu.

- [ ] **Step 4: Chạy kiểm tra cấu trúc**

Run: Cùng lệnh PowerShell ở Step 1.
Expected: PASS đủ `14/14` file.

- [ ] **Step 5: Commit Task 3**

```bash
git add docs/week-01/day-*.md docs/week-02/day-*.md
git commit -m "docs: simplify weekly progress reports"
```

### Task 4: Chuẩn hóa README và liên kết

**Files:**
- Modify: `README.md`
- Modify: `docs/week-01/README.md`
- Modify: `docs/week-02/README.md`

**Interfaces:**
- Consumes: Cấu trúc tài liệu hoàn chỉnh từ Task 1-3.
- Produces: Điểm vào tài liệu bằng link tương đối và số liệu hiện tại.

- [ ] **Step 1: Xác nhận link tuyệt đối còn tồn tại**

Run: `rg -n "file:///|D:/" README.md docs/week-01/README.md docs/week-02/README.md`
Expected: Có kết quả trước khi chỉnh sửa.

- [ ] **Step 2: Viết lại ba README**

README gốc liên kết tới hai tuần; README tuần chứa mục lục bảy ngày, liên kết tài liệu tham chiếu và tóm tắt kết quả. Dùng link tương đối từ vị trí từng file.

- [ ] **Step 3: Kiểm tra link tuyệt đối đã hết**

Run: `rg -n "file:///|D:/" README.md docs/week-01/README.md docs/week-02/README.md`
Expected: Không có kết quả.

- [ ] **Step 4: Commit Task 4**

```bash
git add README.md docs/week-01/README.md docs/week-02/README.md
git commit -m "docs: use portable documentation links"
```

### Task 5: Xóa tài liệu dư thừa và xác minh

**Files:**
- Delete: `docs/codebase-structure-and-files.md`
- Delete: `docs/week-01/database-design.md`
- Delete: `docs/week-01/tests-explanation.md`
- Delete: `docs/week-01/week-01-logic-and-solutions.md`
- Delete: `docs/week-02/tests-explanation.md`
- Delete: `docs/week-02/week-02-logic-and-solutions.md`

**Interfaces:**
- Consumes: Toàn bộ cấu trúc tài liệu mới.
- Produces: Cây tài liệu cuối cùng chỉ chứa các file thuộc Phương án A.

- [ ] **Step 1: Kiểm tra file đích đã chứa các chủ đề cần giữ**

Run: Các lệnh `rg` của Task 1 và Task 2 cùng kiểm tra phần database, test và syntax.
Expected: PASS trước khi xóa nguồn cũ.

- [ ] **Step 2: Xóa các file đã hợp nhất**

Chỉ xóa đúng danh sách trên; không xóa thay đổi mã nguồn hoặc tài liệu ngoài phạm vi.

- [ ] **Step 3: Kiểm tra cây tài liệu**

Run: PowerShell xác nhận mỗi thư mục tuần có đúng `README.md`, bảy `day-*.md` và `codebase-structure-and-files.md`.
Expected: PASS cho cả hai tuần.

- [ ] **Step 4: Kiểm tra nội dung và link toàn cục**

Run: quét toàn bộ Markdown để phát hiện `file:///`, `D:/`, `23 Handlers`, `15 Validators`, `0 Warning`, mô tả hạ Grade C, constructor GateOut cũ và link tới file không tồn tại.
Expected: Không có lỗi.

- [ ] **Step 5: Chạy kiểm thử solution**

Run: `dotnet test SnpDepotManagement.sln --no-restore --nologo --verbosity minimal -m:1 /nodeReuse:false`
Expected: 120 Domain, 131 Application và 7 Architecture tests passed.

- [ ] **Step 6: Commit Task 5**

```bash
git add -A docs README.md
git commit -m "docs: finalize weekly documentation structure"
```
## Final Cleanup

Sau khi toàn bộ task, kiểm thử và final review hoàn tất, xóa `docs/superpowers/` rồi tạo commit dọn tài liệu quy trình. Việc này diễn ra sau khi executor không còn cần đọc plan/spec.
