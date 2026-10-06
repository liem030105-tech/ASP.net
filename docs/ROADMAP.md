# Lộ trình rèn luyện: LeaveFlow (Fullstack .NET / Angular)

Lộ trình này bám theo tài liệu *Interview Prep .NET / Angular* (JD Amaris, Middle/Senior).
Toàn bộ việc học xoay quanh **một project duy nhất**: **LeaveFlow**, module quản lý đơn nghỉ phép
chuyển từ Managers System sang stack của JD (mục 12 trong tài liệu).

> Mục tiêu: sau 3 tuần (2–3 giờ/ngày) bạn có thể nói *"I built this with ASP.NET Core and Angular
> to learn the stack"* và giải thích được từng dòng code bằng tiếng Anh.

---

## 0. Nguyên tắc rèn luyện

1. **Tự gõ code.** Dùng tài liệu, AI hay Claude để *giải thích, review và hỏi "why"*, không để viết hộ.
   Lịch sử commit chính là bằng chứng bạn tự làm.
2. **Học → Làm → Nói.** Mỗi khái niệm P1 phải đi qua 3 bước: đọc docs, áp dụng vào LeaveFlow,
   rồi tự giải thích thành tiếng bằng tiếng Anh trong 1 phút.
3. **Commit nhỏ, mỗi ngày ít nhất 1 commit**, message tiếng Anh (`feat: add leave request approval`).
   Mỗi tính năng làm trên một branch riêng và mở Pull Request có mô tả tiếng Anh: làm gì, vì sao,
   test thế nào (luyện kỹ năng ở mục 11).
4. **Backend xong, chạy được Swagger rồi mới làm Angular.**
5. **Ghi nhật ký** vào `docs/LOG.md` mỗi ngày: hôm nay làm gì, học được gì, câu hỏi nào còn chưa trả lời được.

### Lịch mỗi ngày (≈ 2,5 giờ)

| Thời lượng | Việc |
|---|---|
| 15 phút | Shadowing tiếng Anh (video .NET/Angular ngắn) |
| 30 phút | Đọc docs hoặc làm lab nhỏ của ngày |
| 75–90 phút | Code LeaveFlow theo checklist của ngày |
| 15 phút | Trả lời thành tiếng 2–3 câu hỏi (mục 13/13b), ghi âm, nghe lại |
| 5 phút | Commit, push, ghi `docs/LOG.md` |

---

## 1. Cấu trúc repo đề xuất

```
ASP.net/
├── README.md                    # tiếng Anh, dùng để gắn vào CV
├── docs/
│   ├── ROADMAP.md               # file này
│   ├── LOG.md                   # nhật ký học hằng ngày
│   └── interview-notes/         # câu trả lời tiếng Anh do bạn TỰ viết, mỗi mục 1 file
├── labs/                        # bài tập nhỏ, tách khỏi project chính
│   ├── 01-linq-console/
│   ├── 02-async-starvation/
│   ├── 03-di-lifetime/
│   ├── 04-efcore-n-plus-1/
│   ├── 05-sql-index/            # script .sql + ảnh chụp execution plan
│   ├── 06-rxjs-operators/
│   └── 07-sql-injection/
├── src/
│   ├── LeaveFlow.Api/           # ASP.NET Core Web API (Controllers)
│   └── leaveflow-web/           # Angular (standalone + signals)
├── tests/
│   ├── LeaveFlow.UnitTests/     # xUnit + Moq
│   └── LeaveFlow.IntegrationTests/  # WebApplicationFactory + Testcontainers
├── docker-compose.yml
└── .github/workflows/ci.yml
```

**Phiên bản:** dùng **.NET 10 (LTS)**; nếu công ty dùng .NET 8 thì khái niệm giống hệt.
Angular dùng bản mới nhất (standalone component, signals, `@if`/`@for`), nhưng đọc thêm về NgModule
vì dự án khách hàng có thể còn dùng kiểu cũ.

**Kiến trúc:** bắt đầu bằng **một project API** chia thư mục theo tính năng
(`Features/LeaveRequests`, `Data`, `Common`). Đến tuần 4 (tùy chọn) tách thành
Api / Application / Domain / Infrastructure. Lần tách này trở thành câu chuyện
*"refactor an toàn có test"* để kể khi phỏng vấn.

---

## 2. Thiết kế dữ liệu (EF Core Code-first)

| Bảng | Cột chính | Ghi chú luyện tập |
|---|---|---|
| `Departments` | Id, Name | quan hệ 1-n với Employees |
| `Employees` | Id, FullName, Email, PasswordHash, Role, DepartmentId, ManagerId | unique index trên Email; tự tham chiếu ManagerId |
| `LeaveRequests` | Id, EmployeeId, StartDate, EndDate, Type, Reason, Status, ReviewedById, ReviewedAt, CreatedAt, **RowVersion** | composite index `(EmployeeId, Status)`; RowVersion cho optimistic concurrency |
| `LeaveBalances` | EmployeeId, Year, TotalDays, UsedDays | cập nhật cùng transaction khi duyệt đơn |
| `AuditLogs` *(tuần 4)* | Id, Entity, EntityId, Action, UserId, At | dùng cho câu system design "leave management" |

`Status`: `Pending → Approved | Rejected | Cancelled`. Cascade delete: dùng `Restrict`, không xóa cứng nhân viên.

## 3. API contract (`/api/v1`)

| Method | Route | Role | Status code cần trả đúng |
|---|---|---|---|
| POST | `/auth/login` | public | 200, 400, 401 |
| GET | `/employees?page=&pageSize=&search=` | Manager | 200, 401, 403 |
| GET | `/employees/{id}` | Manager | 200, 404 |
| GET | `/leave-requests?status=&page=&pageSize=` | Employee (chỉ của mình), Manager (cả team) | 200 |
| GET | `/leave-requests/{id}` | chủ đơn hoặc Manager | 200, 403/404 (chống IDOR) |
| POST | `/leave-requests` | Employee | **201 + Location**, 400 (ProblemDetails) |
| PUT | `/leave-requests/{id}` | chủ đơn, khi còn Pending | 204, 400, 404, 409 |
| POST | `/leave-requests/{id}/approve` | Manager | 204, 403, 404, **409** (đã bị người khác duyệt) |
| POST | `/leave-requests/{id}/reject` | Manager | 204, 403, 404, 409 |
| DELETE | `/leave-requests/{id}` | chủ đơn, khi còn Pending | 204, 404 |

Quy tắc nghiệp vụ: `EndDate >= StartDate`; không trùng với đơn khác đang Pending/Approved;
không vượt số ngày phép còn lại; Manager chỉ duyệt đơn của nhân viên mình quản lý.

---

## 4. Lộ trình theo ngày

Ký hiệu: 📖 học · 🛠 làm · ✅ xong khi · 🗣 luyện nói (tiếng Anh). Số mục (§) là mục trong tài liệu PDF.

### Tuần 1: C# hiện đại, ASP.NET Core Web API, EF Core, SQL (§1–4)

**Ngày 1: C# và LINQ** (§1)
- 📖 Microsoft Learn: *C# fundamentals*; `IEnumerable` vs `IQueryable`, deferred execution, `record`, nullable reference types.
- 🛠 `labs/01-linq-console`: danh sách nhân viên giả, viết **20 câu LINQ** (group theo phòng ban, top 3 lương, join phòng ban, đếm theo trạng thái…).
- 🛠 `labs/02-async-starvation`: dùng `.Result` / `.Wait()` gây treo hoặc chậm, sau đó sửa bằng async/await và `CancellationToken`.
- ✅ Giải thích được deferred execution và vì sao không dùng `.Result`.
- 🗣 *IEnumerable vs IQueryable? What is sync-over-async?*

**Ngày 2: Khởi tạo Web API, DI, middleware** (§2)
- 🛠 `dotnet new sln`, `dotnet new webapi -o src/LeaveFlow.Api --use-controllers`, `dotnet new gitignore`.
- 🛠 `labs/03-di-lifetime`: 3 service Singleton / Scoped / Transient, mỗi cái sinh `Guid` trong constructor, log qua 2 request để thấy khác biệt. Thử inject Scoped vào Singleton để thấy lỗi captive dependency.
- 🛠 Viết `RequestTimingMiddleware` đo thời gian xử lý request; thử đổi thứ tự `UseAuthentication` / `UseAuthorization`.
- ✅ Swagger chạy; log thể hiện rõ 3 lifetime.
- 🗣 *AddSingleton vs AddScoped vs AddTransient? What is middleware, does order matter?*

**Ngày 3: EF Core và SQL Server** (§3)
- 🛠 Chạy SQL Server bằng Docker (`mcr.microsoft.com/mssql/server`), tạo `AppDbContext`, các entity ở §2 của file này, cấu hình bằng Fluent API trong `OnModelCreating` (index, quan hệ, `Restrict`, `RowVersion`).
- 🛠 `dotnet ef migrations add Init`, `database update`, seed data (2 phòng ban, 1 manager, 5 nhân viên).
- 🛠 Luyện migration: add, remove, rollback về migration cũ; đọc file migration được sinh ra.
- 🛠 Bật log SQL (`LogTo(Console.WriteLine)`).
- ✅ Database được tạo hoàn toàn từ migration.
- 🗣 *Code-first vs Database-first? What does DbContext do?*

**Ngày 4: CRUD theo mẫu Controller → Service → EF Core** (§2, §3)
- 🛠 DTO dạng `record` (`LeaveRequestDto`, `CreateLeaveRequest`), không trả entity ra ngoài.
- 🛠 `ILeaveRequestService` + `LeaveRequestService`, controller dùng `ActionResult<T>`, `CreatedAtAction`, `NotFound`, `NoContent`.
- 🛠 Phân trang + lọc theo status ở database (`Skip/Take`), `AsNoTracking()` và projection `Select` cho truy vấn đọc.
- ✅ Gọi đủ CRUD trong Swagger/Postman, status code đúng như bảng API.
- 🗣 *PUT vs PATCH? Is POST idempotent? Why DTOs instead of entities?*

**Ngày 5: Validation, xử lý lỗi, cấu hình** (§2)
- 🛠 FluentValidation (hoặc DataAnnotations) cho request; lỗi trả về dạng `ProblemDetails`.
- 🛠 Global exception handler bằng `IExceptionHandler`; logging bằng `ILogger` (hoặc Serilog).
- 🛠 Options pattern: đọc `LeavePolicyOptions` (số ngày phép mặc định) từ `appsettings.json` qua `IOptions<T>`.
- 🛠 Quy tắc nghiệp vụ: ngày hợp lệ, không trùng đơn, không vượt số ngày phép.
- ✅ Gửi request sai trả 400 có lỗi theo từng field; exception không lộ stack trace ra client.

**Ngày 6: JWT, phân quyền, chống IDOR** (§2, §9)
- 🛠 `/auth/login`: hash mật khẩu bằng `PasswordHasher<T>`, phát JWT có claim `sub`, `role`; key đặt trong user-secrets, không commit.
- 🛠 `[Authorize(Roles = "Manager")]` và ít nhất 1 policy (ví dụ `CanApproveLeave`).
- 🛠 Endpoint approve/reject; cập nhật `LeaveBalances` trong cùng transaction.
- 🛠 Chống IDOR: Employee đổi id trên URL không xem được đơn của người khác.
- ✅ Kiểm tra lần lượt các status 201, 400, 401, 403, 404 trong Postman.
- 🗣 *401 vs 403? Authentication vs authorization? What is inside a JWT?*

**Ngày 7: Concurrency, N+1, SQL tối ưu + ôn tuần** (§3, §4)
- 🛠 Optimistic concurrency: 2 tab Postman cùng duyệt một đơn → `DbUpdateConcurrencyException` → trả **409**.
- 🛠 `labs/04-efcore-n-plus-1`: cố ý tạo N+1, đếm số câu SQL trong log, sửa bằng `Include` rồi bằng `Select`.
- 🛠 `labs/05-sql-index`: script sinh ~1 triệu dòng Attendance; đo truy vấn trước/sau khi thêm index, đọc execution plan (Seek/Scan/Key Lookup), sửa `YEAR(CheckIn) = 2025` thành điều kiện SARGable; viết câu "lương cao thứ 2 mỗi phòng ban" bằng `DENSE_RANK`.
- 🛠 Từ hôm nay: mỗi ngày 2 bài **LeetCode Database / HackerRank SQL**.
- ✅ Có ảnh chụp execution plan trước/sau trong `labs/05-sql-index`.
- 🗣 *How do you solve N+1? A query is slow in production, what do you do?* Viết câu chuyện STAR tối ưu SQL ở LinkQ.

### Tuần 2: TypeScript, Angular, RxJS (§5–6)

**Ngày 8: Angular cơ bản qua đối chiếu React**
- 📖 Tutorial chính thức trên angular.dev (có thể luyện trên StackBlitz); TypeScript strict, interface, union type.
- 🛠 `ng new leaveflow-web` (strict, routing, standalone). Tạo layout, route `login`, `leave-requests`, `leave-requests/new`, `manage`.
- 🛠 Tự viết vào `docs/interview-notes/angular-vs-react.md` bảng đối chiếu React → Angular theo trí nhớ, sau đó so với tài liệu.
- ✅ App chạy, điều hướng được giữa các trang rỗng.

**Ngày 9: RxJS** (§6)
- 📖 rxjs.dev + rxmarbles.com: Observable vs Promise, Subject, BehaviorSubject, ReplaySubject.
- 🛠 `labs/06-rxjs-operators`: một nút bấm gọi request giả có `delay(2000)`, lần lượt dùng **switchMap, mergeMap, concatMap, exhaustMap**; bấm liên tục và ghi lại hành vi khác nhau của từng cái.
- ✅ Nói được mỗi operator dùng cho tình huống nào trong LeaveFlow (search, upload, lưu tuần tự, nút Submit).
- 🗣 *switchMap vs mergeMap vs concatMap vs exhaustMap? Observable vs Promise?*

**Ngày 10: Auth phía frontend**
- 🛠 `AuthService` giữ user hiện tại bằng signal hoặc `BehaviorSubject`; lưu token (ghi lại trade-off localStorage và httpOnly cookie).
- 🛠 Trang login dùng Reactive Forms; `authInterceptor` gắn JWT; interceptor xử lý lỗi 401 → về trang login.
- 🛠 `canActivate` guard kiểm tra đăng nhập + guard theo role Manager. Bật CORS cho origin Angular ở backend.
- ✅ Đăng nhập thật với API, refresh trang vẫn giữ trạng thái đăng nhập.
- 🗣 *Why do we still need backend authorization if we have guards?*

**Ngày 11: Danh sách đơn nghỉ phép**
- 🛠 `LeaveRequestService` dùng `HttpClient`; component danh sách dùng `async` pipe, hiển thị loading/lỗi/rỗng.
- 🛠 Ô tìm kiếm: `debounceTime` + `distinctUntilChanged` + `switchMap` + `catchError`; lọc theo status, phân trang.
- 🛠 `ChangeDetectionStrategy.OnPush`, `@for (...; track item.id)`.
- ✅ Gõ nhanh vào ô tìm kiếm, tab Network chỉ có request cuối cùng.
- 🗣 *Default vs OnPush change detection?*

**Ngày 12: Form tạo đơn**
- 🛠 Reactive Form: `FormGroup`, `Validators`, **custom validator** "EndDate phải sau StartDate", hiển thị lỗi khi control đã touched.
- 🛠 Hiển thị lỗi theo field từ `ProblemDetails` của backend.
- 🛠 Nút Submit dùng `exhaustMap` (hoặc disable khi đang lưu) để chống bấm nhiều lần.
- ✅ Bấm Submit 5 lần liên tục chỉ tạo đúng 1 đơn.

**Ngày 13: Màn hình Manager**
- 🛠 Route `manage` lazy load (`loadComponent` / `loadChildren`); danh sách đơn Pending của team, nút Approve/Reject.
- 🛠 Xử lý 409: thông báo "đơn đã được người khác xử lý" rồi tải lại.
- 🛠 Rà mọi chỗ `.subscribe()`: thay bằng `async` pipe hoặc thêm `takeUntilDestroyed()`.
- ✅ Không còn subscription nào bị bỏ ngỏ.
- 🗣 *How do you prevent memory leaks in Angular?*

**Ngày 14: Ôn tuần + buffer**
- 🛠 Làm nốt việc còn dở. Đọc về NgModule, `@Input/@Output` kiểu cũ, `*ngIf/*ngFor`, state với service + BehaviorSubject vs NgRx.
- 🗣 Trả lời thành tiếng toàn bộ câu Angular/RxJS ở §13 và §13b.

### Tuần 3: Testing, Docker, CI/CD, bảo mật, tích hợp, Azure, phỏng vấn (§7–11, §13–14)

**Ngày 15: Unit test backend** (§7)
- 🛠 `tests/LeaveFlow.UnitTests` (xUnit + Moq), mẫu AAA. Ít nhất **5 test** cho `LeaveRequestService`: tạo đơn hợp lệ, ngày sai, trùng đơn, vượt số ngày phép, duyệt đơn không tồn tại.
- 🛠 Inject `TimeProvider` thay vì gọi `DateTime.Now` để test được logic theo ngày.
- ✅ `dotnet test` xanh. Thói quen mới: sửa bug nào thì viết test tái hiện bug đó trước.

**Ngày 16: Integration test + test Angular** (§7)
- 🛠 `tests/LeaveFlow.IntegrationTests`: `WebApplicationFactory` + Testcontainers (SQL Server thật). Ít nhất 1 test: POST trả 201; gọi không có token trả 401.
- 🛠 Angular: 1 test service với `HttpTestingController`, 1 test component với `TestBed` + service giả.
- ✅ Cả `dotnet test` và `ng test` đều xanh.
- 🗣 *Unit vs integration vs E2E? Why not EF Core InMemory?*

**Ngày 17: Docker + CI** (§8)
- 🛠 Dockerfile multi-stage cho API; `docker-compose.yml` gồm `api` + `sqlserver` (+ `web` nếu kịp); biến môi trường cho connection string.
- 🛠 `.github/workflows/ci.yml`: mỗi Pull Request chạy build + test backend và build + test frontend.
- 📖 learngitbranching.js.org: merge vs rebase, cherry-pick, revert vs reset.
- ✅ `docker compose up` chạy được toàn bộ hệ thống; PR hiển thị check xanh.

**Ngày 18: Bảo mật** (§9)
- 🛠 `labs/07-sql-injection`: cố ý nối chuỗi SQL, chèn `' OR 1=1 --`, rồi sửa bằng tham số.
- 🛠 Dán JWT vào jwt.io để xem payload; thử đổi id trên URL để kiểm tra IDOR lần nữa.
- 🛠 Rate limiting cho `/auth/login` (`AddRateLimiter`); bật HTTPS/HSTS; kiểm tra không có secret nào trong Git.
- 🛠 *(Stretch)* refresh token lưu DB, xoay vòng mỗi lần dùng.
- ✅ `docs/interview-notes/owasp-top10.md`: mỗi mục tóm tắt 2 câu tiếng Anh.
- 🗣 *How do you revoke a JWT before it expires? localStorage vs httpOnly cookie?*

**Ngày 19: Tích hợp hệ thống bên ngoài** (§10)
- 🛠 Gọi API ngày lễ công khai (ví dụ Nager.Date) để **không tính ngày lễ vào số ngày nghỉ**, dùng `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience` (timeout, retry có backoff, circuit breaker).
- 🛠 Tắt mạng hoặc trỏ sai URL để thấy retry, timeout và fallback hoạt động.
- 🛠 *(Stretch)* Đăng nhập Google (OIDC); hoặc một demo nhỏ Stripe test mode + webhook để có trải nghiệm thật về idempotency key.
- 🗣 *How would you integrate a payment provider safely?*

**Ngày 20: Azure** (§8)
- 📖 Lộ trình AZ-900 trên Microsoft Learn: App Service, Azure SQL, Blob Storage, Key Vault, Application Insights.
- 🛠 Tài khoản Azure free: deploy API lên App Service + Azure SQL; connection string để trong App Settings / Key Vault; bật Application Insights.
- ✅ Có URL demo chạy được (hoặc ghi rõ các bước đã làm nếu hết credit).

**Ngày 21: Đóng gói và luyện phỏng vấn** (§12–14)
- 🛠 `README.md` tiếng Anh: vấn đề, kiến trúc (sơ đồ), cách chạy, ảnh chụp màn hình, những gì đã học. Thêm link GitHub vào CV.
- 🗣 Thuyết trình project trong **3 phút** bằng tiếng Anh: vấn đề, kiến trúc, khó khăn, bài học.
- 🗣 Tự giới thiệu 60–90 giây; 3 câu chuyện STAR (tối ưu SQL, tích hợp tra cứu mã số thuế, luồng contract/payment hoặc phân quyền).
- 🗣 Phỏng vấn thử với bạn bè hoặc AI: yêu cầu hỏi tiếp "why" sau mỗi câu trả lời.

### Tuần 4 (tùy chọn): biến câu hỏi §13b thành thí nghiệm thật

Mỗi thí nghiệm giúp bạn trả lời *"I tried it in my side project…"* thay vì chỉ học thuộc.

| Câu hỏi §13b | Thí nghiệm trong LeaveFlow |
|---|---|
| Offset pagination chậm ở trang 5.000 | Seed 10 triệu dòng, so sánh `OFFSET` với keyset pagination (`WHERE Id > @lastId`) |
| Hai manager duyệt cùng lúc | Đã làm ngày 7; thêm cách `UPDATE ... WHERE Status = 'Pending'` và so sánh |
| IMemoryCache khi scale 3 instance | Cache danh sách phòng ban, chạy 2 container API, quan sát dữ liệu lệch; ghi chú hướng Redis |
| Thêm cột NOT NULL không downtime | Thực hành expand/contract qua 3 migration |
| Stored procedure lúc nhanh lúc chậm | Tái hiện parameter sniffing, sửa bằng `OPTION (RECOMPILE)` |
| Lỗi production không tái hiện được | Thêm correlation id vào log, tra trong Application Insights |
| Repository trên EF Core: có nên không? | Viết thử, rồi ghi lại trade-off vào `docs/interview-notes` |
| Design a leave management module | Thêm `AuditLogs` + gửi thông báo sau khi commit (outbox table) |
| Refactor legacy code | Tách API thành Api / Application / Domain / Infrastructure, giữ test xanh |

---

## 5. Bảng đối chiếu: mục tài liệu → nơi luyện trong project

| § | Chủ đề | Luyện ở đâu |
|---|---|---|
| 1 | C# & .NET hiện đại | `labs/01`, `labs/02`, DTO `record`, nullable |
| 2 | ASP.NET Core Web API | `LeaveFlow.Api`, `labs/03` |
| 3 | EF Core | `AppDbContext`, migrations, `labs/04`, RowVersion |
| 4 | SQL | `labs/05`, LeetCode Database mỗi ngày |
| 5 | TypeScript & Angular | `leaveflow-web` |
| 6 | RxJS | `labs/06`, ô tìm kiếm, nút Submit |
| 7 | Testing | `tests/*`, `ng test` |
| 8 | Git, CI/CD, Docker, Azure | PR mỗi tính năng, `ci.yml`, `docker-compose.yml`, App Service |
| 9 | Bảo mật | JWT, IDOR, `labs/07`, rate limiting |
| 10 | Tích hợp bên thứ ba | API ngày lễ + resilience, (Stripe/Google) |
| 11 | Hiệu năng, refactor, làm việc nhóm | AsNoTracking, OnPush, tuần 4, mô tả PR |
| 12 | Mini project | Toàn bộ LeaveFlow |
| 13–14 | Câu hỏi & tiếng Anh | `docs/interview-notes`, ghi âm mỗi ngày |

## 6. Checklist hoàn thành (theo §12 và checklist trước ngày phỏng vấn)

**Project**
- [ ] ASP.NET Core Web API: Employees, LeaveRequests (CRUD + Approve/Reject)
- [ ] EF Core + SQL Server, migrations, seed data, quan hệ 1-n
- [ ] JWT login, 2 role: Employee (tạo đơn), Manager (duyệt đơn)
- [ ] Validation + global exception handler trả ProblemDetails; Swagger
- [ ] Phân trang + lọc theo trạng thái; AsNoTracking + projection
- [ ] Optimistic concurrency trả 409
- [ ] Angular: login, guard, interceptor, danh sách có tìm kiếm (debounce + switchMap), reactive form tạo đơn
- [ ] Ít nhất 5 unit test (xUnit + Moq) và 1 integration test
- [ ] Dockerfile + docker-compose (api + sqlserver); GitHub Actions chạy build và test
- [ ] README tiếng Anh: kiến trúc, cách chạy, ảnh chụp màn hình; link trong CV

**Phỏng vấn**
- [ ] Giải thích bằng tiếng Anh: DI lifetime, middleware, N+1, index, 4 flattening operator, OnPush
- [ ] Viết tay được controller + service + DTO và một component Angular có HttpClient
- [ ] Thuộc phần tự giới thiệu + 3 câu chuyện STAR
- [ ] Giải thích được mọi dòng trong CV
- [ ] Chuẩn bị 3 câu hỏi để hỏi lại người phỏng vấn

## 7. Tài liệu chính thức

- Microsoft Learn: ASP.NET Core fundamentals, Web API tutorial, EF Core docs, AZ-900
- angular.dev: tutorial, Essentials, Signals, Router, Forms, HttpClient
- rxjs.dev + rxmarbles.com
- use-the-index-luke.com
- OWASP Top 10 + OWASP Cheat Sheet Series
- github.com/dotnet/eShop: tham khảo cách tổ chức project và test thực tế
