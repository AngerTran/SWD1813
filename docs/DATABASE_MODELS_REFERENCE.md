# Tham chiếu Models & Database hiện có

Tài liệu mô tả **lớp Models và DbContext** của dự án, dùng làm nền cho [API_AND_IMPLEMENTATION_PLAN.md](API_AND_IMPLEMENTATION_PLAN.md).

---

## 1. DbContext

- **Tên:** `SWD1813.Models.ProjectManagementContext`
- **Kế thừa:** `Microsoft.EntityFrameworkCore.DbContext`
- **Database:** SQL Server, connection (trong code): `Server=.;Database=swp391_project_management;Trusted_Connection=True;TrustServerCertificate=True`
- **Khuyến nghị:** Đưa connection string vào `appsettings.json` và dùng `optionsBuilder` từ DI (không cấu hình trong `OnConfiguring`).

---

## 2. Các bảng (Entities) và thuộc tính

### 2.1 User (`users`)

| Cột (DB)     | Thuộc tính (C#) | Kiểu     | Ghi chú        |
|--------------|----------------|----------|----------------|
| user_id      | UserId         | string   | PK, max 36     |
| email        | Email          | string   | Unique         |
| password_hash| PasswordHash   | string   |                |
| full_name    | FullName       | string   |                |
| role         | Role           | string   | Admin/Lecturer/Leader/Member |
| created_at   | CreatedAt      | DateTime?|                |

**Navigation:** ContributorStats, GroupMembers, Lecturer (1-1), Reports, Tasks.

---

### 2.2 Lecturer (`lecturers`)

| Cột (DB)    | Thuộc tính (C#) | Kiểu     | Ghi chú        |
|-------------|-----------------|----------|----------------|
| lecturer_id | LecturerId      | string   | PK, max 36     |
| user_id     | UserId          | string?  | FK → User, Unique |
| department  | Department      | string?  |                |
| created_at  | CreatedAt       | DateTime?|                |

**Quan hệ:** 1 User ↔ 1 Lecturer. Gán lecturer cho nhóm = gán **LecturerId** (bản ghi Lecturer), không phải UserId.

**Navigation:** Groups, User.

---

### 2.3 Group (`groups`)

| Cột (DB)    | Thuộc tính (C#) | Kiểu     | Ghi chú        |
|-------------|-----------------|----------|----------------|
| group_id    | GroupId         | string   | PK, max 36     |
| group_name  | GroupName       | string?  |                |
| lecturer_id | LecturerId      | string?  | FK → **Lecturer** |
| created_at  | CreatedAt       | DateTime?|                |

**Lưu ý:** Nhóm không có `project_id` trực tiếp; **Project** có `group_id` → một Group có nhiều Project.

**Navigation:** GroupMembers, Lecturer, Projects.

---

### 2.4 GroupMember (`group_members`)

| Cột (DB)  | Thuộc tính (C#) | Kiểu   | Ghi chú     |
|-----------|-----------------|--------|-------------|
| id        | Id              | string | PK, max 36  |
| group_id  | GroupId         | string?| FK → Group  |
| user_id   | UserId          | string?| FK → User   |
| role      | Role            | string?| Leader/Member |
| joined_at | JoinedAt        | DateTime?|            |

**Navigation:** Group, User.

---

### 2.5 Project (`projects`)

| Cột (DB)          | Thuộc tính (C#) | Kiểu     | Ghi chú      |
|-------------------|-----------------|----------|--------------|
| project_id        | ProjectId       | string   | PK, max 36   |
| project_name      | ProjectName     | string?  |              |
| group_id          | GroupId         | string?  | FK → Group   |
| jira_project_key  | JiraProjectKey  | string?  |              |
| start_date        | StartDate       | DateOnly?|              |
| end_date          | EndDate         | DateOnly?|              |
| created_at        | CreatedAt       | DateTime?|              |

**Navigation:** ApiIntegrations, Group, JiraIssues, Reports, Repositories, Sprints.

---

### 2.6 ApiIntegration (`api_integrations`)

| Cột (DB)       | Thuộc tính (C#) | Kiểu   | Ghi chú    |
|----------------|----------------|--------|------------|
| integration_id | IntegrationId  | string | PK, max 36 |
| project_id     | ProjectId      | string?| FK → Project |
| jira_token    | JiraToken      | string?|            |
| github_token   | GithubToken    | string?|            |
| created_at     | CreatedAt      | DateTime?|           |

**Ý nghĩa:** Mỗi **Project** có thể có một (hoặc nhiều) bản ghi ApiIntegration chứa **JiraToken** và **GithubToken**. Khi gọi Jira/GitHub API theo project, lấy token từ bảng này (theo ProjectId).

---

### 2.7 Repository (`repositories`)

| Cột (DB)      | Thuộc tính (C#) | Kiểu     | Ghi chú    |
|---------------|-----------------|----------|------------|
| repo_id       | RepoId          | string   | PK, max 36 |
| project_id    | ProjectId       | string?  | FK → Project |
| repo_name     | RepoName        | string?  |            |
| repo_url      | RepoUrl         | string?  |            |
| github_owner  | GithubOwner     | string?  |            |
| created_at    | CreatedAt       | DateTime?|            |

**Navigation:** Commits, ContributorStats, Project.

---

### 2.8 JiraIssue (`jira_issues`)

| Cột (DB)   | Thuộc tính (C#) | Kiểu   | Ghi chú   |
|------------|----------------|--------|-----------|
| issue_id   | IssueId         | string | PK, max 50 |
| project_id | ProjectId       | string?| FK → Project |
| issue_key  | IssueKey        | string?|           |
| summary    | Summary         | string?|           |
| description| Description     | string?|           |
| issue_type | IssueType       | string?|           |
| priority   | Priority        | string?|           |
| status     | Status          | string?|           |
| assignee   | Assignee         | string?|           |
| created_at | CreatedAt       | DateTime?|          |
| updated_at | UpdatedAt       | DateTime?|          |

**Navigation:** Project, Tasks.

---

### 2.9 Task (`tasks`)

| Cột (DB)     | Thuộc tính (C#)   | Kiểu     | Ghi chú     |
|--------------|-------------------|----------|-------------|
| task_id      | TaskId            | string   | PK, max 36  |
| issue_id     | IssueId           | string?  | FK → JiraIssue |
| assigned_to  | AssignedTo        | string?  | FK → **User** |
| status       | Status            | string?  |             |
| deadline     | Deadline          | DateOnly?|             |
| progress     | Progress          | int?     |             |

**Navigation:** AssignedToNavigation (User), Issue (JiraIssue).

---

### 2.10 Commit (`commits`)

| Cột (DB)      | Thuộc tính (C#) | Kiểu     | Ghi chú   |
|---------------|-----------------|----------|-----------|
| commit_id     | CommitId        | string   | PK, max 100 |
| repo_id       | RepoId          | string?  | FK → Repository |
| author_name   | AuthorName      | string?  |           |
| author_email  | AuthorEmail     | string?  |           |
| message       | Message         | string?  |           |
| commit_date   | CommitDate      | DateTime?|           |
| files_changed | FilesChanged    | int?     |           |
| additions     | Additions       | int?     |           |
| deletions     | Deletions       | int?     |           |

**Lưu ý:** Bảng hiện không có cột `is_merge`. Có thể suy merge commit từ `Message` (ví dụ chứa "Merge") hoặc thêm cột sau.

**Navigation:** Repo (Repository).

---

### 2.11 ContributorStat (`contributor_stats`)

| Cột (DB)        | Thuộc tính (C#) | Kiểu     | Ghi chú   |
|-----------------|-----------------|----------|-----------|
| stat_id         | StatId          | string   | PK, max 36 |
| user_id         | UserId          | string?  | FK → User |
| repo_id         | RepoId          | string?  | FK → Repository |
| total_commits   | TotalCommits    | int?     |           |
| total_additions | TotalAdditions  | int?     |           |
| total_deletions | TotalDeletions  | int?     |           |
| last_commit     | LastCommit       | DateTime?|           |

**Navigation:** Repo, User.

---

### 2.12 Sprint (`sprints`)

| Cột (DB)    | Thuộc tính (C#) | Kiểu     | Ghi chú   |
|-------------|-----------------|----------|-----------|
| sprint_id   | SprintId        | string   | PK, max 36 |
| project_id  | ProjectId       | string?  | FK → Project |
| sprint_name | SprintName      | string?  |           |
| start_date  | StartDate       | DateOnly?|           |
| end_date    | EndDate         | DateOnly?|           |

**Navigation:** Project.

---

### 2.13 Report (`reports`)

| Cột (DB)      | Thuộc tính (C#)   | Kiểu     | Ghi chú   |
|---------------|-------------------|----------|-----------|
| report_id     | ReportId          | string   | PK, max 36 |
| project_id    | ProjectId         | string?  | FK → Project |
| report_type   | ReportType        | string?  |           |
| generated_by   | GeneratedBy       | string?  | FK → **User** |
| file_url      | FileUrl           | string?  |           |
| generated_at  | GeneratedAt       | DateTime?|           |

**Navigation:** GeneratedByNavigation (User), Project.

---

## 3. Sơ đồ quan hệ (tóm tắt)

```
User (1) ────── (1) Lecturer
  │                    │
  │                    └── Group.LecturerId (nhiều Group)
  │
  ├── GroupMember (n) ─── Group (1)
  │                            │
  │                            └── Project (n)  [GroupId]
  │                                  │
  │                                  ├── ApiIntegration (n) [JiraToken, GithubToken]
  │                                  ├── JiraIssue (n)
  │                                  ├── Repository (n)
  │                                  ├── Sprint (n)
  │                                  └── Report (n)
  │
  ├── Task.AssignedTo ─── JiraIssue (1)
  ├── ContributorStat (n) ─── Repository (1)
  └── Report.GeneratedBy
```

---

## 4. Ánh xạ với Business Rules & API

| BR / Nghiệp vụ        | Áp dụng trên Models hiện có |
|-----------------------|-----------------------------|
| BR2, BR3 – Chỉ Admin tạo nhóm, gán lecturer | Kiểm tra `User.Role == "Admin"`. Gán lecturer = set `Group.LecturerId` = `Lecturer.LecturerId` (không phải UserId). |
| BR4 – Mỗi nhóm có Team Leader | Trong nhóm, ít nhất một `GroupMember` có `Role == "Leader"`. |
| BR5 – Một sinh viên một nhóm | Trước khi thêm: `GroupMembers` không tồn tại bản ghi nào khác có cùng `UserId`. |
| BR6, BR7 – Nhóm liên kết Jira + GitHub | Thể hiện qua **Project**: Project có `JiraProjectKey` và có ít nhất một **Repository**; Project thuộc Group qua `Project.GroupId`. |
| BR8, BR9 – Token trước khi sync | Lấy token từ **ApiIntegration** theo `ProjectId`; nếu null hoặc API lỗi → không sync. |
| Assign task (BR14–BR17) | Dùng **Task**: `AssignedTo` = `User.UserId`, `Deadline` bắt buộc; chỉ user là Leader trong `GroupMember` mới được assign. |
| Commit (BR18–BR21) | **Commit.RepoId** thuộc Repository của Project; lọc theo `Project.StartDate/EndDate`; merge commit suy từ `Message`. |
| Report (BR26–BR29) | **Report**: `ProjectId`, `ReportType`, `GeneratedBy` = UserId, `FileUrl`; tạo báo cáo theo sprint/tuần. |

---

## 5. Khác biệt so với bản plan gốc (đã điều chỉnh)

| Plan gốc              | Models của bạn | Cách dùng |
|-----------------------|----------------|-----------|
| ApplicationDbContext  | **ProjectManagementContext** | Dùng `ProjectManagementContext` cho toàn bộ API. |
| User.Role (enum)      | **User.Role** (string) | So sánh `"Admin"`, `"Lecturer"`, `"Leader"`, `"Member"`. |
| Group.ProjectId       | **Project.GroupId** | Một Group có nhiều Project; nghiệp vụ “1 nhóm 1 Jira + 1 repo” = 1 Project đó có JiraKey + 1 Repository. |
| Gán lecturer = UserId | **Group.LecturerId** = LecturerId | Trước khi gán cần có bản ghi **Lecturer** (UserId đã có). Nếu user chưa là Lecturer thì tạo Lecturer trước hoặc chỉ cho chọn user đã có Lecturer. |
| Token trong config    | **ApiIntegration** (theo Project) | Jira/GitHub token lưu theo **Project** trong `ApiIntegration`; mỗi project có thể token riêng. |
| PK Guid (C#)          | **string (36)** | Tất cả Id dùng string (GUID string); tạo mới dùng `Guid.NewGuid().ToString()`. |
| Commit.IsMerge        | Chưa có cột    | Tạm suy từ `Message` (ví dụ `Message?.Contains("Merge")`); nếu cần có thể thêm cột sau. |

---

## 6. Gợi ý khi viết Service/API

1. **Inject:** `ProjectManagementContext` (và các service khác), không dùng `ApplicationDbContext`.
2. **Id mới:** `Guid.NewGuid().ToString()` cho UserId, GroupId, ProjectId, TaskId, v.v.
3. **Lecturer:** Khi “Assign Lecturer” cho Group: dropdown lấy danh sách **Lecturer** (join User); set `Group.LecturerId = lecturer.LecturerId`.
4. **Jira/GitHub token:** Lấy từ `ApiIntegrations.FirstOrDefault(x => x.ProjectId == projectId)`; nếu null hoặc token rỗng → báo lỗi BR8/BR9.
5. **Project thuộc Group:** List project của nhóm: `Projects.Where(p => p.GroupId == groupId)`. Tạo project mới: gán `Project.GroupId = groupId`.

File [API_AND_IMPLEMENTATION_PLAN.md](API_AND_IMPLEMENTATION_PLAN.md) sẽ tham chiếu tài liệu này và dùng đúng tên entity, thuộc tính, và DbContext trên khi triển khai API.
