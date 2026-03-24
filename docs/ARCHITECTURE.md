# Kiến trúc MVC (SWD1813)

## Thư mục chính

| Thư mục | Vai trò |
|--------|---------|
| `Controllers/` | **C** — Nhận HTTP, điều phối; giữ **mỏng** (ủy quyền `Services/`). |
| `Models/` | **M** — Entity EF (`*.cs`), `ViewModels/` (DTO cho View). |
| `Views/` | **V** — Razor; `Shared/_Layout.cshtml`; `Views/<Controller>/<Action>.cshtml`. |
| `Services/Interfaces/` + `Services/Implementations/` | **Nghiệp vụ** — truy vấn DB, quy tắc domain; đăng ký DI trong `Program.cs`. |
| `Hubs/` | SignalR (chat realtime). |
| `Migrations/` | EF Core migrations. |
| `wwwroot/` | CSS/JS/static. |
| `Configuration/` | Options binding (`appsettings.json`). |

## Nguyên tắc

1. **Controller** không chứa SQL dài; dùng `I*Service`.
2. **View** nhận **ViewModel** (`Models/ViewModels/`) thay vì `ViewBag` khi có thể (vd. `ReportsIndexVm`).
3. **DbContext** chỉ inject vào **Services** (hoặc controller rất mỏng nếu chỉ CRUD đơn giản).

## Đã gỡ

- Demo trang mẫu commit (`DemoController`, `GitHubCommitsDemoData`, view `Demo/`) — không còn trong menu; code đã xóa để giảm nhiễu.
