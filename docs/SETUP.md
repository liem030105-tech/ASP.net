# Cài đặt môi trường (Windows + Visual Studio Community)

Làm hết file này **trước Ngày 1** của [ROADMAP.md](ROADMAP.md). Mất khoảng 1–2 giờ, phần lớn là chờ tải.

## 1. Công cụ sẽ dùng

| Việc | Công cụ |
|---|---|
| Viết, chạy, debug backend .NET | **Visual Studio Community** (IDE chính) |
| Viết Angular | Visual Studio Community (mở thư mục). VS Code là tùy chọn, xem mục 3 |
| Chạy lệnh `dotnet`, `ng`, `git`, `docker` | **Developer PowerShell** trong Visual Studio (*View → Terminal*) |
| Xem database, execution plan | **SSMS** |
| Gọi API | Swagger (có sẵn), file `.http` trong Visual Studio, hoặc Postman |

Visual Studio Community miễn phí cho cá nhân học tập. Thứ thật sự chạy .NET là **.NET SDK**
(lệnh `dotnet`); Visual Studio tự cài SDK và gọi nó khi bạn bấm F5. Vì vậy bạn vẫn nên tập dùng
một số lệnh `dotnet` (`build`, `run`, `test`, `ef`): người phỏng vấn hay hỏi, và CI (Ngày 17) chỉ
chạy bằng dòng lệnh.

## 2. Cài đặt

### 2.1 Visual Studio Community
Tải **Visual Studio Community** bản mới nhất từ `visualstudio.microsoft.com` (để làm .NET 10 cần
Visual Studio 2026 trở lên). Trong Visual Studio Installer, tick các workload:

| Workload | Dùng để |
|---|---|
| **ASP.NET and web development** | Web API, .NET SDK, hỗ trợ JavaScript/TypeScript |
| **Data storage and processing** | SQL Server Data Tools, SQL Server Express LocalDB |
| **Node.js development** (tùy chọn) | Hỗ trợ thêm cho project JavaScript/TypeScript |

Sau khi cài, mở Visual Studio một lần, đăng nhập tài khoản Microsoft (để không bị hết hạn dùng thử).

### 2.2 Git, Node.js, Angular CLI
Mở **PowerShell** và chạy:

```powershell
winget install Git.Git
winget install OpenJS.NodeJS.LTS
```

Đóng PowerShell, mở lại (để nhận PATH mới), rồi cài tiếp:

```powershell
npm install -g @angular/cli
dotnet tool install --global dotnet-ef
dotnet dev-certs https --trust
```

`dotnet dev-certs https --trust` tạo chứng chỉ HTTPS cho localhost. Bấm **Yes** khi Windows hỏi
(Visual Studio cũng sẽ hỏi việc này ở lần chạy API đầu tiên).

### 2.3 SQL Server: chọn một cách

**Cách A (khuyến nghị): Docker.** Giống cách chạy ở Ngày 17 với `docker compose`.

```powershell
wsl --install                      # cần khởi động lại máy nếu chưa có WSL2
winget install Docker.DockerDesktop
```

Mở Docker Desktop một lần, chờ báo "Engine running", rồi chạy SQL Server (viết trên **một dòng**):

```powershell
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=LeaveFlow#Dev2026" -p 1433:1433 --name leaveflow-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Mật khẩu này chỉ dùng cho máy local; không đưa vào code hay Git (Ngày 6 sẽ dùng user-secrets:
trong Visual Studio, chuột phải project → **Manage User Secrets**).
Lần sau chỉ cần `docker start leaveflow-sql`.

**Cách B: SQL Server đã cài sẵn trên Windows** (Developer/Express, từ thời làm LinkQ). Dùng luôn,
chỉ cần đổi connection string ở Ngày 3.

**Cách C: LocalDB** (đi kèm workload *Data storage and processing*). Không cần cài thêm gì, connection
string `Server=(localdb)\MSSQLLocalDB;Database=LeaveFlow;Trusted_Connection=True`. Tiện để bắt đầu,
nhưng đến Ngày 16–17 (Testcontainers, docker compose) bạn vẫn cần Docker.

### 2.4 SSMS
Tải **SQL Server Management Studio (SSMS)** từ trang Microsoft Learn. Dùng để xem bảng, chạy script
và xem execution plan (Ctrl+M) ở Ngày 7.
Kết nối Docker: Server `localhost,1433`, SQL Server Authentication, user `sa`, mật khẩu ở trên, tick
*Trust server certificate*. (Trong Visual Studio cũng có **SQL Server Object Explorer** để xem nhanh.)

### 2.5 Tùy chọn
- **Postman** để gọi API và test 2 tab cùng lúc (Ngày 7).
- **Windows Terminal** cho dễ dùng nhiều tab.

## 3. Làm Angular với Visual Studio

Project Angular được tạo bằng Angular CLI (`ng new`, Ngày 8) trong thư mục `src/leaveflow-web`.
Để sửa code trong Visual Studio: **File → Open → Folder…** → chọn `src/leaveflow-web`.
Chạy bằng terminal của Visual Studio: `ng serve`, `ng test`, `ng generate ...`.

Visual Studio xử lý TypeScript tốt, nhưng gợi ý và báo lỗi trong **template HTML của Angular** yếu
hơn VS Code (VS Code có extension *Angular Language Service*). Nếu thấy khó chịu khi viết template,
có thể cài thêm VS Code **chỉ cho phần Angular** (`winget install Microsoft.VisualStudioCode`, rồi
cài extension *Angular Language Service*, *ESLint*, *Prettier*). Hai công cụ mở chung một repo,
không xung đột. Đây chỉ là tùy chọn; làm hoàn toàn trong Visual Studio vẫn được.

## 4. Thao tác Visual Studio hay dùng trong lộ trình

| Việc | Trong Visual Studio | Lệnh tương đương |
|---|---|---|
| Tạo solution + Web API (Ngày 2) | File → New → Project → **ASP.NET Core Web API**, tick **Use controllers**, bật OpenAPI | `dotnet new sln` + `dotnet new webapi --use-controllers` |
| Tạo console app cho lab | File → New → Project → **Console App** | `dotnet new console` |
| Thêm project test (Ngày 15) | Chuột phải solution → Add → New Project → **xUnit Test Project** | `dotnet new xunit` |
| Cài NuGet (EF Core, FluentValidation…) | Chuột phải project → **Manage NuGet Packages** | `dotnet add package ...` |
| Chạy + debug | **F5** (Swagger tự mở) / Ctrl+F5 chạy không debug | `dotnet run` |
| Breakpoint | **F9**; F10 bước qua, F11 bước vào | — |
| Chạy test | **Test → Test Explorer** → Run All | `dotnet test` |
| Migration EF Core (Ngày 3) | Tools → NuGet Package Manager → **Package Manager Console**: `Add-Migration Init`, `Update-Database` | `dotnet ef migrations add Init`, `dotnet ef database update` |
| User secrets (Ngày 6) | Chuột phải project → **Manage User Secrets** | `dotnet user-secrets set ...` |
| Gọi API thử | Mở file `.http` có sẵn trong project → **Send request** | Postman |
| Git | **Git Changes** (Ctrl+0, Ctrl+G), tạo branch, commit, push | `git ...` |
| Docker (Ngày 17) | Chuột phải project → Add → **Docker Support** | viết `Dockerfile` tay |

Gợi ý: làm bằng giao diện Visual Studio cho nhanh, nhưng **mỗi thao tác hãy chạy thử lệnh `dotnet`
tương đương ít nhất một lần** để hiểu Visual Studio đang làm gì phía sau.

## 5. Kiểm tra môi trường

Mở Visual Studio → **View → Terminal** và chạy:

```powershell
git --version
dotnet --version        # 10.x
dotnet --list-sdks
node -v                 # bản LTS
npm -v
ng version
dotnet ef --version
docker ps               # thấy container leaveflow-sql (nếu dùng Docker)
```

### Hello world .NET
1. File → New → Project → **Console App** → tên `HelloDotnet` → Framework **.NET 10**.
2. Đặt breakpoint (F9) ở dòng `Console.WriteLine`, nhấn **F5**: chương trình dừng ở breakpoint.
3. Nhấn F5 lần nữa: cửa sổ console in ra `Hello, World!`.

### Hello world Web API
1. File → New → Project → **ASP.NET Core Web API** → tick **Use controllers** → F5.
2. Trình duyệt mở Swagger (hoặc gọi thử bằng file `.http`), endpoint `WeatherForecast` trả về JSON.

### Hello world Angular

```powershell
cd $HOME\source
ng new hello-ng --defaults
cd hello-ng
ng serve                # mở http://localhost:4200
```

## 6. Lấy project về máy

**Bằng Visual Studio:** màn hình khởi động → **Clone a repository** →
`https://github.com/liem030105-tech/ASP.net.git` → chọn thư mục → Clone.

**Hoặc bằng lệnh:**

```powershell
cd $HOME\source
git clone https://github.com/liem030105-tech/ASP.net.git
```

Cấu hình Git một lần (dùng tên và email GitHub của bạn):

```powershell
git config --global user.name "Ten Cua Ban"
git config --global user.email "email-github@example.com"
git config --global core.autocrlf true
```

## 7. Lỗi hay gặp trên Windows

| Lỗi | Cách sửa |
|---|---|
| Không thấy **.NET 10** trong danh sách Framework khi tạo project | Cập nhật Visual Studio lên bản mới nhất (Visual Studio Installer → Update) |
| `ng : File ...\ng.ps1 cannot be loaded because running scripts is disabled` | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| `dotnet` / `ng` không được nhận ra sau khi cài | Đóng hẳn rồi mở lại terminal và Visual Studio |
| Trình duyệt báo chứng chỉ HTTPS không an toàn | `dotnet dev-certs https --clean` rồi `dotnet dev-certs https --trust` |
| `Add-Migration` báo không tìm thấy lệnh | Cài NuGet `Microsoft.EntityFrameworkCore.Tools` vào project API; chọn đúng *Default project* trong Package Manager Console |
| Container SQL tắt ngay sau khi chạy | Mật khẩu `MSSQL_SA_PASSWORD` chưa đủ mạnh (cần chữ hoa, chữ thường, số, ký tự đặc biệt, ≥ 8 ký tự); xem `docker logs leaveflow-sql` |
| Cổng 1433 đã bị dùng | SQL Server trên Windows đang chạy: dùng Cách B, hoặc đổi thành `-p 1434:1433` và kết nối `localhost,1434` |
| Docker báo cần WSL2 | `wsl --install`, khởi động lại máy, mở lại Docker Desktop |
| `dotnet ef` báo không tìm thấy | `dotnet tool install --global dotnet-ef` rồi mở lại terminal |
