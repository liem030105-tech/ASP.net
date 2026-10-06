# Cài đặt môi trường (Windows)

Làm hết file này **trước Ngày 1** của [ROADMAP.md](ROADMAP.md). Mất khoảng 1–2 giờ, phần lớn là chờ tải.

## 1. Có bắt buộc dùng VS Code để chạy .NET không?

**Không.** Thứ chạy .NET là **.NET SDK** (lệnh `dotnet`). Editor chỉ để viết code và debug.
Sau khi cài SDK, bạn chạy project bằng `dotnet run` ở bất kỳ terminal nào (PowerShell, CMD,
Windows Terminal).

| Công cụ | Giá | Ưu điểm | Nhược điểm |
|---|---|---|---|
| **VS Code** + C# Dev Kit | Miễn phí | Nhẹ, dùng **một công cụ cho cả .NET lẫn Angular**, terminal tích hợp, giống cách đa số team fullstack làm | Phải cài extension; công cụ refactor/debug kém Visual Studio một chút |
| **Visual Studio Community** | Miễn phí cho cá nhân | Quen thuộc nếu bạn từng làm .NET Framework; debug, profiler, quản lý NuGet rất mạnh | Nặng; làm Angular trong Visual Studio không thoải mái bằng VS Code |
| **JetBrains Rider** | Miễn phí cho mục đích phi thương mại | Refactor và phân tích code rất mạnh | Phải đăng ký tài khoản; tốn RAM |

**Khuyến nghị:** dùng **VS Code cho cả backend và Angular**. Bạn sẽ quen với dòng lệnh `dotnet`
(`new`, `build`, `run`, `test`, `ef`), và người phỏng vấn hay hỏi đúng những lệnh này. Nếu thấy debug
C# trong VS Code khó chịu, mở file `.sln` bằng Visual Studio Community cho backend, Angular vẫn để
VS Code. Hai công cụ dùng chung một thư mục, không xung đột.

## 2. Cài đặt

Mở **PowerShell** (không cần quyền admin, trừ khi winget yêu cầu) và chạy từng dòng:

```powershell
winget install Git.Git
winget install Microsoft.VisualStudioCode
winget install Microsoft.DotNet.SDK.10
winget install OpenJS.NodeJS.LTS
```

Đóng PowerShell, mở lại (để nhận PATH mới), rồi cài tiếp các công cụ dòng lệnh:

```powershell
npm install -g @angular/cli
dotnet tool install --global dotnet-ef
dotnet dev-certs https --trust
```

`dotnet dev-certs https --trust` tạo chứng chỉ HTTPS cho localhost. Bấm **Yes** khi Windows hỏi.

### SQL Server: chọn một trong hai cách

**Cách A (khuyến nghị): Docker.** Giống cách chạy ở Ngày 17 với `docker compose`.

```powershell
wsl --install                      # cần khởi động lại máy nếu chưa có WSL2
winget install Docker.DockerDesktop
```

Mở Docker Desktop một lần, chờ báo "Engine running", rồi chạy SQL Server (viết trên **một dòng**):

```powershell
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=LeaveFlow#Dev2026" -p 1433:1433 --name leaveflow-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Mật khẩu này chỉ dùng cho máy local; không đưa vào code hay Git (Ngày 6 sẽ dùng user-secrets).
Lần sau chỉ cần `docker start leaveflow-sql`.

**Cách B: SQL Server cài sẵn trên Windows.** Nếu máy đã có SQL Server Developer/Express (từ thời
làm LinkQ), dùng luôn, chỉ cần đổi connection string ở Ngày 3.

### SSMS
Tải **SQL Server Management Studio (SSMS)** từ trang Microsoft Learn. Dùng để xem bảng, chạy script
và xem execution plan (Ctrl+M) ở Ngày 7.
Kết nối: Server `localhost,1433`, SQL Server Authentication, user `sa`, mật khẩu ở trên, tick
*Trust server certificate*.

### Tùy chọn
- **Postman** (hoặc dùng extension REST Client trong VS Code) để gọi API.
- **Windows Terminal** cho dễ dùng nhiều tab.

## 3. Extension cho VS Code

Mở VS Code, tab Extensions (Ctrl+Shift+X), tìm và cài:

| Extension | Dùng để |
|---|---|
| C# Dev Kit (Microsoft) | IntelliSense, chạy/debug .NET, Solution Explorer, chạy test |
| Angular Language Service | Gợi ý và báo lỗi trong template Angular |
| ESLint | Kiểm tra lỗi TypeScript |
| Prettier | Format code |
| SQL Server (mssql) | Chạy câu SQL ngay trong VS Code |
| REST Client | Gọi API bằng file `.http` |
| GitLens (tùy chọn) | Xem lịch sử Git từng dòng |

## 4. Kiểm tra môi trường

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

### Hello world .NET (chạy được = môi trường ổn)

```powershell
mkdir $HOME\scratch; cd $HOME\scratch
dotnet new console -o HelloDotnet
cd HelloDotnet
dotnet run              # in ra "Hello, World!"
code .                  # mở thư mục trong VS Code
```

Trong VS Code: mở `Program.cs`, đặt breakpoint (F9), nhấn **F5**, chọn **C#** để debug.

### Hello world Angular

```powershell
cd $HOME\scratch
ng new hello-ng --defaults
cd hello-ng
ng serve                # mở http://localhost:4200
```

## 5. Lấy project về máy

```powershell
cd $HOME\source          # hoặc thư mục bạn hay để code
git clone https://github.com/liem030105-tech/ASP.net.git
cd ASP.net
code .
```

Cấu hình Git một lần (dùng tên và email GitHub của bạn):

```powershell
git config --global user.name "Ten Cua Ban"
git config --global user.email "email-github@example.com"
git config --global core.autocrlf true
```

## 6. Lỗi hay gặp trên Windows

| Lỗi | Cách sửa |
|---|---|
| `ng : File ...\ng.ps1 cannot be loaded because running scripts is disabled` | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| `dotnet` / `ng` không được nhận ra sau khi cài | Đóng hẳn rồi mở lại terminal và VS Code |
| Trình duyệt báo chứng chỉ HTTPS không an toàn | `dotnet dev-certs https --clean` rồi `dotnet dev-certs https --trust` |
| Container SQL tắt ngay sau khi chạy | Mật khẩu `MSSQL_SA_PASSWORD` chưa đủ mạnh (cần chữ hoa, chữ thường, số, ký tự đặc biệt, ≥ 8 ký tự); xem `docker logs leaveflow-sql` |
| Cổng 1433 đã bị dùng | SQL Server trên Windows đang chạy: dùng Cách B, hoặc đổi thành `-p 1434:1433` và kết nối `localhost,1434` |
| Docker báo cần WSL2 | `wsl --install`, khởi động lại máy, mở lại Docker Desktop |
| `dotnet ef` báo không tìm thấy | `dotnet tool install --global dotnet-ef` rồi mở lại terminal |
