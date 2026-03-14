# Phân tích SRS & Kế hoạch triển khai

## 1. Tóm tắt phân tích SRS

### 1.1 Mục tiêu hệ thống
Hệ thống quản lý dự án phần mềm tích hợp **Jira** (requirements, tasks) và **GitHub** (commits), phục vụ:
- **Admin**: Quản lý nhóm, gán giảng viên
- **Lecturer**: Theo dõi tiến độ project
- **Team Leader**: Quản lý task nhóm
- **Team Member**: Thực hiện task, commit code

### 1.2 Các chức năng chính (Product Functions)
| # | Chức năng | Mô tả ngắn |
|---|-----------|------------|
| 1 | User authentication | Đăng nhập/đăng xuất, session |
| 2 | Group management | CRUD nhóm, gán lecturer, thêm sinh viên |
| 3 | Jira integration | Kết nối project, import/sync issues, backlog, sprint |
| 4 | GitHub integration | Kết nối repo, import commits, lịch sử, phân tích theo member |
| 5 | Requirement management | Quản lý requirement (từ Jira) |
| 6 | Task management | Xem/assign/cập nhật task từ Jira |
| 7 | Commit analysis | Đếm commit, theo dõi theo member, tần suất, báo cáo |
| 8 | Progress dashboard | Task completion, commit stats, contribution, sprint progress |
| 9 | SRS generation | Trích requirement → template SRS, export PDF/DOCX |
| 10 | Report export | Báo cáo (PDF, v.v.) |

### 1.3 Ràng buộc kỹ thuật
- **Database**: SQL Server (SQLms)
- **Backend**: ASP.NET Core MVC
- **API bên ngoài**: Jira Cloud REST API, GitHub REST API
- **Auth**: OAuth hoặc API token

### 1.4 Functional Requirements (FR) – ánh xạ theo module
| Module | FR IDs | Số lượng |
|--------|--------|----------|
| User Authentication | FR1–FR4 | 4 |
| Group Management | FR5–FR9 | 5 |
| Jira Integration | FR10–FR14 | 5 |
| GitHub Integration | FR15–FR18 | 4 |
| Task Management | FR19–FR22 | 4 |
| Commit Analysis | FR23–FR26 | 4 |
| SRS Generation | FR27–FR30 | 4 |
| Progress Dashboard | FR31–FR34 | 4 |
| **Tổng** | | **34 FR** |

### 1.5 Business Rules (BR) – tóm tắt
- **User/Role**: 1 user 1 role; chỉ Admin tạo nhóm, gán lecturer; mỗi nhóm có Team Leader; 1 sinh viên 1 nhóm.
- **Project**: 1 nhóm ↔ 1 Jira project, 1 GitHub repo; Jira/GitHub phải cấu hình trước khi sync.
- **Requirement**: Dạng Jira Issue, có Title, Description, Priority, Status.
- **Task**: Chỉ Team Leader assign; 1 task 1 người; 1 user nhiều task; task có deadline.
- **Commit**: Thuộc repo nhóm, liên kết GitHub account, trong thời gian project; merge commit không tính.
- **Contribution**: Dựa trên commit + task; task Done mới coi hoàn thành; commit nên liên quan Jira; không commit → low contribution.
- **Reporting**: Theo sprint/tuần; commit report theo member; progress report %; export PDF.

**Tài liệu BR đầy đủ (29 rules):** [docs/BUSINESS_RULES.md](BUSINESS_RULES.md).  
**Trong code:** `Constants/BusinessRuleIds.cs`, `Constants/BusinessRuleMessages.cs`, `Models/Validation/BusinessRuleViolation.cs`, `Models/Validation/BusinessRuleResult.cs`.

---

## 2. Trạng thái codebase hiện tại

- **Đã có**: ASP.NET Core 9 MVC, HomeController, layout Bootstrap, routing, logging. **Đã có Models & DbContext:** `ProjectManagementContext`, các entity User, Lecturer, Group, GroupMember, Project, ApiIntegration, Repository, JiraIssue, Task, Commit, ContributorStat, Sprint, Report (xem [docs/DATABASE_MODELS_REFERENCE.md](DATABASE_MODELS_REFERENCE.md)).
- **Chưa có**: Identity/auth (có thể dùng bảng User có sẵn), Services, Jira/GitHub client, config connection string từ appsettings.

→ **Kế hoạch triển khai** dùng đúng Models/Database hiện có; không tạo lại DbContext hay entities. Chi tiết API và từng bước: [docs/API_AND_IMPLEMENTATION_PLAN.md](API_AND_IMPLEMENTATION_PLAN.md).

---

## 3. Kế hoạch triển khai (Implementation Plan)

### Phase 1: Nền tảng (Foundation)

#### 1.1 NuGet packages
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.Extensions.Http` (cho Jira/GitHub API client)
- Thư viện export: `QuestPDF` hoặc `iTextSharp` (PDF), `DocumentFormat.OpenXml` (DOCX)

#### 1.2 Cấu hình
- **appsettings.json**: Connection string SQL Server; Jira (BaseUrl, ApiToken/Email); GitHub (Token, Optional BaseUrl).
- **Options**: `JiraOptions`, `GitHubOptions` (bind từ config).

#### 1.3 Data layer
- **DbContext**: `ApplicationDbContext` kế thừa `IdentityDbContext<User>` (nếu dùng Identity).
- **Entities** (theo Data Dictionary):
  - `User` (mở rộng IdentityUser hoặc bảng riêng với `user_id`, `email`, `password`, `full_name`, `role`).
  - `Group`, `GroupMember`, `Project`, `Repository`, `JiraIssue`, `Task`, `Commit`, `ContributorStat`, `Sprint`, `Report`.
- **Enums**: `UserRole` (Admin, Lecturer, Leader, Member), `GroupMemberRole` (Leader, Member), `IssueType`, v.v.
- Quan hệ: User ↔ GroupMembers ↔ Groups ↔ Project; Project ↔ JiraIssues, Tasks, Repositories; Repository ↔ Commits, ContributorStats; Sprint → Project; Report → Project.
- **Migrations**: Tạo migration đầu, cập nhật khi đổi schema.

#### 1.4 Authentication & Authorization
- **Identity**: Cookie authentication, `AddIdentity<User, IdentityRole>`, đăng ký User store (có role).
- **Roles**: Admin, Lecturer, Leader, Member (có thể dùng Identity roles hoặc claim/custom).
- **FR1–FR4**: Login (email/password), verify credentials, create session, logout.
- **Policy/Requirement**: Role-based (e.g. `[Authorize(Roles = "Admin")]` cho Group management).

---

### Phase 2: Group Management (FR5–FR9)

- **Actor**: Admin.
- **Controllers**: `GroupsController` (CRUD), `GroupMembersController` (add/remove student, assign leader).
- **Services**: `IGroupService`, `GroupService` (create/update/delete group, assign lecturer, add student); áp dụng BR2, BR3, BR4, BR5.
- **Views**: List groups, Create/Edit group, Assign lecturer, Add members; dropdown/user picker cho Lecturer và Student.

---

### Phase 3: Jira Integration (FR10–FR14)

- **Jira API**: Base URL `https://<tenant>.atlassian.net`, auth qua API token (email + token) hoặc OAuth.
- **Endpoints dùng**: `GET /rest/api/3/project`, `GET /rest/api/3/issue`, `GET /rest/api/3/search` (JQL).
- **Services**: `IJiraApiService`, `JiraApiService` (HttpClient, options); DTOs cho project, issue, search result.
- **Chức năng**:
  - **FR10**: Connect Jira project (lưu `jira_project_key` vào `Project`).
  - **FR11**: Import issues (search → map vào `JiraIssue`).
  - **FR12**: Sync tasks (cập nhật từ Jira vào bảng `Task`/`JiraIssue`).
  - **FR13**: View backlog (JQL hoặc dữ liệu đã import).
  - **FR14**: View sprint tasks (Jira Agile API nếu cần: board/sprint).
- **Business rules**: BR6, BR8; validate `jira_project_key` tồn tại trước khi sync.

---

### Phase 4: GitHub Integration (FR15–FR18)

- **GitHub API**: `https://api.github.com`, auth qua Personal Access Token (PAT).
- **Endpoints**: `GET /repos/{owner}/{repo}/commits`, `GET /repos/{owner}/{repo}/contributors`.
- **Services**: `IGitHubApiService`, `GitHubApiService`; DTOs cho commit, contributor.
- **Chức năng**:
  - **FR15**: Connect GitHub repository (lưu `repo_url`, `github_owner`, `repo_name` vào `Repository`).
  - **FR16**: Import commits (fetch commits → parse → lưu `Commit`); BR18, BR19, BR20, BR21 (bỏ merge commit).
  - **FR17**: View commit history (filter theo repo, date, author).
  - **FR18**: Analyze commits per member (map author_email/author_name → User/GroupMember; aggregate).
- **BR7, BR9**: Repo phải có quyền API; validate trước khi sync.

---

### Phase 5: Task Management (FR19–FR22)

- **Actor**: Team Leader.
- **Services**: `ITaskService` (lấy task từ Jira hoặc từ DB đã sync); assign task (FR20) → cập nhật `Task.assigned_to`; update status (FR21) → sync với Jira hoặc chỉ DB; monitor progress (FR22).
- **Controllers**: `TasksController` (list, assign, update status, filter by group/sprint).
- **Views**: Danh sách task, form assign, form cập nhật trạng thái; BR14, BR15, BR16, BR17.

---

### Phase 6: Commit Analysis (FR23–FR26)

- **FR23**: Count commits (theo repo, sprint, member).
- **FR24**: Track commits per member (ContributorStats + Commit).
- **FR25**: Analyze commit frequency (theo thời gian: theo ngày/tuần).
- **FR26**: Generate commit report (theo BR27: theo member; export PDF nếu cần).
- **Services**: `ICommitAnalysisService` (aggregate từ `Commit`, `ContributorStats`); có thể dùng background job để cập nhật `ContributorStats` sau mỗi lần import.

---

### Phase 7: Progress Dashboard (FR31–FR34)

- **FR31**: Task completion rate (Done / Total theo project/sprint).
- **FR32**: Commit statistics (tổng commit, additions/deletions).
- **FR33**: Member contribution (commit count, task done, có thể tính điểm/rank).
- **FR34**: Sprint progress (sprint scope vs completed).
- **Controller**: `DashboardController` (hoặc mở rộng `HomeController` khi đã login).
- **View**: Dashboard với charts (Chart.js hoặc tương đương): task progress, commit stats, member contribution, sprint progress.
- **API/ViewModel**: Cung cấp data từ `TaskService`, `CommitAnalysisService`, Jira (sprint).

---

### Phase 8: SRS Generation (FR27–FR30)

- **FR27**: Extract requirements từ Jira (JiraIssue với type Story/Epic hoặc label “requirement”).
- **FR28**: Convert to SRS template (cấu trúc chuẩn SRS: intro, functional requirements, v.v.).
- **FR29–FR30**: Export PDF, DOCX.
- **Services**: `ISrsGenerationService` (extract → build document model → export); thư viện PDF/DOCX như đã nêu.
- **Controller**: `SrsController` (chọn project → generate → download file).
- **BR10–BR13**: Chỉ lấy issue thỏa requirement (có title, description, priority, status).

---

### Phase 9: Report Export & Polish

- **Report types**: Progress report (BR28: % task), Commit report (BR27), SRS (đã có ở Phase 8).
- **BR26**: Báo cáo theo sprint hoặc tuần.
- **BR29**: Export PDF.
- **Service**: `IReportExportService` (tạo file, lưu path vào `Report`, trả file cho user).
- **Controller**: `ReportsController` (list, generate, download).
- **Data security**: Password hashing (Identity), token không lưu plain text, role-based access, HTTPS (đã có trong template).

---

## 4. Thứ tự triển khai đề xuất (Sprint-style)

| Bước | Nội dung | Ước lượng |
|------|----------|-----------|
| 1 | Phase 1: Packages, DbContext, Entities, Migrations, Identity, Login/Logout | 1–2 sprint |
| 2 | Phase 2: Group CRUD, Assign lecturer, Add members (Admin) | 0.5–1 sprint |
| 3 | Phase 3: Jira client, Connect project, Import/Sync issues, Backlog/Sprint views | 1–2 sprint |
| 4 | Phase 4: GitHub client, Connect repo, Import commits, History & per-member analysis | 1 sprint |
| 5 | Phase 5: Task list, Assign, Update status, Monitor (Team Leader) | 0.5–1 sprint |
| 6 | Phase 6: Commit counts, ContributorStats, Frequency, Commit report | 0.5 sprint |
| 7 | Phase 7: Dashboard UI (charts, task %, commits, contribution, sprint) | 1 sprint |
| 8 | Phase 8: SRS extract + template, Export PDF/DOCX | 1 sprint |
| 9 | Phase 9: Report export (progress, commit), PDF, polish & NFR (performance, security) | 0.5–1 sprint |

---

## 5. Cấu trúc thư mục đề xuất

```
SWD1813/
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs      # Login, Logout
│   ├── GroupsController.cs
│   ├── GroupMembersController.cs
│   ├── ProjectsController.cs     # Jira/GitHub link
│   ├── JiraController.cs         # Connect, Import, Backlog, Sprint
│   ├── GitHubController.cs       # Connect, Import, History
│   ├── TasksController.cs
│   ├── DashboardController.cs
│   ├── SrsController.cs
│   └── ReportsController.cs
├── Models/
│   ├── ProjectManagementContext.cs  (đã có)
│   ├── User.cs, Group.cs, ...       (entities đã có)
│   └── ...
├── Models/
│   ├── Entities/                  # User, Group, Project, ...
│   ├── ViewModels/                # Cho views
│   └── DTOs/                      # Jira, GitHub API DTOs
├── Services/
│   ├── Interfaces/
│   │   ├── IGroupService.cs
│   │   ├── IJiraApiService.cs
│   │   ├── IGitHubApiService.cs
│   │   ├── ITaskService.cs
│   │   ├── ICommitAnalysisService.cs
│   │   ├── ISrsGenerationService.cs
│   │   └── IReportExportService.cs
│   └── Implementations/
├── Views/
│   ├── Account/
│   ├── Groups/
│   ├── Projects/
│   ├── Jira/
│   ├── GitHub/
│   ├── Tasks/
│   ├── Dashboard/
│   ├── Srs/
│   └── Reports/
├── Options/
│   ├── JiraOptions.cs
│   └── GitHubOptions.cs
└── docs/
    └── SRS_ANALYSIS_AND_IMPLEMENTATION_PLAN.md  # file này
```

---

## 6. Giao diện (UI) – Dashboard

Theo 4.1 User Interface:
- **Task progress chart**: Biểu đồ % hoàn thành (theo sprint/project).
- **Commit statistics chart**: Số commit, lines added/deleted theo thời gian hoặc member.
- **Member contribution graph**: Cột/tròn theo từng member (commit, task done).
- **Project status**: Tổng quan trạng thái project (sprint hiện tại, backlog, done).

Có thể dùng **Chart.js** (đã có thể dùng với layout hiện tại) hoặc thư viện tương đương.

---

## 7. Non-Functional Requirements

- **Performance**: Response time < 3s → cache Jira/GitHub response nếu cần; sync nền; index DB phù hợp.
- **Security**: OAuth/API token; mã hóa token trong config; role-based access; HTTPS.
- **Reliability**: Uptime ≥ 99% → hosting ổn định, retry cho API ngoài.
- **Scalability**: Hỗ trợ ~1000 users → stateless app, connection pooling, không block thread khi gọi API.

---

## 8. Tài liệu bổ sung

- **Kế hoạch chi tiết + Đặc tả API từng endpoint:** [docs/API_AND_IMPLEMENTATION_PLAN.md](API_AND_IMPLEMENTATION_PLAN.md) – gồm từng bước triển khai theo phase, danh sách API đầy đủ (route, request/response, FR/BR), và hướng dẫn từng bước làm từng API.

---

## 9. Bước tiếp theo (Next Steps)

1. **Thêm packages** (EF Core, Identity, Http, PDF/DOCX) vào `SWD1813.csproj`.
2. **Tạo Options và config** trong `appsettings.json` cho DB, Jira, GitHub.
3. **Tạo Entities và ApplicationDbContext**, chạy migration đầu tiên.
4. **Cấu hình Identity** và triển khai Login/Logout (FR1–FR4).
5. **Triển khai lần lượt** Phase 2 → 9 theo thứ tự trên, test từng phase (unit/integration test nếu có).

Nếu cần, có thể tách thêm từng phase thành các task nhỏ (user stories) cho từng sprint.
