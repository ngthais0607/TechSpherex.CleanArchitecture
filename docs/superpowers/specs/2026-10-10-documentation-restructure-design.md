# Documentation Restructure Design

## 1. Muc tieu

Tai cau truc tai lieu Tuan 1 va Tuan 2 thanh bo tai lieu ngan gon, de tra cuu va bam sat code hien tai. Cac bao cao theo ngay chi ghi lai cong viec, yeu cau va cach giai quyet; noi dung giai thich ky thuat duoc gom vao mot tai lieu tham chieu rieng cho moi tuan.

## 2. Cau truc dich

```text
docs/
|-- week-01/
|   |-- README.md
|   |-- day-01.md ... day-07.md
|   `-- codebase-structure-and-files.md
`-- week-02/
    |-- README.md
    |-- day-01.md ... day-07.md
    `-- codebase-structure-and-files.md
```

`README.md` o thu muc goc du an chi lien ket den hai README theo tuan. Tat ca lien ket trong tai lieu dung duong dan Markdown tuong doi, khong dung `file:///` hay duong dan tuyet doi theo may.

## 3. Trach nhiem tung loai tai lieu

### README theo tuan

- Gioi thieu pham vi cua tuan.
- Bang muc luc `day-01` den `day-07`.
- Lien ket den `codebase-structure-and-files.md` cua tuan.
- Tom tat ket qua da xac minh, khong lap lai noi dung giai thich chi tiet.

### Bao cao theo ngay

Moi file `day-01.md` den `day-07.md` chi co cac phan:

1. **Viec da thuc hien**: liet ke artifact va hanh vi da hoan thanh trong ngay.
2. **Yeu cau can giai quyet**: neu bai toan nghiep vu hoac ky thuat, khong dua them yeu cau chua co trong code/PDF.
3. **Phuong phap va ket qua**: mo ta cach giai quyet, quy tac chinh, ket qua test tai thoi diem do va gioi han con lai.

Cac file ngay khong chua phan giang giai cu phap, cay thu muc dai, ma test day du, ERD/DDL, hay noi dung thiet ke cua tuan sau.

### Tai lieu tham chieu theo tuan

Moi `codebase-structure-and-files.md` gom cac noi dung giai thich truoc day bi phan tan:

- So do thu muc va trach nhiem cua tung project, folder va nhom file.
- Bang cac entity, value object, enum, rule, handler, validator va test suite thuoc pham vi tuan.
- Dinh nghia cac cau truc C# dang chu y: access modifier, `sealed`, `record`, enum, factory method, `private set`, backing field, `IReadOnlyCollection`, generic variance, reflection, CQRS, `Result`, `TimeProvider`.
- Luong nghiep vu va cac quy tac chinh.
- Chien luoc test, pham vi ma test chung minh va cac gioi han cua EF Core InMemory.
- Tuan 1 chua phan thiet ke du lieu da dong bo voi entity hien tai; day la mo hinh de xuat cho Infrastructure, khong duoc mo ta la schema da trien khai.
- Tuan 2 chua phan Application, DI, CQRS, validation, orchestration va ranh gioi persistence.

## 4. Noi dung can hieu chinh

- Dung so lieu hien tai: 29 handlers, 17 validators, 120 Domain tests, 131 Application tests va 7 Architecture tests.
- Doi ten `DepotEndToEndWorkflowTests` trong tai lieu thanh application workflow test; khong tuyen bo day la E2E qua API/CSDL that.
- Mo ta mot lan `SaveChangesAsync` la mot ranh gioi luu thay doi. ACID/rollback chi duoc khang dinh sau relational integration test.
- Bo tuyen bo `0 Warning` trong khi project van co canh bao nullable/xUnit analyzer.
- Dong bo constructor `GateOutCommandHandler` voi `IAppDbContext`, `IRuleEngine`, `TimeProvider`.
- Ghi ro `FakeTimeProvider` can package `Microsoft.Extensions.TimeProvider.Testing`, hoac dung fixed `TimeProvider` noi bo nhu test hien tai.
- Dong bo database design voi entity hien tai, dac biet `DeliveryOrder` va `ContainerLifecycle`.
- Giu `Condition` va `Classification` doc lap; khong mo ta hu hong tu dong ha Grade C.
- Giu logic ISO 6346 hien tai.
- Han che cac cum tu tuyet doi nhu `100%`, `hoan toan`, `tuyet doi` neu khong co test hoac bang chung truc tiep.

## 5. Hop nhat va xoa file

Sau khi noi dung da duoc chuyen va doi chieu, xoa:

- `docs/codebase-structure-and-files.md`
- `docs/week-01/database-design.md`
- `docs/week-01/tests-explanation.md`
- `docs/week-01/week-01-logic-and-solutions.md`
- `docs/week-02/tests-explanation.md`
- `docs/week-02/week-02-logic-and-solutions.md`
- Tai lieu ke hoach tam trong `docs/superpowers/` sau khi hoan tat cong viec.

Khong xoa thong tin duy nhat cho den khi da chuyen sang file tham chieu tuong ung.

## 6. Tieu chi hoan thanh

- Moi thu muc tuan chi con README, bay file ngay va mot file tham chieu.
- Moi file ngay tuan theo dung khuon ba phan.
- Khong con lien ket `file:///` hoac duong dan `D:/...` trong Markdown.
- Khong con chu ky ham, enum, so handler/validator hay mo hinh database da loi thoi.
- Khong con tuyen bo ACID, E2E hoac zero-warning vuot qua bang chung hien co.
- Cac lien ket Markdown noi bo deu tro den file ton tai.
- `dotnet test SnpDepotManagement.sln` van dat 258/258 test sau khi chi thay doi tai lieu.
