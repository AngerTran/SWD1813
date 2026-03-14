# Kế hoạch triển khai đầy đủ & Đặc tả API chi tiết

Tài liệu bổ sung cho [SRS_ANALYSIS_AND_IMPLEMENTATION_PLAN.md](SRS_ANALYSIS_AND_IMPLEMENTATION_PLAN.md), gồm:
1. **Kế hoạch triển khai đầy đủ** – từng bước theo phase
2. **Đặc tả API** – tất cả endpoint (MVC + JSON API)
3. **Hướng dẫn triển khai từng API** – thứ tự tạo file, validation, test

---

## Dựa trên Models & Database hiện có

**Kế hoạch này dùng đúng lớp Models và DbContext có sẵn trong project.** Tham chiếu đầy đủ: **[docs/DATABASE_MODELS_REFERENCE.md](DATABASE_MODELS_REFERENCE.md)**.

- **DbContext:** `SWD1813.Models.ProjectManagementContext` (không tạo ApplicationDbContext mới).
- **Entities có sẵn:** User, Lecturer, Group, GroupMember, Project, ApiIntegration, Repository, JiraIssue, Task, Commit, ContributorStat, Sprint, Report.
- **Quan hệ:** Project có `GroupId` (một Group nhiều Project); Group có `LecturerId` → **Lecturer** (không trỏ trực tiếp User); token Jira/GitHub lưu theo **Project** trong **ApiIntegration**.
- **Khóa chính:** tất cả dạng `string` (36 ký tự); tạo mới: `Guid.NewGuid().ToString()`.

---

# PHẦN A: KẾ HOẠCH TRIỂN KHAI ĐẦY ĐỦ

## Phase 1: Nền tảng (Foundation)

### Bước 1.1 – NuGet & cấu hình cơ bản
| # | Việc | Chi tiết |
|---|------|----------|
| 1 | Thêm package | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.Extensions.Http`; (Identity tùy chọn nếu dùng cookie auth với User có sẵn) |
| 2 | Thêm package (export) | `QuestPDF` (PDF), `DocumentFormat.OpenXml` (DOCX) |
| 3 | appsettings.json | `ConnectionStrings:DefaultConnection` trỏ DB `swp391_project_management`; section `Jira` (BaseUrl, Email – dùng khi chưa có token theo project); section `GitHub` (ApiBaseUrl). Token theo từng project lưu trong **ApiIntegration** (JiraToken, GithubToken). |
| 4 | Options class | `Options/JiraOptions.cs`, `Options/GitHubOptions.cs`; bind trong Program.cs. Khi gọi API Jira/GitHub theo project: ưu tiên lấy token từ **ApiIntegration** theo ProjectId. |

### Bước 1.2 – Data layer (dùng Models có sẵn)
| # | Việc | Chi tiết |
|---|------|----------|
| 1 | Tham chiếu Models | Dùng **ProjectManagementContext** và các entity trong `Models/`: User, Lecturer, Group, GroupMember, Project, ApiIntegration, Repository, JiraIssue, Task, Commit, ContributorStat, Sprint, Report. Xem [DATABASE_MODELS_REFERENCE.md](DATABASE_MODELS_REFERENCE.md). |
| 2 | Constants / Enums | Tạo constants cho `User.Role`: "Admin", "Lecturer", "Leader", "Member"; `GroupMember.Role`: "Leader", "Member". (Entity dùng string, không cần đổi DB.) |
| 3 | Connection string | Trong **ProjectManagementContext**: bỏ (hoặc override) `OnConfiguring` hard-code; đăng ký DI: `AddDbContext<ProjectManagementContext>(options => options.UseSqlServer(connectionString))` với connection string lấy từ Configuration. |
| 4 | Migration | Database đã có (scaffold từ DB); nếu đổi schema thì tạo migration mới. Không tạo lại bảng. |
| 5 | Commit.IsMerge (tùy chọn) | Bảng **Commit** chưa có cột `is_merge`. BR21: merge commit không tính – tạm suy từ `Message` (ví dụ chứa "Merge"); nếu cần có thể thêm cột sau. |

### Bước 1.3 – Authentication & Authorization
| # | Việc | Chi tiết |
|---|------|----------|
| 1 | Auth với User có sẵn | Dùng bảng **User** (Email, PasswordHash, Role). Cookie authentication: so sánh password hash khi login; lưu UserId + Role vào claim. Hoặc tích hợp Identity với custom User store trỏ vào bảng Users. |
| 2 | Role | So sánh `User.Role` với "Admin", "Lecturer", "Leader", "Member". `[Authorize(Roles = "Admin")]` cho Group management. |
| 3 | AccountController | Login (GET/POST), Logout (POST). FR1–FR4. |
| 4 | Views Account | Login.cshtml (email, password, remember). Redirect sau login theo role. |
| 5 | Pipeline | app.UseAuthentication() trước UseAuthorization(); route /Account/Login, /Account/Logout |

---

## Phase 2: Group Management

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | IGroupService | CreateGroup(name), UpdateGroup(id, name), DeleteGroup(id), **AssignLecturer(groupId, lecturerId)** – gán **Group.LecturerId** = Lecturer.LecturerId (bảng Lecturer), AddMember(groupId, userId, role), RemoveMember(groupId, userId), GetGroups(), GetGroupById(id), GetMembers(groupId). Validate BR2, BR3, BR4, BR5. Id: **string** (Guid.NewGuid().ToString()). |
| 2 | GroupService | Implement IGroupService; khi AddMember kiểm tra user chưa thuộc nhóm khác (BR5) – query **GroupMember**; khi RemoveMember/đổi role kiểm tra nhóm còn ít nhất 1 **GroupMember** có Role = "Leader" (BR4). |
| 3 | GroupsController | [Authorize(Roles="Admin")]. Index (list **Group**), Create GET/POST, Edit GET/POST(id), Delete GET/POST(id), AssignLecturer GET/POST(groupId) – dropdown danh sách **Lecturer** (có thể join User để hiển thị tên). |
| 4 | GroupMembersController | [Authorize(Roles="Admin")]. AddMember GET/POST(groupId), RemoveMember POST(groupId, userId), SetLeader POST(groupId, userId). Đảm bảo BR4. |
| 5 | ViewModels | GroupListVm, GroupCreateVm, GroupEditVm, AssignLecturerVm (LecturerId từ bảng Lecturer), AddMemberVm |
| 6 | Views Groups | Index, Create, Edit, AssignLecturer (chọn Lecturer), (partial) _MemberList |
| 7 | Views GroupMembers | AddMember: dropdown User chưa có trong GroupMember nào (hoặc chưa thuộc nhóm nào), role Leader/Member |

---

## Phase 3: Jira Integration

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | Jira DTOs | JiraProjectDto, JiraIssueDto, JiraSearchResultDto (fields: summary, description, issuetype, priority, status, assignee) |
| 2 | IJiraApiService | GetProjects(), GetProject(key), SearchIssues(jql, maxResults), GetIssue(issueKey). Auth: lấy token từ **ApiIntegration** theo ProjectId (JiraToken); nếu null dùng JiraOptions (global). |
| 3 | JiraApiService | HttpClient BaseAddress từ config; Authorization với token từ ApiIntegration hoặc Options. GET /rest/api/3/project, /rest/api/3/search?jql=... |
| 4 | IProjectService | CreateProject(projectName, groupId, jiraKey?, startDate, endDate) – **Project.GroupId** bắt buộc; UpdateProject, LinkJiraProject(projectId, jiraKey), GetProject(id). ProjectId, GroupId: **string**. |
| 5 | IJiraSyncService | ImportIssues(projectId) – lấy **ApiIntegration** (JiraToken) theo projectId; nếu null/empty → Fail BR8. Gọi JiraApiService.SearchIssues, map vào entity **JiraIssue** (ProjectId, IssueId, IssueKey, Summary, ...), lưu DB. SyncTasks(projectId) – cập nhật status từ Jira. |
| 6 | JiraController | [Authorize]. Connect GET/POST (chọn project, nhập Jira project key + token; lưu/update **ApiIntegration** cho project). ImportIssues POST(projectId). Sync POST(projectId). Backlog GET(projectId). SprintTasks GET(projectId, sprintId?) |
| 7 | ProjectsController | Index (list **Project** theo GroupId hoặc toàn hệ thống), Create (cần GroupId), Edit, LinkJira (lưu JiraProjectKey + JiraToken vào Project + ApiIntegration), LinkGitHub (Phase 4) |
| 8 | ViewModels/Views | Connect Jira (form project key, token); Backlog (table JiraIssue); Sprint tasks (filter by sprint) |

---

## Phase 4: GitHub Integration

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | GitHub DTOs | GitHubCommitDto, GitHubContributorDto (từ API commits, contributors) |
| 2 | IGitHubApiService | GetCommits(owner, repo, since?, until?), GetContributors(owner, repo). Token: lấy từ **ApiIntegration.GithubToken** theo ProjectId (BR9). Header: Authorization: Bearer token, User-Agent. |
| 3 | GitHubApiService | GET /repos/{owner}/{repo}/commits, /repos/{owner}/{repo}/contributors. **Repository** có GithubOwner, RepoName, RepoUrl, ProjectId. |
| 4 | IRepositoryService | CreateRepo(projectId, repoName, repoUrl, githubOwner) – set **Repository.ProjectId**; LinkToProject đã thể hiện qua ProjectId. RepoId: **string** (Guid). |
| 5 | ICommitSyncService | ImportCommits(repoId, projectId) – lấy **Project** (StartDate, EndDate), **Repository** (GithubOwner, RepoName); token từ ApiIntegration. Filter BR20 (CommitDate trong [StartDate, EndDate]); BR21: bỏ commit có Message chứa "Merge" (bảng Commit chưa có IsMerge). Map author_email → **User** (BR19); lưu **Commit** (CommitId, RepoId, AuthorName, AuthorEmail, ...); cập nhật **ContributorStat** (UserId, RepoId, TotalCommits, ...). |
| 6 | GitHubController | [Authorize]. Connect GET/POST (chọn project, repo URL/owner+name; tạo **Repository** + lưu GithubToken vào **ApiIntegration**). ImportCommits POST(repoId). History GET(repoId, from?, to?) |
| 7 | Views | Connect GitHub (form repo URL), Commit history (table **Commit**), Analyze by member (**ContributorStat**) |

---

## Phase 5: Task Management

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | ITaskService | GetTasksByProject(projectId), GetTasksByGroup(groupId) – qua Project.GroupId. AssignTask(taskId, userId) – BR14: chỉ user là **GroupMember** với Role = "Leader" của nhóm (Project → Group); BR15: **Task.AssignedTo** = 1 User (string UserId); UpdateStatus(taskId, status), SetDeadline(taskId, date) – BR17. Entity **Task**: TaskId, IssueId (FK JiraIssue), AssignedTo (FK User), Status, Deadline, Progress. |
| 2 | TaskService | AssignTask: lấy Task → Issue → Project → Group; kiểm tra current user là GroupMember của Group đó và Role = "Leader"; gán Task.AssignedTo = userId (string), Deadline. |
| 3 | TasksController | [Authorize]. Index (filter by groupId/projectId). Assign GET/POST(taskId). UpdateStatus POST(taskId, status). Monitor GET(groupId) – view tiến độ (Task + JiraIssue). |
| 4 | Views | Task list (IssueKey, Summary từ JiraIssue, AssignedToNavigation.FullName, Status, Deadline), Assign form (dropdown User trong nhóm), Update status, Monitor |

---

## Phase 6: Commit Analysis

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | ICommitAnalysisService | GetCommitCount(repoId, from?, to?, userId?), GetCommitsPerMember(repoId, projectId), GetCommitFrequency(repoId, groupByDayOrWeek), GetCommitReportData(repoId, projectId) – theo BR27 (theo member) |
| 2 | CommitAnalysisService | Aggregate từ Commit, ContributorStats; filter BR20 (project date range); exclude merge BR21 |
| 3 | API/Controller | CommitAnalysisController hoặc action trong GitHubController: Count GET, ByMember GET, Frequency GET, ReportData GET (cho export) |

---

## Phase 7: Progress Dashboard

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | IDashboardService | GetTaskCompletionRate(projectId, sprintId?), GetCommitStatistics(projectId), GetMemberContribution(groupId hoặc projectId), GetSprintProgress(projectId, sprintId) |
| 2 | DashboardController | [Authorize]. Index GET – tổng quan (user có thể chọn group/project). API: Dashboard/TaskCompletion, Dashboard/CommitStats, Dashboard/MemberContribution, Dashboard/SprintProgress (trả JSON cho Chart.js) |
| 3 | Views | Dashboard/Index với 4 khu vực: task progress chart, commit stats chart, member contribution graph, project/sprint status. Dùng Chart.js, gọi API JSON |

---

## Phase 8: SRS Generation

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | ISrsGenerationService | ExtractRequirements(projectId) – lấy JiraIssue có type Story/Epic, có Summary, Description, Priority, Status (BR10–BR13). BuildSrsDocument(requirements) – template SRS. ExportPdf(document), ExportDocx(document) |
| 2 | SrsController | [Authorize]. Index GET(projectId). Generate GET(projectId). ExportPdf GET(projectId). ExportDocx GET(projectId) – trả file download |
| 3 | Views | Chọn project → Generate → Download PDF/DOCX |

---

## Phase 9: Report Export & Polish

| # | Việc | Chi tiết |
|---|------|----------|
| 1 | IReportExportService | GenerateProgressReport(projectId, sprintId?, fromWeek?, toWeek?) – BR26, BR28 (% task). GenerateCommitReport(projectId, sprintId?) – BR27. Save report record (Report table), trả file path hoặc stream |
| 2 | ReportsController | [Authorize]. Index (list reports). GenerateProgress POST. GenerateCommit POST. Download GET(reportId). Export PDF BR29 |
| 3 | Views | List reports, Generate (chọn type, sprint/tuần), Download link |

---

# PHẦN B: ĐẶC TẢ API CHI TIẾT

Quy ước:
- **MVC**: route dạng `/Controller/Action`; trả View hoặc Redirect.
- **API JSON**: dùng attribute `[HttpGet]`/`[HttpPost]` với route rõ ràng; trả `JsonResult` hoặc `IActionResult` với object.

---

## 1. Authentication (FR1–FR4)

| Method | Route | Mô tả | Request | Response | BR |
|--------|-------|--------|---------|----------|-----|
| GET | /Account/Login | Form đăng nhập | - | View Login | - |
| POST | /Account/Login | Xử lý đăng nhập | Email, Password, RememberMe | Redirect (returnUrl hoặc Dashboard) | - |
| POST | /Account/Logout | Đăng xuất | - | Redirect /Home | - |

---

## 2. Group Management (FR5–FR9)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /Groups | Danh sách nhóm | - | View Index | FR5 |
| GET | /Groups/Create | Form tạo nhóm | - | View Create | FR5 |
| POST | /Groups/Create | Tạo nhóm | GroupName | Redirect /Groups (hoặc ModelState) | FR5, BR2 |
| GET | /Groups/Edit/{id} | Form sửa nhóm | id (Guid) | View Edit | FR6 |
| POST | /Groups/Edit/{id} | Cập nhật nhóm | id, GroupName | Redirect | FR6 |
| GET | /Groups/Delete/{id} | Xác nhận xóa | id | View Delete | FR7 |
| POST | /Groups/Delete/{id} | Xóa nhóm | id | Redirect | FR7 |
| GET | /Groups/AssignLecturer/{id} | Form gán GV | id (groupId) | View AssignLecturer | FR8, BR3 |
| POST | /Groups/AssignLecturer/{id} | Gán lecturer | id, LecturerId (userId) | Redirect | FR8, BR3 |
| GET | /Groups/AddMember/{id} | Form thêm SV | id (groupId) | View AddMember | FR9, BR5 |
| POST | /Groups/AddMember/{id} | Thêm thành viên | id, UserId, Role (Leader/Member) | Redirect | FR9, BR4, BR5 |
| POST | /GroupMembers/Remove | Xóa thành viên | GroupId, UserId | Redirect | BR4 (còn ít nhất 1 Leader) |
| POST | /GroupMembers/SetLeader | Chỉ định Leader | GroupId, UserId | Redirect | BR4 |

---

## 3. Project & Jira (FR10–FR14)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /Projects | Danh sách project | - | View | - |
| GET | /Projects/Create | Form tạo project | GroupId? | View | BR6 |
| POST | /Projects/Create | Tạo project | ProjectName, GroupId, JiraProjectKey?, StartDate, EndDate | Redirect | BR6 |
| GET | /Projects/Edit/{id} | Form sửa | id | View | - |
| POST | /Projects/Edit/{id} | Cập nhật project | id, ... | Redirect | - |
| GET | /Jira/Connect/{projectId} | Form kết nối Jira | projectId | View | FR10, BR8 |
| POST | /Jira/Connect | Kết nối Jira | ProjectId, JiraProjectKey | Redirect | FR10, BR6, BR8 |
| POST | /Jira/ImportIssues/{projectId} | Import issues | projectId | Redirect + message | FR11, BR8 |
| POST | /Jira/Sync/{projectId} | Sync tasks từ Jira | projectId | Redirect | FR12, BR8 |
| GET | /Jira/Backlog/{projectId} | Xem backlog | projectId | View + danh sách issues | FR13 |
| GET | /Jira/SprintTasks/{projectId} | Task theo sprint | projectId, SprintId? | View | FR14 |
| GET | /api/Jira/Issues | API danh sách issues (JSON) | projectId, sprintId? | JSON List\<JiraIssueDto\> | FR13, FR14 |

---

## 4. GitHub (FR15–FR18)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /GitHub/Connect/{projectId} | Form kết nối repo | projectId | View | FR15, BR7, BR9 |
| POST | /GitHub/Connect | Kết nối repo | ProjectId, RepoUrl (hoặc Owner, RepoName) | Redirect | FR15, BR7, BR9 |
| POST | /GitHub/ImportCommits/{repoId} | Import commits | repoId, ProjectId? | Redirect | FR16, BR18–BR21 |
| GET | /GitHub/History/{repoId} | Lịch sử commit | repoId, From?, To?, Author? | View | FR17 |
| GET | /api/GitHub/Commits | API danh sách commits (JSON) | repoId, from?, to? | JSON List\<CommitVm\> | FR17 |
| GET | /api/GitHub/Contributors | Phân tích theo member (JSON) | repoId, projectId? | JSON List\<ContributorStatVm\> | FR18 |

---

## 5. Task Management (FR19–FR22)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /Tasks | Danh sách task | groupId?, projectId?, status? | View | FR19 |
| GET | /Tasks/Assign/{taskId} | Form assign | taskId | View | FR20, BR14, BR15 |
| POST | /Tasks/Assign | Gán task | TaskId, UserId (assignedTo), Deadline | Redirect | FR20, BR14, BR15, BR17 |
| POST | /Tasks/UpdateStatus | Cập nhật trạng thái | TaskId, Status | Redirect hoặc JSON | FR21 |
| GET | /Tasks/Monitor | Theo dõi tiến độ | groupId | View | FR22 |
| GET | /api/Tasks/List | API danh sách task (JSON) | projectId, groupId?, status? | JSON List\<TaskVm\> | FR19 |

---

## 6. Commit Analysis (FR23–FR26)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /api/CommitAnalysis/Count | Đếm commit | repoId, from?, to?, userId? | JSON { count } | FR23 |
| GET | /api/CommitAnalysis/ByMember | Commit theo member | repoId, projectId? | JSON List\<MemberCommitVm\> | FR24, BR27 |
| GET | /api/CommitAnalysis/Frequency | Tần suất commit | repoId, groupBy=day\|week, from?, to? | JSON (labels, data) | FR25 |
| GET | /CommitAnalysis/Report | Trang báo cáo commit | repoId, projectId? | View | FR26 |
| GET | /CommitAnalysis/ExportPdf | Export PDF commit report | repoId, projectId? | File PDF | FR26, BR29 |

---

## 7. Dashboard (FR31–FR34)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /Dashboard | Trang dashboard | - | View (chọn group/project) | - |
| GET | /api/Dashboard/TaskCompletion | % task hoàn thành | projectId, sprintId? | JSON { total, done, percentage } | FR31 |
| GET | /api/Dashboard/CommitStats | Thống kê commit | projectId | JSON { totalCommits, totalAdditions, totalDeletions } | FR32 |
| GET | /api/Dashboard/MemberContribution | Đóng góp theo member | groupId hoặc projectId | JSON List\<ContributionVm\> | FR33, BR22, BR25 |
| GET | /api/Dashboard/SprintProgress | Tiến độ sprint | projectId, sprintId | JSON { sprintName, total, done, percentage } | FR34 |

---

## 8. SRS Generation (FR27–FR30)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /Srs/Index | Chọn project, xem requirements | projectId? | View | FR27 |
| GET | /Srs/Generate/{projectId} | Tạo SRS (preview) | projectId | View hoặc JSON | FR27, FR28 |
| GET | /Srs/ExportPdf/{projectId} | Export SRS PDF | projectId | File PDF | FR29, BR29 |
| GET | /Srs/ExportDocx/{projectId} | Export SRS DOCX | projectId | File DOCX | FR30 |

---

## 9. Reports (BR26–BR29)

| Method | Route | Mô tả | Request | Response | FR/BR |
|--------|-------|--------|---------|----------|--------|
| GET | /Reports | Danh sách báo cáo | - | View | - |
| GET | /Reports/Create | Form tạo báo cáo | - | View (chọn type, sprint/tuần) | BR26 |
| POST | /Reports/Generate | Tạo báo cáo | ReportType (Progress/Commit), ProjectId, SprintId?, FromWeek?, ToWeek? | Redirect + lưu Report | BR26, BR27, BR28 |
| GET | /Reports/Download/{id} | Tải file báo cáo | id (reportId) | File PDF | BR29 |

---

# PHẦN C: CÁC BƯỚC LÀM TỪNG API CỤ THỂ

## C.1 Chuẩn chung cho mọi API

1. **Model/ViewModel**: Tạo DTO hoặc ViewModel cho request/response (trong `Models/ViewModels` hoặc `Models/DTOs`).
2. **Service**: Khai báo interface trong `Services/Interfaces`, implement trong `Services/Implementations`. Inject vào Controller.
3. **Validation**: Trong service hoặc FluentValidation: kiểm tra BR (dùng `BusinessRuleIds`, `BusinessRuleViolation`, `BusinessRuleResult`). Trả lỗi rõ (RuleId + Message).
4. **Controller**: Gọi service; nếu validation fail trả View với ModelState hoặc JSON `{ success: false, message, ruleId }`. Success: Redirect hoặc JSON.
5. **View** (nếu MVC): Binding model, hiển thị validation message, form post đúng route.

---

## C.2 Ví dụ từng bước: API Group (Create, AssignLecturer, AddMember)

**Dùng entity:** `Group` (GroupId, GroupName, LecturerId → Lecturer), `GroupMember` (Id, GroupId, UserId, Role), `Lecturer` (LecturerId, UserId). **DbContext:** `ProjectManagementContext`. Id: **string** (Guid.NewGuid().ToString()).

### Bước 1 – ViewModels
- `GroupCreateVm`: GroupName (required, max length, unique qua service).
- `AssignLecturerVm`: GroupId, **LecturerId** (dropdown từ bảng **Lecturer** – có thể join User để hiển thị tên).
- `AddMemberVm`: GroupId, UserId (dropdown users chưa có trong **GroupMember** với nhóm khác – BR5), Role (Leader/Member). Validate BR4: nếu role=Member và nhóm chưa có Leader thì không cho thêm Member trước (hoặc bắt chọn Leader trước).

### Bước 2 – IGroupService
- `Task<BusinessRuleResult> CreateGroupAsync(string groupName)` → BR2: kiểm tra current user role Admin trong controller (Authorize). Tạo **Group** với GroupId = Guid.NewGuid().ToString().
- `Task<BusinessRuleResult> AssignLecturerAsync(string groupId, string lecturerId)` → BR3: kiểm tra Admin trong controller. Gán **Group.LecturerId** = lecturerId (là Lecturer.LecturerId, không phải UserId).
- `Task<BusinessRuleResult> AddMemberAsync(string groupId, string userId, string role)` → kiểm tra **GroupMember** không tồn tại bản ghi nào khác có cùng UserId (BR5); nếu role=Member và nhóm chưa có GroupMember nào Role="Leader" thì Fail BR4; insert **GroupMember** (Id, GroupId, UserId, Role).

### Bước 3 – GroupService implementation
- Inject **ProjectManagementContext**. CreateGroupAsync: check GroupName unique; insert Group; return Success.
- AssignLecturerAsync: update Group.LecturerId = lecturerId; return Success.
- AddMemberAsync: query GroupMembers.Where(m => m.UserId == userId && m.GroupId != groupId); nếu Any() return Fail(BR5); nếu role=Member và !GroupMembers.Any(m => m.GroupId == groupId && m.Role == "Leader") return Fail(BR4); insert GroupMember.

### Bước 4 – GroupsController
- Create POST: nhận GroupCreateVm; gọi _groupService.CreateGroupAsync; nếu !result.IsValid thì ModelState.AddModelError("", result.ToErrorMessage()); return View(vm); else RedirectToAction("Index").
- AssignLecturer POST: AssignLecturerVm (LecturerId); _groupService.AssignLecturerAsync(groupId, vm.LecturerId); tương tự.
- AddMember POST: AddMemberVm; _groupService.AddMemberAsync; tương tự.

### Bước 5 – Views
- Create: form GroupName, anti-forgery, submit POST /Groups/Create.
- AssignLecturer: dropdown LecturerId (danh sách từ **Lecturer**), submit POST /Groups/AssignLecturer/{id}.
- AddMember: dropdown UserId (users chưa thuộc nhóm nào), Radio Leader/Member, submit POST /Groups/AddMember/{id}.

---

## C.3 Ví dụ từng bước: API Jira (Connect, ImportIssues)

**Dùng entity:** `Project` (ProjectId, GroupId, JiraProjectKey), `JiraIssue`, `ApiIntegration` (ProjectId, JiraToken). **DbContext:** `ProjectManagementContext`. Id: **string**.

### Bước 1 – DTOs
- `JiraConnectRequest`: ProjectId, JiraProjectKey, JiraToken (lưu vào **ApiIntegration**).
- `JiraSearchResultDto`: issues array; mỗi issue: key, fields.summary, fields.description, fields.issuetype.name, fields.priority.name, fields.status.name.

### Bước 2 – IJiraApiService
- Token lấy từ **ApiIntegration** theo ProjectId (JiraToken). Nếu null dùng config global.
- `Task<List<JiraProjectDto>> GetProjectsAsync(string? token = null)` → GET /rest/api/3/project.
- `Task<bool> ValidateProjectKeyAsync(string key, string? token)` → GET /rest/api/3/project/{key} hoặc search 1 issue.
- `Task<List<JiraIssueDto>> SearchIssuesAsync(string jql, int maxResults, string? token)` → GET /rest/api/3/search?jql=...

### Bước 3 – IJiraSyncService
- `Task<BusinessRuleResult> ImportIssuesAsync(string projectId)` → BR8: lấy **ApiIntegration** theo projectId (JiraToken); nếu null/empty return Fail(BR8). Lấy Project.JiraProjectKey; gọi JiraApiService.SearchIssuesAsync("project=" + key, token); map sang entity **JiraIssue** (IssueId, ProjectId, IssueKey, Summary, Description, IssueType, Priority, Status, ...); Upsert vào DB.

### Bước 4 – JiraController
- Connect POST: JiraConnectRequest; _projectService.UpdateJiraProjectKey(projectId, request.JiraProjectKey); tạo hoặc cập nhật **ApiIntegration** (ProjectId, JiraToken). Có thể gọi _jiraApiService.ValidateProjectKeyAsync trước.
- ImportIssues POST(projectId): _jiraSyncService.ImportIssuesAsync(projectId); return Redirect + TempData message.

### Bước 5 – View Connect
- Form: ProjectId (hidden), JiraProjectKey (text), JiraToken (text). Submit POST /Jira/Connect. Sau khi connect có thể redirect đến ImportIssues hoặc Backlog.

---

## C.4 Ví dụ từng bước: API GitHub (ImportCommits, BR18–BR21)

**Entity:** `Repository`, `Commit`, `ContributorStat`, `Project`, `ApiIntegration` (GithubToken). Merge commit: suy từ `Message` (bảng Commit chưa có IsMerge).

### Bước 1 – IGitHubApiService
- `Task<List<GitHubCommitDto>> GetCommitsAsync(string owner, string repo, DateTime? since, DateTime? until, int perPage = 100)`.
- Mỗi commit: sha, commit.message, commit.author.name, commit.author.date; parents (length > 1 → merge); có thể gọi thêm API để lấy stats (additions, deletions) nếu cần.

### Bước 2 – ICommitSyncService
- `Task<BusinessRuleResult> ImportCommitsAsync(string repoId, string projectId)`.
  - Lấy Repo (GithubOwner, RepoName), Project (StartDate, EndDate). BR18: repo phải thuộc project (đã link). BR9: gọi API thử trước.
  - Gọi GetCommitsAsync(owner, repo, project.StartDate, project.EndDate). BR20: filter commit_date trong [StartDate, EndDate] (API có thể dùng since/until).
  - BR21: bỏ commit có parents.Length > 1 hoặc message.StartsWith("Merge").
  - Map author email/name → User (BR19): tìm User bằng email hoặc GitHub username; nếu không có thì AuthorName/AuthorEmail vẫn lưu, ContributorStat có thể để null userId hoặc “unknown”.
  - Insert **Commit** (CommitId=sha, RepoId, ...). Cập nhật **ContributorStat** (UserId, RepoId, TotalCommits, ...). Token từ **ApiIntegration.GithubToken** theo projectId (BR9).

### Bước 3 – GitHubController
- ImportCommits POST(repoId): body có ProjectId; _commitSyncService.ImportCommitsAsync(repoId, projectId); return Redirect + message.

---

## C.5 Ví dụ từng bước: API Dashboard (JSON cho Chart.js)

**Dùng entity:** `Task`, `JiraIssue`, `Commit`, `Repository`, `Project`, `ContributorStat`, `GroupMember`. **DbContext:** `ProjectManagementContext`. Id: **string**.

### Bước 1 – IDashboardService
- `Task<TaskCompletionVm> GetTaskCompletionRateAsync(string projectId, string? sprintId)` → đếm **Task** (hoặc **JiraIssue**) total và status=Done; tính percentage.
- `Task<CommitStatsVm> GetCommitStatisticsAsync(string projectId)` → từ **Commit** join **Repository** (ProjectId); sum commits, additions, deletions.
- `Task<List<ContributionVm>> GetMemberContributionAsync(string groupId)` → từ **GroupMember**, **ContributorStat** (theo Repo của Project thuộc Group), **Task** (Done); list member với totalCommits, tasksDone; BR25: totalCommits=0 → flag LowContribution.

### Bước 2 – DashboardController
- `[HttpGet]` `/api/Dashboard/TaskCompletion?projectId=...&sprintId=...` → gọi service, return Json(vm).
- Tương tự cho CommitStats, MemberContribution, SprintProgress.

### Bước 3 – View Dashboard/Index
- Script: fetch `/api/Dashboard/TaskCompletion?projectId=...` → vẽ Chart.js (doughnut hoặc bar). Lặp cho các endpoint còn lại.

---

## C.6 Thứ tự triển khai API đề xuất (theo dependency)

1. **Auth** (Account/Login, Logout) – không phụ thuộc module khác.
2. **Groups** (CRUD, AssignLecturer, AddMember) – cần User, Role.
3. **Projects** (Create, Edit) – cần Group; **Jira Connect** – cần Project.
4. **Jira** (ImportIssues, Sync, Backlog, SprintTasks) – cần Project + JiraApiService.
5. **GitHub Connect**, **ImportCommits**, **History**, **Contributors** – cần Project, Repository.
6. **Tasks** (List, Assign, UpdateStatus, Monitor) – cần JiraIssue, GroupMember.
7. **CommitAnalysis** (Count, ByMember, Frequency, Report) – cần Commit, ContributorStats.
8. **Dashboard** (các API JSON) – cần Task, Commit, ContributorStats.
9. **SRS** (Generate, ExportPdf, ExportDocx) – cần JiraIssue.
10. **Reports** (Generate, Download) – dùng lại logic Progress/Commit, lưu Report.

---

# PHỤ LỤC: Bảng tổng hợp API (quick reference)

| # | Module | Method | Route | Mô tả ngắn |
|---|--------|--------|-------|------------|
| 1 | Auth | GET | /Account/Login | Form login |
| 2 | Auth | POST | /Account/Login | Xử lý login |
| 3 | Auth | POST | /Account/Logout | Logout |
| 4 | Groups | GET | /Groups | List nhóm |
| 5 | Groups | GET/POST | /Groups/Create | Tạo nhóm |
| 6 | Groups | GET/POST | /Groups/Edit/{id} | Sửa nhóm |
| 7 | Groups | GET/POST | /Groups/Delete/{id} | Xóa nhóm |
| 8 | Groups | GET/POST | /Groups/AssignLecturer/{id} | Gán lecturer |
| 9 | Groups | GET/POST | /Groups/AddMember/{id} | Thêm member |
| 10 | GroupMembers | POST | /GroupMembers/Remove | Xóa member |
| 11 | GroupMembers | POST | /GroupMembers/SetLeader | Chỉ định Leader |
| 12 | Projects | GET | /Projects | List project |
| 13 | Projects | GET/POST | /Projects/Create | Tạo project |
| 14 | Projects | GET/POST | /Projects/Edit/{id} | Sửa project |
| 15 | Jira | GET/POST | /Jira/Connect, /Jira/Connect/{projectId} | Kết nối Jira |
| 16 | Jira | POST | /Jira/ImportIssues/{projectId} | Import issues |
| 17 | Jira | POST | /Jira/Sync/{projectId} | Sync Jira |
| 18 | Jira | GET | /Jira/Backlog/{projectId} | Backlog |
| 19 | Jira | GET | /Jira/SprintTasks/{projectId} | Sprint tasks |
| 20 | Jira API | GET | /api/Jira/Issues | JSON issues |
| 21 | GitHub | GET/POST | /GitHub/Connect, /GitHub/Connect/{projectId} | Kết nối repo |
| 22 | GitHub | POST | /GitHub/ImportCommits/{repoId} | Import commits |
| 23 | GitHub | GET | /GitHub/History/{repoId} | Lịch sử commit |
| 24 | GitHub API | GET | /api/GitHub/Commits | JSON commits |
| 25 | GitHub API | GET | /api/GitHub/Contributors | JSON contributors |
| 26 | Tasks | GET | /Tasks | List task |
| 27 | Tasks | GET/POST | /Tasks/Assign, /Tasks/Assign/{taskId} | Assign task |
| 28 | Tasks | POST | /Tasks/UpdateStatus | Cập nhật status |
| 29 | Tasks | GET | /Tasks/Monitor | Monitor tiến độ |
| 30 | Tasks API | GET | /api/Tasks/List | JSON tasks |
| 31 | CommitAnalysis | GET | /api/CommitAnalysis/Count | Đếm commit |
| 32 | CommitAnalysis | GET | /api/CommitAnalysis/ByMember | Theo member |
| 33 | CommitAnalysis | GET | /api/CommitAnalysis/Frequency | Tần suất |
| 34 | CommitAnalysis | GET | /CommitAnalysis/Report | Trang report |
| 35 | CommitAnalysis | GET | /CommitAnalysis/ExportPdf | Export PDF |
| 36 | Dashboard | GET | /Dashboard | Trang dashboard |
| 37 | Dashboard API | GET | /api/Dashboard/TaskCompletion | JSON task % |
| 38 | Dashboard API | GET | /api/Dashboard/CommitStats | JSON commit stats |
| 39 | Dashboard API | GET | /api/Dashboard/MemberContribution | JSON contribution |
| 40 | Dashboard API | GET | /api/Dashboard/SprintProgress | JSON sprint |
| 41 | SRS | GET | /Srs/Index | Chọn project |
| 42 | SRS | GET | /Srs/Generate/{projectId} | Generate SRS |
| 43 | SRS | GET | /Srs/ExportPdf/{projectId} | Export PDF |
| 44 | SRS | GET | /Srs/ExportDocx/{projectId} | Export DOCX |
| 45 | Reports | GET | /Reports | List reports |
| 46 | Reports | GET/POST | /Reports/Create, /Reports/Generate | Tạo báo cáo |
| 47 | Reports | GET | /Reports/Download/{id} | Tải file |

---

Kết thúc tài liệu. Khi triển khai, làm lần lượt Phase 1 → 9 và với mỗi API tuân thủ C.1–C.6; tham chiếu [BUSINESS_RULES.md](BUSINESS_RULES.md) cho từng BR.
