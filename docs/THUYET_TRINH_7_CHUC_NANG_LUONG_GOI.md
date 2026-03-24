# Bài thuyết trình – 7 luồng chính (SWD1813)

Tài liệu này dùng để thuyết trình đúng **7 luồng chính** của hệ thống (không tính login/logout), trong đó có **Chat realtime**.

---

## I. Kiến trúc tổng quát (MVC + Service)

Luồng xử lý chuẩn:

`View (.cshtml)` -> `Controller` -> `Service Interface` -> `Service Implementation` -> `ProjectManagementContext` -> `SQL Server`

File trục chính:

- `Program.cs`: đăng ký DI, HttpClient, SignalR, hosted services.
- `Models/ProjectManagementContext.cs`: DbContext + mapping entity.
- `Services/Interfaces/*`: hợp đồng nghiệp vụ.
- `Services/Implementations/*`: xử lý nghiệp vụ.
- `Controllers/*`: điều phối request/response.

---

## II. Danh sách 7 luồng chính

1. **Quản lý nhóm** (Group & Member)
2. **Quản lý dự án** (Project lifecycle)
3. **Tích hợp Jira** (Connect + Sync issue)
4. **Tích hợp GitHub** (Connect + Sync commit)
5. **Task** (tạo, giao, cập nhật trạng thái)
6. **Chat realtime** (private team + public community)
7. **SRS & Reports** (sinh tài liệu và quản lý báo cáo)

---

## 1) Quản lý nhóm

### File chính

- Controller: `Controllers/GroupsController.cs`
- Service: `Services/Interfaces/IGroupService.cs`, `Services/Implementations/GroupService.cs`
- Model: `Models/Group.cs`, `Models/GroupMember.cs`, `Models/Lecturer.cs`, `Models/User.cs`

### Luồng gọi class

`Views/Groups/*`
-> `GroupsController` (`Index`, `Create`, `Details`, `AddMember`, `RemoveMember`, `AssignLecturer`)
-> `IGroupService`
-> `GroupService`
-> `ProjectManagementContext`

### Kết quả nghiệp vụ

- Quản lý nhóm và thành viên.
- Làm nền phân quyền cho toàn hệ thống thông qua `group_id`.

---

## 2) Quản lý dự án

### File chính

- Controller: `Controllers/ProjectsController.cs`
- Service: `Services/Interfaces/IProjectService.cs`, `Services/Implementations/ProjectService.cs`
- Model: `Models/Project.cs`

### Luồng gọi class

`Views/Projects/*`
-> `ProjectsController` (`Index`, `Create`, `Edit`, `Details`)
-> `IProjectService`
-> `ProjectService`
-> `ProjectManagementContext`

### Kết quả nghiệp vụ

- Quản lý vòng đời dự án.
- Mỗi dự án liên kết 1 nhóm để kiểm soát quyền truy cập.

---

## 3) Tích hợp Jira

### File chính

- Controller: `Controllers/ProjectsController.cs` (`ConnectJira`, `SyncJira`)
- Service: `Services/Interfaces/IIntegrationSyncService.cs`, `Services/Implementations/IntegrationSyncService.cs`
- Config: `Configuration/IntegrationOptions.cs`, `appsettings*.json` (`Jira`)
- Model: `Models/ApiIntegration.cs`, `Models/JiraIssue.cs`

### Luồng gọi class

`Views/Projects/ConnectJira.cshtml`
-> `ProjectsController.ConnectJira` (lưu project key + token)
-> `ProjectService.SaveApiIntegrationAsync`

`ProjectsController.SyncJira`
-> `IIntegrationSyncService.SyncJiraIssuesAsync`
-> `IntegrationSyncService` gọi Jira API (`/rest/api/3/search/jql`)
-> upsert `jira_issues`

### Ghi chú kỹ thuật

- Đã cập nhật endpoint Jira mới tương thích Cloud.
- Có hỗ trợ auto-sync khi app khởi động (xem `IntegrationAutoSyncHostedService`).

---

## 4) Tích hợp GitHub

### File chính

- Controller: `Controllers/ProjectsController.cs` (`ConnectGitHub`, `SyncGitHub`)
- Service: `Services/Implementations/IntegrationSyncService.cs`
- Helper: `Services/Implementations/GitHubRepoParser.cs`
- Config: `Configuration/IntegrationOptions.cs`, `appsettings*.json` (`GitHub`)
- Model: `Models/Repository.cs`, `Models/Commit.cs`, `Models/ApiIntegration.cs`

### Luồng gọi class

`Views/Projects/ConnectGitHub.cshtml`
-> `ProjectsController.ConnectGitHub` (lưu token + repo URL)
-> `ProjectService.SaveApiIntegrationAsync` + `UpsertGitHubRepositoryAsync`

`ProjectsController.SyncGitHub`
-> `IIntegrationSyncService.SyncGitHubCommitsAsync`
-> `IntegrationSyncService` gọi GitHub API (`repos/{owner}/{repo}/commits`)
-> upsert `commits`

### Ghi chú kỹ thuật

- Có cơ chế fallback khi token lỗi mà repo public.
- Có tùy chọn ưu tiên repo cấu hình (`PreferConfiguredRepoUrl`).

---

## 5) Task

### File chính

- Controller: `Controllers/TasksController.cs`
- Service: `Services/Interfaces/ITaskService.cs`, `Services/Implementations/TaskService.cs`
- Model: `Models/Task.cs`, `Models/JiraIssue.cs`

### Luồng gọi class

`Views/Tasks/Create.cshtml`, `Assign.cshtml`, `Index.cshtml`
-> `TasksController` (`Create`, `Assign`, `UpdateStatus`, `GetIssues`)
-> `ITaskService` + `IProjectService` + `IGroupService`
-> `TaskService`
-> `ProjectManagementContext`

### Kết quả nghiệp vụ

- Tạo task từ Jira issue hoặc thủ công.
- Giao việc và cập nhật trạng thái theo phân quyền.

---

## 6) Chat realtime (SignalR)

### File chính

- Hub: `Hubs/ProjectChatHub.cs`
- Controller: `Controllers/ChatController.cs`
- Service: `Services/Interfaces/IChatService.cs`, `Services/Implementations/ChatService.cs`
- View chính: `Views/Chat/Index.cshtml`, `Views/Chat/Project.cshtml`
- Widget: `ViewComponents/ChatWidgetViewComponent.cs`, `Views/Shared/Components/ChatWidget/Default.cshtml`
- Model: `Models/ChatMessage.cs`

### Luồng gọi class

`Chat UI / Widget`
-> SignalR `ProjectChatHub` (`JoinTeamChat`, `SendTeamChat`, `JoinPublicChat`, `SendPublicChat`)
-> `IChatService`
-> `ChatService`
-> `ProjectManagementContext`

### Mô hình chat hiện tại

- **Private theo Team (GroupId)**: chỉ thành viên nhóm thấy nội dung.
- **Public cộng đồng**: mọi user đăng nhập có thể tham gia.
- Lịch sử chat lấy qua `ChatController.TeamMessagesJson` / `PublicMessagesJson`.

---

## 7) SRS & Reports

### File chính

- SRS Controller: `Controllers/SrsController.cs`
- Reports Controller: `Controllers/ReportsController.cs`
- Service: `Services/Interfaces/ISrsService.cs`, `IReportService.cs`, `IReportContentService.cs`
- Service impl: `SrsService.cs`, `ReportService.cs`, `ReportContentService.cs`
- View: `Views/Srs/*`, `Views/Reports/Index.cshtml`
- ViewModel: `Models/ViewModels/ReportsIndexVm.cs`

### Luồng gọi class

`Views/Srs/Generate`
-> `SrsController.Generate`
-> `ISrsService.GenerateSrsContentAsync`
-> render `ShowSrs`

`SrsController.Download` hoặc `SaveToReportList`
-> `IReportService.RecordAsync` (ghi log báo cáo)

`Views/Reports/Index`
-> `ReportsController.Index`
-> `IReportService.GetIndexAsync`
-> hiển thị danh sách báo cáo theo quyền nhóm

### Kết quả nghiệp vụ

- Sinh tài liệu SRS markdown.
- Tạo/tải báo cáo và quản lý lịch sử báo cáo theo dự án.

---

## III. Auto-sync khi chạy localhost

Đang có sẵn:

- `Services/Implementations/IntegrationAutoSyncHostedService.cs`
- Cấu hình trong `appsettings.Development.json`:
  - `IntegrationAutoSync.Enabled`
  - `StartupDelaySeconds`
  - `SyncJira`, `SyncGitHub`

Luồng:

`App start`
-> `IntegrationAutoSyncHostedService`
-> duyệt project có token/cấu hình
-> gọi `IntegrationSyncService.SyncJiraIssuesAsync` và `SyncGitHubCommitsAsync`.

---

## IV. Phân công 7 người (đề xuất)

| Người | Luồng |
|------|------|
| 1 | Quản lý nhóm |
| 2 | Quản lý dự án |
| 3 | Tích hợp Jira |
| 4 | Tích hợp GitHub |
| 5 | Task |
| 6 | Chat realtime |
| 7 | SRS & Reports |

---

## V. Kết luận

- Tài liệu đã chuẩn hóa theo **7 luồng chính**.
- Có đầy đủ luồng **Chat realtime** (private + public).
- Mỗi luồng đều nêu rõ `View -> Controller -> Service -> DbContext`.
