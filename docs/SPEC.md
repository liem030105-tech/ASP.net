# LeaveFlow: Product & Technical Specification

| | |
|---|---|
| Version | 1.0 |
| Status | Ready for implementation |
| Stack | ASP.NET Core Web API (.NET 10), EF Core, SQL Server, Angular, RxJS |
| Related | [ROADMAP.md](ROADMAP.md) (learning plan), [SETUP.md](SETUP.md) (dev environment) |

This document is the single source of truth for **what** LeaveFlow does. The roadmap describes
**when** each part is built. If the two disagree, this spec wins and the roadmap should be updated.

---

## 1. Overview

### 1.1 Problem
Small companies often handle leave requests by chat or email. Managers lose track of who is off,
employees do not know how many days they have left, and two people can approve or change the same
request at the same time.

### 1.2 Solution
LeaveFlow is a web app where employees submit leave requests and see their remaining balance, and
managers review and approve or reject requests from their team.

### 1.3 Goals
- G1: An employee can submit a leave request in under one minute and always sees an up-to-date balance.
- G2: A manager sees every pending request from their team in one place and can approve or reject it.
- G3: Data stays consistent: no overlapping requests, no negative balance, no double approval.
- G4 (learning): the codebase demonstrates the P1 topics of the interview guide (REST, EF Core,
  JWT, validation, RxJS, testing, Docker, CI).

### 1.4 Glossary

| Term | Meaning |
|---|---|
| Working day | Monday to Friday, excluding public holidays |
| Balance | Annual leave days granted for a calendar year |
| Available days | `TotalDays − UsedDays − PendingDays` (Annual leave only) |
| Direct report | An employee whose `ManagerId` is the current manager |

---

## 2. Scope

**Priority** uses MoSCoW: **M**ust (MVP, weeks 1–3), **S**hould (do if time allows in weeks 1–3),
**C**ould (week 4 or later).

### In scope
- Authentication with email and password, JWT bearer tokens
- Two roles: Employee and Manager
- Leave requests: create, edit, cancel, approve, reject, list, detail
- Annual leave balance per employee per year
- Working-day calculation that excludes weekends and public holidays
- Employee directory for managers

### Out of scope (Future)
- Half-day leave, leave spanning two calendar years
- Cancelling a request that is already approved
- Multi-level approval, delegation while a manager is away
- Email / push notifications, audit log UI (see roadmap week 4)
- User self-registration and password reset (accounts come from seed data)
- Multi-tenancy

---

## 3. Roles and permissions

A Manager is also an employee: a manager can submit their own requests.

| Action | Employee | Manager |
|---|:---:|:---:|
| Log in, view own profile | ✅ | ✅ |
| View own requests and balance | ✅ | ✅ |
| Create / edit / cancel own **Pending** request | ✅ | ✅ |
| View requests of direct reports | ❌ | ✅ |
| Approve / reject requests of direct reports | ❌ | ✅ |
| Approve / reject own request | ❌ | ❌ |
| View employee directory | ❌ | ✅ |

Requests from an employee who has **no manager** (`ManagerId = null`) can be reviewed by **any other**
Manager.

---

## 4. User stories and acceptance criteria

Format: *As a … I want … so that …*, followed by Given / When / Then criteria.
Each story lists the business rules (§5) and endpoints (§8) it depends on.

### US-01 · Log in · M
As a user I want to log in with email and password so that I can use the app.
- **Given** valid credentials **when** I log in **then** I receive an access token and my profile (id, name, email, role).
- **Given** a wrong email or password **when** I log in **then** I get 401 with a generic message that does not say which field was wrong.
- **Given** more than 5 login attempts per minute from the same IP **then** I get 429.
- **Given** an inactive account **then** login fails with 401.
- Endpoints: `POST /auth/login`, `GET /auth/me`

### US-02 · Log out · M
As a user I want to log out so that nobody else can use my session on this browser.
- **When** I click Log out **then** the token is removed on the client and I am redirected to `/login`.
- **When** any API call returns 401 **then** the app logs me out and redirects to `/login`.

### US-03 · View my requests · M
As an employee I want to see my leave requests so that I know their status.
- List shows type, dates, working days, status, created date; newest first.
- I can filter by status and by date range, and page through results (default 20 per page).
- Empty state: "You have no leave requests yet" with a button to create one.
- Endpoint: `GET /leave-requests?scope=mine`

### US-04 · View my balance · M
As an employee I want to see my annual leave balance so that I know how many days I can still request.
- Shows total, used, pending and available days for the current year.
- Balance updates right after a request is created, cancelled, approved or rejected.
- BR-05 · Endpoint: `GET /leave-balances/me`

### US-05 · Create a request · M
As an employee I want to submit a leave request so that my manager can approve it.
- **Given** valid data **when** I submit **then** the request is created with status Pending and I get 201 with a `Location` header.
- **Given** invalid data **then** I get 400 with field-level errors and the form shows them next to each field.
- Clicking Submit several times quickly creates **exactly one** request.
- BR-01, BR-02, BR-03, BR-04, BR-05, BR-06 · Endpoint: `POST /leave-requests`

### US-06 · Edit a pending request · M
As an employee I want to change a request that has not been reviewed so that I can fix mistakes.
- Only my own requests in status Pending can be edited; otherwise 409.
- If the request was changed by someone else since I loaded it (stale `rowVersion`), I get 409 and the UI asks me to reload.
- All creation rules apply again.
- BR-01…BR-07, BR-10 · Endpoint: `PUT /leave-requests/{id}`

### US-07 · Cancel a pending request · M
As an employee I want to cancel a request I no longer need.
- Only my own Pending requests can be cancelled; status becomes Cancelled (the row is never deleted).
- Pending days are released from the balance.
- BR-07, BR-08 · Endpoint: `POST /leave-requests/{id}/cancel`

### US-08 · View team requests · M
As a manager I want to see requests from my direct reports so that I can review them.
- Default filter: status Pending, oldest first (first come, first served).
- I can search by employee name (debounced as I type), filter by status, and page.
- I never see requests from employees who are not my direct reports.
- Endpoint: `GET /leave-requests?scope=team`

### US-09 · Approve a request · M
As a manager I want to approve a pending request so that the employee can take leave.
- **Given** a Pending request from my direct report **when** I approve **then** status becomes Approved, `ReviewedBy`/`ReviewedAt` are set, and for Annual leave `UsedDays` increases by the request's working days, all in **one transaction**.
- **Given** the request is no longer Pending (another manager already reviewed it, or it was cancelled) **then** I get 409 and the UI shows "This request was already processed" and reloads the list.
- **Given** two managers approve at the same moment **then** exactly one succeeds and the other gets 409.
- BR-07, BR-08, BR-09, BR-10 · Endpoint: `POST /leave-requests/{id}/approve`

### US-10 · Reject a request · M
As a manager I want to reject a request with a reason so that the employee understands why.
- A reason (1–500 characters) is required; otherwise 400.
- Status becomes Rejected; pending days are released.
- Same 409 behaviour as US-09.
- BR-07, BR-09, BR-10 · Endpoint: `POST /leave-requests/{id}/reject`

### US-11 · Employee directory · M
As a manager I want to search employees so that I can check someone's department and balance.
- Search by name or email, filter by department, paged.
- Detail shows department, manager and current-year balance.
- Endpoints: `GET /employees`, `GET /employees/{id}`, `GET /departments`

### US-12 · Exclude public holidays · S
As an employee I want public holidays not to count as leave days so that my balance is correct.
- Working days exclude Saturdays, Sundays and public holidays of the configured country.
- Holidays come from an external public API, cached for 24 hours.
- If the holiday API is unavailable, the system retries with backoff; if it still fails, it falls back to weekends only and logs a warning (it never blocks the user).
- BR-04 · Endpoint: `GET /leave-requests/working-days`

### US-13 · Working-day preview · S
As an employee I want to see how many working days my request will use before I submit it.
- When both dates are valid, the form shows "N working days" and lists any holidays in the range.
- Endpoint: `GET /leave-requests/working-days`

### US-14 · Request detail · M
As an employee or manager I want to open one request and see all its information.
- Shows all fields, including reviewer, review time and comment.
- A user who is neither the owner nor a reviewer allowed by §3 gets **404** (not 403), so ids of other people's requests cannot be discovered (anti-IDOR).
- Endpoint: `GET /leave-requests/{id}`

---

## 5. Business rules

| ID | Rule | Error |
|---|---|---|
| BR-01 | `EndDate >= StartDate` | 400 `INVALID_DATE_RANGE` |
| BR-02 | `StartDate` must not be in the past (server date, UTC) | 400 `START_DATE_IN_PAST` |
| BR-03 | `StartDate` and `EndDate` are in the same calendar year | 400 `CROSS_YEAR_NOT_SUPPORTED` |
| BR-04 | `WorkingDays` = number of working days in the range and must be ≥ 1 | 400 `NO_WORKING_DAYS` |
| BR-05 | For **Annual** leave, `WorkingDays <= AvailableDays`. Sick and Unpaid leave do not use the balance | 400 `INSUFFICIENT_BALANCE` |
| BR-06 | A request must not overlap another **Pending or Approved** request of the same employee | 400 `OVERLAPPING_REQUEST` |
| BR-07 | Only **Pending** requests can be edited, cancelled, approved or rejected | 409 `INVALID_STATUS` |
| BR-08 | Only the owner can edit or cancel a request | 403 (or 404 if they cannot see it) |
| BR-09 | Only an allowed reviewer (§3) can approve or reject; nobody reviews their own request | 403 (or 404 if they cannot see it) |
| BR-10 | Concurrent changes are detected with `RowVersion` (optimistic concurrency) | 409 `CONCURRENCY_CONFLICT` |
| BR-11 | Approving Annual leave increases `UsedDays` in the **same transaction** as the status change | — |

`Reason` is optional (max 500 characters). `ReviewComment` is required on reject (1–500), optional on approve.

---

## 6. Leave request lifecycle

```
              approve (Manager)
          ┌──────────────────────▶ Approved
          │
Pending ──┼── reject (Manager) ──▶ Rejected
          │
          └── cancel (Owner) ────▶ Cancelled
   ▲
   └── edit (Owner) keeps status Pending
```

| From | Action | To | Who |
|---|---|---|---|
| — | create | Pending | Owner |
| Pending | edit | Pending | Owner |
| Pending | cancel | Cancelled | Owner |
| Pending | approve | Approved | Allowed reviewer |
| Pending | reject | Rejected | Allowed reviewer |
| Approved / Rejected / Cancelled | any | — | 409 (final states) |

---

## 7. Data model

All timestamps are stored in **UTC** (`datetime2`). Calendar dates use `date` (`DateOnly` in C#).
Enums are stored as strings (`nvarchar(20)`) so the database stays readable.
Delete behaviour is **Restrict** everywhere; nothing is hard-deleted.

### Departments
| Column | Type | Null | Notes |
|---|---|---|---|
| Id | int identity | no | PK |
| Name | nvarchar(100) | no | unique |

### Employees
| Column | Type | Null | Notes |
|---|---|---|---|
| Id | int identity | no | PK |
| FullName | nvarchar(150) | no | |
| Email | nvarchar(256) | no | unique index, stored lower-case |
| PasswordHash | nvarchar(500) | no | ASP.NET Core `PasswordHasher<T>` |
| Role | nvarchar(20) | no | `Employee` \| `Manager` |
| DepartmentId | int | no | FK → Departments |
| ManagerId | int | yes | FK → Employees (self reference) |
| IsActive | bit | no | default 1 |
| CreatedAt | datetime2 | no | |

### LeaveRequests
| Column | Type | Null | Notes |
|---|---|---|---|
| Id | int identity | no | PK |
| EmployeeId | int | no | FK → Employees |
| Type | nvarchar(20) | no | `Annual` \| `Sick` \| `Unpaid` |
| StartDate | date | no | |
| EndDate | date | no | check `EndDate >= StartDate` |
| WorkingDays | int | no | calculated on create/edit (BR-04) |
| Reason | nvarchar(500) | yes | |
| Status | nvarchar(20) | no | `Pending` \| `Approved` \| `Rejected` \| `Cancelled` |
| ReviewedById | int | yes | FK → Employees |
| ReviewedAt | datetime2 | yes | |
| ReviewComment | nvarchar(500) | yes | |
| CreatedAt | datetime2 | no | |
| UpdatedAt | datetime2 | yes | |
| RowVersion | rowversion | no | concurrency token |

Indexes:
- `IX_LeaveRequests_EmployeeId_Status` on `(EmployeeId, Status)` INCLUDE `(StartDate, EndDate)`: "my requests" list and overlap check (BR-06)
- `IX_LeaveRequests_Status_CreatedAt` on `(Status, CreatedAt)`: team list ordered by oldest pending

### LeaveBalances
| Column | Type | Null | Notes |
|---|---|---|---|
| Id | int identity | no | PK |
| EmployeeId | int | no | FK → Employees |
| Year | int | no | unique `(EmployeeId, Year)` |
| TotalDays | int | no | default from `LeavePolicy:AnnualDays` (12) |
| UsedDays | int | no | default 0, check `UsedDays <= TotalDays` |
| RowVersion | rowversion | no | |

`PendingDays` is **not stored**: it is calculated from Pending Annual requests, so it can never drift.

### Configuration (`appsettings.json`, Options pattern)
```json
{
  "Jwt": { "Issuer": "LeaveFlow", "Audience": "LeaveFlow.Web", "AccessTokenMinutes": 60 },
  "LeavePolicy": { "AnnualDays": 12 },
  "Holidays": { "CountryCode": "VN", "BaseUrl": "https://date.nager.at/api/v3/", "CacheHours": 24 },
  "Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }
}
```
`Jwt:SigningKey` and the connection string are **secrets**: user-secrets in development, environment
variables or Key Vault in Docker/Azure. Never commit them.

---

## 8. API specification

Base URL: `/api/v1`. JSON uses camelCase. Dates are `yyyy-MM-dd`; timestamps are ISO 8601 UTC.

### 8.1 Authentication
- `Authorization: Bearer <accessToken>` on every endpoint except `POST /auth/login`.
- Token claims: `sub` (employee id), `email`, `name`, `role`. Lifetime 60 minutes (MVP).
- **Could:** 15-minute access token + refresh token stored in the database and rotated on every use (`POST /auth/refresh`).

### 8.2 Pagination
Query: `page` (default 1), `pageSize` (default 20, max 100). Response envelope:
```json
{ "items": [], "page": 1, "pageSize": 20, "totalCount": 57, "totalPages": 3 }
```

### 8.3 Errors
All errors use **ProblemDetails** (RFC 9457). Validation errors add `errors`; business-rule errors add `code`.
```json
{
  "type": "https://httpstatuses.io/400",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "endDate": [ "End date must be on or after start date." ] },
  "traceId": "00-4f1c…-01"
}
```
```json
{ "title": "Leave request is no longer pending.", "status": 409, "code": "INVALID_STATUS", "traceId": "…" }
```
Unhandled exceptions return 500 with a generic message only; details go to the log.

| Status | When |
|---|---|
| 200 / 201 / 204 | success (201 includes `Location`) |
| 400 | validation or business rule (BR-01…BR-06) |
| 401 | missing/invalid token, wrong credentials |
| 403 | authenticated but role/ownership does not allow the action |
| 404 | resource not found **or** not visible to the caller |
| 409 | status conflict or concurrency conflict |
| 429 | login rate limit exceeded |

### 8.4 Endpoints

#### Auth
| Method | Route | Auth | Success | Errors |
|---|---|---|---|---|
| POST | `/auth/login` | public | 200 | 400, 401, 429 |
| GET | `/auth/me` | any | 200 | 401 |

```http
POST /api/v1/auth/login
{ "email": "an.nguyen@leaveflow.local", "password": "…" }

200 OK
{
  "accessToken": "eyJhbGciOi…",
  "expiresAt": "2026-10-06T11:00:00Z",
  "user": { "id": 3, "fullName": "Nguyen Van An", "email": "an.nguyen@leaveflow.local", "role": "Employee" }
}
```

#### Leave requests
| Method | Route | Auth | Success | Errors |
|---|---|---|---|---|
| GET | `/leave-requests` | any | 200 | 400, 403 (`scope=team` as Employee) |
| GET | `/leave-requests/{id}` | owner / reviewer | 200 | 404 |
| GET | `/leave-requests/working-days?startDate=&endDate=` | any | 200 | 400 |
| POST | `/leave-requests` | any | 201 | 400 |
| PUT | `/leave-requests/{id}` | owner | 204 | 400, 403, 404, 409 |
| POST | `/leave-requests/{id}/cancel` | owner | 204 | 403, 404, 409 |
| POST | `/leave-requests/{id}/approve` | reviewer | 204 | 403, 404, 409 |
| POST | `/leave-requests/{id}/reject` | reviewer | 204 | 400, 403, 404, 409 |

List query parameters: `scope` (`mine` default \| `team`), `status`, `from`, `to`, `search`
(employee name, team scope only), `sort` (`createdAt` \| `-createdAt` \| `startDate`), `page`, `pageSize`.

```http
POST /api/v1/leave-requests
{ "type": "Annual", "startDate": "2026-10-19", "endDate": "2026-10-21", "reason": "Family trip" }

201 Created
Location: /api/v1/leave-requests/42
{
  "id": 42, "employeeId": 3, "employeeName": "Nguyen Van An",
  "type": "Annual", "startDate": "2026-10-19", "endDate": "2026-10-21", "workingDays": 3,
  "reason": "Family trip", "status": "Pending",
  "reviewedBy": null, "reviewedAt": null, "reviewComment": null,
  "createdAt": "2026-10-06T10:15:00Z", "rowVersion": "AAAAAAAAB9E="
}
```
```http
PUT /api/v1/leave-requests/42
{ "type": "Annual", "startDate": "2026-10-19", "endDate": "2026-10-20", "reason": "Shorter trip", "rowVersion": "AAAAAAAAB9E=" }
→ 204 No Content
```
```http
POST /api/v1/leave-requests/42/reject
{ "comment": "Release week, please choose other dates." }
→ 204 No Content
```
```http
GET /api/v1/leave-requests/working-days?startDate=2026-12-24&endDate=2026-12-31
→ 200 { "workingDays": 6, "holidays": [] }
```

#### Balances, employees, departments
| Method | Route | Auth | Success | Errors |
|---|---|---|---|---|
| GET | `/leave-balances/me?year=` | any | 200 | 404 |
| GET | `/employees?search=&departmentId=&page=&pageSize=` | Manager | 200 | 403 |
| GET | `/employees/{id}` | Manager | 200 | 403, 404 |
| GET | `/departments` | any | 200 | — |

```json
GET /api/v1/leave-balances/me?year=2026
{ "year": 2026, "totalDays": 12, "usedDays": 4, "pendingDays": 3, "availableDays": 5 }
```

#### Operations
| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/health` | public | health check (Could) |
| GET | `/swagger` | public, Development only | OpenAPI UI |

---

## 9. Frontend

### 9.1 Routes
| Path | Screen | Guard | Loading |
|---|---|---|---|
| `/login` | Login | redirect to `/requests` if already logged in | eager |
| `/requests` | My requests + balance | auth | eager |
| `/requests/new` | New request | auth | eager |
| `/requests/:id` | Request detail | auth | eager |
| `/requests/:id/edit` | Edit request | auth | eager |
| `/team/requests` | Team requests | auth + role Manager | **lazy** |
| `/employees` | Employee directory | auth + role Manager | **lazy** |
| `**` | Not found | — | eager |

Top bar: app name, links by role, user name, Log out.

### 9.2 Screens
- **Login**: reactive form (email required + email format, password required); Submit disabled while sending; shows the API error message.
- **My requests**: balance card (total / used / pending / available); table with status filter and paging; status shown as coloured badge; actions Edit and Cancel only for Pending rows; Cancel asks for confirmation.
- **New / Edit request**: fields Type, Start date, End date, Reason. Client validators: required fields, custom validator `endDate >= startDate`, reason ≤ 500. Shows "N working days" preview (US-13). Server errors from ProblemDetails are mapped to the matching control. Submit uses `exhaustMap` so double clicks send one request. On 409 shows "This request was changed, reload?".
- **Request detail**: read-only view with reviewer information.
- **Team requests**: search box (`debounceTime(300)` + `distinctUntilChanged` + `switchMap` + `catchError`), status filter, paging; Approve button and Reject button (opens a dialog with a required comment). On 409 shows a message and reloads.
- **Employees**: search + department filter + paging; row click shows detail with balance.

Every data screen has **loading**, **empty** and **error** states.

### 9.3 Technical rules
- Standalone components, `ChangeDetectionStrategy.OnPush`, `@if` / `@for (…; track item.id)`.
- Components never call `HttpClient` directly; they use services (`AuthService`, `LeaveRequestService`, `EmployeeService`).
- Current user state lives in `AuthService` (signal or `BehaviorSubject`).
- Functional interceptors: `authInterceptor` (adds Bearer token), `errorInterceptor` (401 → logout).
- Prefer the `async` pipe; manual subscriptions use `takeUntilDestroyed()`.
- API base URL comes from environment configuration.

---

## 10. Non-functional requirements

### Security
- Passwords hashed with `PasswordHasher<T>`; never logged or returned.
- All SQL through EF Core or parameterised queries; no string concatenation.
- Every authorization decision is made on the backend; the frontend only hides UI.
- Ownership checks on every request by id (anti-IDOR, US-14).
- CORS allows only configured origins. HTTPS + HSTS outside Development.
- Login rate limit: 5 requests per minute per IP (`AddRateLimiter`).
- Secrets in user-secrets / environment variables / Key Vault; `.gitignore` covers local settings.

### Performance
- Read queries use `AsNoTracking()` and `Select` projection to DTOs; no N+1 (verified in SQL logs).
- Paging and filtering happen in the database.
- p95 response time < 300 ms for list endpoints with 10,000 requests in the database (local Docker).
- Holiday data cached (`IMemoryCache`, 24 h).

### Reliability
- External HTTP calls use `IHttpClientFactory` with timeout, retry with exponential backoff and circuit breaker (`Microsoft.Extensions.Http.Resilience`).
- Global exception handler (`IExceptionHandler`) returns ProblemDetails.

### Observability
- Structured logging with `ILogger` (Serilog optional); every log includes the trace id.
- **Could:** Application Insights when deployed to Azure.

### Testing (minimum for MVP)
- ≥ 5 unit tests (xUnit + Moq) covering BR-01…BR-07 in the leave request service.
- ≥ 1 integration test (`WebApplicationFactory` + Testcontainers SQL Server): create returns 201, no token returns 401.
- Angular: 1 service test with `HttpTestingController`, 1 component test with `TestBed`.
- Time-dependent logic uses `TimeProvider` so tests control "today".

### Delivery
- `docker compose up` starts API + SQL Server (+ web, optional).
- GitHub Actions on every pull request: restore, build, test (backend and frontend).

---

## 11. Seed data (Development only)

| Email | Name | Role | Department | Manager |
|---|---|---|---|---|
| `binh.tran@leaveflow.local` | Tran Thi Binh | Manager | Engineering | — |
| `cuong.le@leaveflow.local` | Le Van Cuong | Manager | HR | — |
| `an.nguyen@leaveflow.local` | Nguyen Van An | Employee | Engineering | Binh |
| `dung.pham@leaveflow.local` | Pham Minh Dung | Employee | Engineering | Binh |
| `giang.vo@leaveflow.local` | Vo Thu Giang | Employee | Engineering | Binh |
| `hoa.dang@leaveflow.local` | Dang Thi Hoa | Employee | HR | Cuong |
| `khoa.bui@leaveflow.local` | Bui Dang Khoa | Employee | HR | Cuong |

- Every account gets a `LeaveBalances` row for the current year with 12 days.
- A few requests in each status so every screen has data.
- Demo password is read from configuration (`Seed:DemoPassword`), not hard-coded in source.

---

## 12. Definition of Done

A user story is done when:
1. All acceptance criteria pass when checked manually in Swagger/Postman and in the Angular UI.
2. Business rules it references have unit tests.
3. Status codes and error bodies match §8.
4. No warnings in `dotnet build`; `ng build` succeeds.
5. Merged through a pull request with an English description (what, why, how tested).

### Traceability

| Story | Rules | Roadmap day |
|---|---|---|
| US-01, US-02 | — | Day 6 (API), Day 10 (UI) |
| US-03, US-14 | BR-08 | Day 4 (API), Day 11 (UI) |
| US-04 | BR-05 | Day 5 (API), Day 11 (UI) |
| US-05 | BR-01…BR-06 | Day 4–5 (API), Day 12 (UI) |
| US-06, US-07 | BR-07, BR-08, BR-10 | Day 5, Day 7 (API), Day 12 (UI) |
| US-08 | — | Day 6 (API), Day 13 (UI) |
| US-09, US-10 | BR-07, BR-09, BR-10, BR-11 | Day 6–7 (API), Day 13 (UI) |
| US-11 | — | Day 4 (API), Day 13 (UI) |
| US-12, US-13 | BR-04 | Day 19 |
