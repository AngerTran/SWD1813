# Bài thuyết trình – 7 chức năng chính & luồng gọi class (SWD1813)

Tài liệu dùng cho nhóm **7 người**, **không** lấy login/logout, **không** lấy riêng “Quản lý nhóm” và “Quản lý dự án” làm chủ đề (nhưng vẫn **nhắc** khi cần: dữ liệu gắn `group_id` / `project_id`).

---

## Kiến trúc & DI (file .cs gốc)

| Vai trò | File `.cs` |
|--------|------------|
| Khởi động app, đăng ký DI, HttpClient, Options | `Program.cs` |
| Cấu hình Jira/GitHub (đọc từ `appsettings.json`) | `Configuration/IntegrationOptions.cs` (`JiraIntegrationOptions`, `GitHubIntegrationOptions`) |
| DbContext + mapping bảng | `Models/ProjectManagementContext.cs` |
| Entity (bảng) | `Models/User.cs`, `Models/Group.cs`, `Models/GroupMember.cs`, `Models/Project.cs`, `Models/JiraIssue.cs`, `Models/Task.cs`, `Models/Repository.cs`, `Models/Commit.cs`, `Models/ContributorStat.cs`, `Models/ApiIntegration.cs`, `Models/Report.cs`, `Models/Sprint.cs`, `Models/Lecturer.cs` |

**Luồng tổng quát:** `Browser` → **Razor View** (`.cshtml`) → **Controller** (`Controllers/*.cs`) → **Interface** (`Services/Interfaces/*.cs`) → **Implementation** (`Services/Implementations/*.cs`) → **`ProjectManagementContext`** → **SQL Server**.

**Đăng ký DI** (trong `Program.cs`):  
`AddDbContext<ProjectManagementContext>` · `AddScoped<IAuthService, AuthService>` · `IGroupService` · `IProjectService` · `ITaskService` · `IDashboardService` · `ISrsService` · `IIntegrationSyncService` · `AddHttpClient("Jira")` · `AddHttpClient("GitHub")` · `Configure<JiraIntegrationOptions>` · `Configure<GitHubIntegrationOptions>`.

**Cập nhật thực tế code (Jira/GitHub):** Đồng bộ issue/commit **có gọi API** qua `IHttpClientFactory` trong `Services/Implementations/IntegrationSyncService.cs` (Jira: `POST rest/api/3/search`; GitHub: `GET repos/{owner}/{repo}/commits`). Lưu cấu hình vẫn qua `ProjectService` → bảng `api_integrations`, `projects`, `jira_issues`, `commits`, …

---

## 1. Tích hợp Jira (cấu hình + lưu issue vào DB + đồng bộ từ Jira Cloud)

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Projects/ConnectJira.cshtml`, `Views/Projects/Details.cshtml` |
| Controller | `Controllers/ProjectsController.cs` (`ConnectJira` GET/POST, `Details`, `SyncJira` POST) |
| Lưu key/token, query issue trong DB | `Services/Interfaces/IProjectService.cs` → `Services/Implementations/ProjectService.cs` |
| Đồng bộ từ API Jira → `jira_issues` | `Services/Interfaces/IIntegrationSyncService.cs` → `Services/Implementations/IntegrationSyncService.cs` |
| Phân quyền theo nhóm | `Services/Interfaces/IGroupService.cs` → `Services/Implementations/GroupService.cs` |
| Options (BaseUrl, Email) | `Configuration/IntegrationOptions.cs` |
| Entity | `Models/Project.cs`, `Models/ApiIntegration.cs`, `Models/JiraIssue.cs` |
| DbContext | `Models/ProjectManagementContext.cs` |

### Các file `.cs` theo thứ tự luồng (đọc từ trên xuống)

1. **`Program.cs`** — đăng ký `IProjectService`, `IIntegrationSyncService`, `IHttpClientFactory` tên `"Jira"`, bind `JiraIntegrationOptions`.
2. **`Controllers/ProjectsController.cs`** — `ConnectJira` (POST): validate key, `SetJiraProjectKeyAsync`, `SaveApiIntegrationAsync`; `SyncJira` (POST): gọi `IIntegrationSyncService.SyncJiraIssuesAsync`.
3. **`Services/Interfaces/IProjectService.cs`** — hợp đồng: `SetJiraProjectKeyAsync`, `SaveApiIntegrationAsync`, `GetJiraIssuesByProjectAsync`, …
4. **`Services/Implementations/ProjectService.cs`** — EF: `Projects`, `ApiIntegrations`, `JiraIssues`.
5. **`Services/Interfaces/IIntegrationSyncService.cs`** — `SyncJiraIssuesAsync`.
6. **`Services/Implementations/IntegrationSyncService.cs`** — HttpClient + Basic Auth (email + token), `POST rest/api/3/search`, upsert `JiraIssue`.
7. **`Configuration/IntegrationOptions.cs`** — `Jira:BaseUrl`, `Jira:Email`.
8. **`Models/ProjectManagementContext.cs`** — mapping bảng `projects`, `api_integrations`, `jira_issues`.

### Luồng POST lưu Jira (Connect)

```
ProjectsController.ConnectJira [HttpPost]
  → GetByIdAsync / kiểm tra nhóm (GetUserGroupIdsAsync → GroupService)
  → IProjectService.SetJiraProjectKeyAsync
  → IProjectService.SaveApiIntegrationAsync(projectId, jiraToken, null)
  → ProjectService → SaveChangesAsync
  → RedirectToAction Details
```

### Luồng đồng bộ Jira (nút “Đồng bộ Jira”)

```
ProjectsController.SyncJira [HttpPost]
  → IIntegrationSyncService.SyncJiraIssuesAsync(projectId)
  → IntegrationSyncService → HttpClient → Jira API → upsert jira_issues → SaveChangesAsync
```

### Luồng đọc issue cho dropdown tạo Task (AJAX)

```
Controllers/TasksController.cs — GetIssues(projectId)
  → IGroupService + IProjectService.GetByIdAsync (authorize)
  → IProjectService.GetJiraIssuesByProjectAsync(projectId)
  → Json(...)
```

**File bổ sung:** `Controllers/TasksController.cs`, `Views/Tasks/Create.cshtml` (gọi `GetIssues`).

---

## 2. Tích hợp GitHub (token + repo + commit trong DB + đồng bộ từ GitHub API)

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Projects/ConnectGitHub.cshtml`, `Views/Projects/Details.cshtml` |
| Controller | `Controllers/ProjectsController.cs` (`ConnectGitHub`, `SyncGitHub`) |
| Lưu token & upsert repo URL | `IProjectService.cs` → `ProjectService.cs` (`SaveApiIntegrationAsync`, `UpsertGitHubRepositoryAsync`) |
| Parse URL GitHub | `Services/Implementations/GitHubRepoParser.cs` |
| Đồng bộ commit | `IIntegrationSyncService.cs` → `IntegrationSyncService.cs` (`SyncGitHubCommitsAsync`) |
| Options | `Configuration/IntegrationOptions.cs` (`GitHubIntegrationOptions`) |
| Entity | `Models/ApiIntegration.cs`, `Models/Repository.cs`, `Models/Commit.cs`, `Models/ContributorStat.cs` |

### Các file `.cs` theo thứ tự luồng

1. **`Program.cs`** — `AddHttpClient("GitHub")`, `Configure<GitHubIntegrationOptions>`.
2. **`Controllers/ProjectsController.cs`** — `ConnectGitHub` POST: `SaveApiIntegrationAsync`, `UpsertGitHubRepositoryAsync`; `SyncGitHub` POST: `SyncGitHubCommitsAsync`.
3. **`Services/Interfaces/IProjectService.cs`** — `SaveApiIntegrationAsync`, `UpsertGitHubRepositoryAsync`.
4. **`Services/Implementations/ProjectService.cs`** — ghi `api_integrations`, `repositories`.
5. **`Services/Implementations/GitHubRepoParser.cs`** — tách `owner`/`repo` từ URL.
6. **`Services/Interfaces/IIntegrationSyncService.cs`** — `SyncGitHubCommitsAsync`.
7. **`Services/Implementations/IntegrationSyncService.cs`** — Bearer token, `GET repos/{owner}/{repo}/commits`, upsert `commits`.
8. **`Models/ProjectManagementContext.cs`** — mapping `repositories`, `commits`, `contributor_stats`.

### Luồng tóm tắt

```
ConnectGitHub POST → ProjectService (token + repo URL)
SyncGitHub POST → IntegrationSyncService → GitHub API → bảng commits
```

---

## 3. Task – xem & lọc (theo nhóm / dự án)

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Tasks/Index.cshtml` |
| Controller | `Controllers/TasksController.cs` — `Index`, `GetIssues` |
| Service | `Services/Interfaces/ITaskService.cs` → `Services/Implementations/TaskService.cs` |
| Phụ trợ | `IGroupService` / `GroupService`, `IProjectService` / `ProjectService` |
| Entity | `Models/Task.cs`, `Models/JiraIssue.cs`, `Models/Project.cs`, `Models/User.cs` |

### Các file `.cs` theo thứ tự luồng

1. **`Controllers/TasksController.cs`** — `Index(projectId, groupId)`: gọi `GetGroupIdsUserParticipatesInAsync`, `GetAllAsync` (group/project), `GetByProjectAsync` / `GetByGroupAsync` / `GetByProjectIdsAsync`.
2. **`Services/Interfaces/IGroupService.cs`** → **`Services/Implementations/GroupService.cs`** — nhóm user được tham gia.
3. **`Services/Interfaces/IProjectService.cs`** → **`Services/Implementations/ProjectService.cs`** — danh sách project.
4. **`Services/Interfaces/ITaskService.cs`** → **`Services/Implementations/TaskService.cs`** — query `Tasks` + `Include(Issue)`, `Include(AssignedToNavigation)`.
5. **`Models/ProjectManagementContext.cs`** — quan hệ `Task` ↔ `JiraIssue` ↔ `Project`.

**Điểm nhấn:** Task gắn `IssueId`; issue có thể là Jira thật hoặc `MANUAL-*` tạo trong app.

---

## 4. Task – tạo, giao, cập nhật trạng thái

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Tasks/Create.cshtml`, `Views/Tasks/Assign.cshtml`, `Views/Tasks/Index.cshtml` |
| Controller | `Controllers/TasksController.cs` — `Create` GET/POST, `Assign` GET/POST, `UpdateStatus` POST |
| Service | `ITaskService.cs` → `TaskService.cs`; authorize nhóm/project qua `IGroupService`, `IProjectService` |

### Các file `.cs` theo thứ tự luồng

1. **`Controllers/TasksController.cs`** — `CanCreateOrAssignTask` (Leader); `Create`, `Assign`, `UpdateStatus`.
2. **`Services/Implementations/TaskService.cs`** — `CreateTaskAsync`, `CreateManualTaskAsync`, `AssignTaskAsync`, `UpdateStatusAsync` (rule: chỉ assignee đổi status khi có `currentUserId`).
3. **`Models/Task.cs`**, **`Models/JiraIssue.cs`** — quan hệ & khóa ngoại.
4. **`Models/ProjectManagementContext.cs`** — cấu hình FK `tasks` → `jira_issues`, `users`.

### Luồng con (mapping vào cùng file trên)

| Hành động | Controller method | Service method |
|-----------|--------------------|----------------|
| Tạo từ Jira issue | `TasksController.Create` POST | `TaskService.CreateTaskAsync` |
| Tạo thủ công MANUAL-* | `TasksController.Create` POST | `TaskService.CreateManualTaskAsync` |
| Giao / đổi người | `TasksController.Assign` POST | `TaskService.AssignTaskAsync` |
| Đổi trạng thái | `TasksController.UpdateStatus` POST | `TaskService.UpdateStatusAsync` |

**Quy tắc nghiệp vụ chi tiết:** xem thêm `docs/BUSINESS_RULES.md` (nếu có trong repo).

---

## 5. Dashboard – thống kê task, commit, đóng góp thành viên

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Dashboard/Index.cshtml` |
| Controller | `Controllers/DashboardController.cs` — `Index`, `TaskCompletion`, `CommitStats`, `MemberContribution` |
| Service | `Services/Interfaces/IDashboardService.cs` — chứa VM `DashboardTaskCompletionVm`, `DashboardCommitStatsVm`, `DashboardContributionVm` |
| Implementation | `Services/Implementations/DashboardService.cs` |
| Phụ trợ | `IGroupService`, `IProjectService` |

### Các file `.cs` theo thứ tự luồng

1. **`Controllers/DashboardController.cs`** — mọi action đều kiểm tra `project`/`group` thuộc `GetUserGroupIdsAsync`.
2. **`Services/Interfaces/IDashboardService.cs`** — interface + view model.
3. **`Services/Implementations/DashboardService.cs`** — đếm `JiraIssues`, `Commits`, `ContributorStats`, `Tasks`, `GroupMembers`.
4. **`Models/ProjectManagementContext.cs`** — truy vấn các `DbSet` tương ứng.

---

## 6. Reports – xem báo cáo theo dự án được phép

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Reports/Index.cshtml` |
| Controller | `Controllers/ReportsController.cs` — inject **`ProjectManagementContext`** trực tiếp (không có `IReportService`) |
| Phân quyền | `IGroupService` — `GetGroupIdsUserParticipatesInAsync` |
| Entity | `Models/Report.cs`, `Models/Project.cs` |

### Các file `.cs` theo thứ tự luồng

1. **`Controllers/ReportsController.cs`** — `Index`: EF Core `Reports` + `Include(Project)` + filter `allowedProjectIds`.
2. **`Services/Interfaces/IGroupService.cs`** → **`Services/Implementations/GroupService.cs`** — danh sách nhóm hợp lệ.
3. **`Models/ProjectManagementContext.cs`** — `DbSet<Report>`, `DbSet<Project>`.

---

## 7. SRS – xem requirement & sinh tài liệu Markdown

### Vai trò & file chính

| Lớp | File |
|-----|------|
| UI | `Views/Srs/Index.cshtml`, `Views/Srs/Generate.cshtml`, `Views/Srs/ShowSrs.cshtml` |
| Controller | `Controllers/SrsController.cs` — `Index`, `Generate`, `Download` |
| Service | `Services/Interfaces/ISrsService.cs` → `Services/Implementations/SrsService.cs` |
| Phụ trợ | `IGroupService` / `GroupService` |
| Entity | `Project`, `Group`, `Lecturer`, `User`, `JiraIssue`, `Task` |

### Các file `.cs` theo thứ tự luồng

1. **`Controllers/SrsController.cs`** — query `Projects`/`JiraIssues` (một số action), gọi `ISrsService.GenerateSrsContentAsync`, trả `FileResult` khi download.
2. **`Services/Interfaces/ISrsService.cs`** — `GenerateSrsContentAsync`.
3. **`Services/Implementations/SrsService.cs`** — build chuỗi Markdown từ dữ liệu EF.
4. **`Models/ProjectManagementContext.cs`** — Include `Group`, `Lecturer`, `User`, `JiraIssues`, `Tasks`.

---

## Sơ đồ tổng hợp (layer – không login)

```text
Views (*.cshtml)
    → Controllers/ProjectsController.cs | TasksController.cs | DashboardController.cs
      | ReportsController.cs | SrsController.cs
    → Services/Implementations/*Service.cs (qua Interfaces)
    → Models/ProjectManagementContext.cs
    → SQL Server
```

**Luồng chéo nhóm:** `IGroupService` / `GroupService.cs` được gọi từ hầu hết controller trên (`GetGroupIdsUserParticipatesInAsync`, `GetAllAsync`, `GetMembersAsync` trong Task).

**Đồng bộ ngoài:** `IntegrationSyncService.cs` nối **Jira Cloud** và **GitHub API** (đăng ký trong `Program.cs`).

---

## Gợi ý slide (mỗi người 1 chủ đề) — gắn file .cs

1. **Jira:** `ProjectsController.cs` → `ProjectService.cs` + `IntegrationSyncService.cs` → `JiraIssue` / `ProjectManagementContext.cs`.
2. **GitHub:** `ProjectsController.cs` → `ProjectService.cs` + `GitHubRepoParser.cs` + `IntegrationSyncService.cs` → `Repository` / `Commit`.
3. **Task xem:** `TasksController.cs` → `TaskService.cs` + `GroupService.cs`.
4. **Task sửa:** `TasksController.cs` → `TaskService.cs` (`UpdateStatusAsync`, …).
5. **Dashboard:** `DashboardController.cs` → `DashboardService.cs` + `IDashboardService.cs` (VM).
6. **Reports:** `ReportsController.cs` + `ProjectManagementContext.cs` + `GroupService.cs`.
7. **SRS:** `SrsController.cs` → `SrsService.cs`.

---

## Phụ lục: file `.cs` không thuộc 7 chủ đề nhưng cần biết

| File | Ghi chú |
|------|---------|
| `Controllers/AccountController.cs` | Login/Register (ngoài phạm vi 7 chức năng) |
| `Controllers/GroupsController.cs` | Quản lý nhóm (ngoài phạm vi chủ đề chính theo nhóm bạn) |
| `Controllers/HomeController.cs` | Trang chủ |
| `Services/AuthService.cs` + `IAuthService.cs` | Xác thực |
| `Program.cs` | **Luôn nhắc** khi nói DI và tích hợp HTTP |

---

*Tài liệu cập nhật theo codebase SWD1813 (có `IntegrationSyncService`, `GitHubRepoParser`, `Configuration/IntegrationOptions`).*
