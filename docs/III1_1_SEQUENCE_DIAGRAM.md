# III.1.1 Sequence Diagram

Sơ đồ tuần tự (Sequence Diagram) cho các luồng chính của web **SWD1813 / SWP Tracker** (quản lý dự án, nhóm, task; tích hợp Jira & GitHub). Thể hiện tương tác giữa User, Browser, Controller, Service và Database theo đúng code hiện tại.

---

## 1. Đăng nhập (Login)

**Luồng:** User nhập email, password → Browser POST `/Account/Login` → AccountController gọi AuthService.ValidateUserAsync → Database; nếu hợp lệ thì SignIn (Cookie) và redirect Dashboard.

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant AccountController
    participant AuthService
    participant Database

    User->>Browser: Nhập email, password, Submit
    Browser->>AccountController: POST /Account/Login (email, password)
    AccountController->>AuthService: ValidateUserAsync(email, password)
    AuthService->>Database: Query User by email
    Database-->>AuthService: User or null
    AuthService->>AuthService: Verify password (BCrypt / legacy)
    AuthService-->>AccountController: User or null
    alt Invalid
        AccountController-->>Browser: View Login (error)
        Browser-->>User: Hiển thị lỗi
    else Valid
        AccountController->>AccountController: Build Claims, SignInAsync(Cookie)
        AccountController-->>Browser: Redirect /Dashboard
        Browser-->>User: Dashboard
    end
```

---

## 2. Đăng ký (Register)

**Luồng:** User điền form (email, password, fullName, role) → AccountController validate → AuthService.RegisterAsync (kiểm tra email, BCrypt, lưu User) → SignIn → Redirect Dashboard.

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant AccountController
    participant AuthService
    participant Database

    User->>Browser: Điền form (email, password, fullName, role), Submit
    Browser->>AccountController: POST /Account/Register (RegisterViewModel)
    AccountController->>AccountController: Validate ModelState, Role required
    AccountController->>AuthService: RegisterAsync(model)
    AuthService->>Database: Check email exists?
    Database-->>AuthService: result
    alt Email đã tồn tại
        AuthService-->>AccountController: (null, errorMessage)
        AccountController-->>Browser: View Register (error)
    else OK
        AuthService->>AuthService: BCrypt.HashPassword, create User
        AuthService->>Database: Add User, SaveChanges
        Database-->>AuthService: OK
        AuthService-->>AccountController: (user, null)
        AccountController->>AccountController: Build Claims, SignInAsync
        AccountController-->>Browser: Redirect /Dashboard
        Browser-->>User: Dashboard
    end
```

---

## 3. Tạo nhóm (Create Group) – Admin/Leader

**Luồng:** User (Admin hoặc Leader) nhập tên nhóm → GroupsController.Create POST → GroupService.CreateAsync → Database Insert Group → Redirect /Groups/Index.

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant GroupsController
    participant GroupService
    participant Database

    User->>Browser: Nhập tên nhóm, Submit
    Browser->>GroupsController: POST /Groups/Create (groupName)
    GroupsController->>GroupsController: Validate groupName, Authorize Admin/Leader
    GroupsController->>GroupService: CreateAsync(groupName, createdBy)
    GroupService->>Database: Insert Group
    Database-->>GroupService: OK
    GroupService-->>GroupsController: OK
    GroupsController-->>Browser: Redirect /Groups/Index
    Browser-->>User: Danh sách nhóm
```

---

## 4. Gán giảng viên cho nhóm (Assign Lecturer) – Admin

**Luồng:** Admin chọn nhóm và lecturer → GroupsController.AssignLecturer POST → kiểm tra IsAdmin → GroupService.AssignLecturerAsync → Database Update Group.lecturer_id → Redirect Index.

```mermaid
sequenceDiagram
    actor Admin
    participant Browser
    participant GroupsController
    participant GroupService
    participant Database

    Admin->>Browser: Chọn lecturer, Submit
    Browser->>GroupsController: POST /Groups/AssignLecturer (id, lecturerId)
    GroupsController->>GroupsController: if (!IsAdmin) return Forbid()
    GroupsController->>GroupService: AssignLecturerAsync(groupId, lecturerId)
    GroupService->>Database: Update Group SET lecturer_id
    Database-->>GroupService: OK
    GroupService-->>GroupsController: true
    GroupsController-->>Browser: Redirect /Groups/Index
    Browser-->>Admin: Danh sách nhóm
```

---

## 5. Tạo dự án (Create Project)

**Luồng:** User chọn Group (thuộc quyền), nhập projectName, ngày → ProjectsController.Create POST → GetUserGroupIdsAsync, kiểm tra groupId ∈ groupIds → ProjectService.CreateAsync → Database Insert Project → Redirect /Projects/Index.

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant ProjectsController
    participant GroupService
    participant ProjectService
    participant Database

    User->>Browser: Chọn Group, projectName, dates, Submit
    Browser->>ProjectsController: POST /Projects/Create (projectName, groupId, startDate, endDate)
    ProjectsController->>GroupService: GetUserGroupIdsAsync() (qua helper)
    GroupService->>Database: Query groups/group_members/lecturers by role
    Database-->>GroupService: groupIds
    GroupService-->>ProjectsController: groupIds
    ProjectsController->>ProjectsController: if !groupIds.Contains(groupId) return Forbid()
    ProjectsController->>ProjectService: CreateAsync(projectName, groupId, startDate, endDate)
    ProjectService->>Database: Insert Project
    Database-->>ProjectService: OK
    ProjectsController-->>Browser: Redirect /Projects/Index
    Browser-->>User: Danh sách dự án
```

---

## 6. Tạo task & giao thành viên (Create Task) – Leader

**Luồng:** Leader chọn Project, nhập taskTitle (hoặc chọn Jira Issue), assignedTo, deadline → TasksController.Create POST → kiểm tra CanCreateOrAssignTask → GroupService/ProjectService kiểm tra quyền → TaskService.CreateManualTaskAsync hoặc CreateTaskAsync (JiraIssue + Task) → Redirect /Tasks/Index.

```mermaid
sequenceDiagram
    actor Leader
    participant Browser
    participant TasksController
    participant GroupService
    participant ProjectService
    participant TaskService
    participant Database

    Leader->>Browser: Chọn Project, taskTitle/issueId, assignedTo, deadline, Submit
    Browser->>TasksController: POST /Tasks/Create (issueId?, taskTitle?, projectId, assignedTo, deadline)
    TasksController->>TasksController: if (!CanCreateOrAssignTask) return Forbid()
    TasksController->>TasksController: Validate assignedTo required

    alt Tạo từ Jira Issue (issueId)
        TasksController->>TaskService: CreateTaskAsync(issueId, assignedTo, deadline)
        TaskService->>Database: Get JiraIssue, Insert Task
        Database-->>TaskService: Task
    else Tạo thủ công (taskTitle + projectId)
        TasksController->>GroupService: GetGroupIdsUserParticipatesInAsync(userId, role)
        GroupService->>Database: Query by role
        Database-->>GroupService: groupIds
        GroupService-->>TasksController: groupIds
        TasksController->>ProjectService: GetByIdAsync(projectId)
        ProjectService->>Database: Get Project
        Database-->>ProjectService: Project
        ProjectService-->>TasksController: Project
        TasksController->>TasksController: Check project.GroupId in groupIds
        TasksController->>TaskService: CreateManualTaskAsync(projectId, taskTitle, assignedTo, deadline)
        TaskService->>Database: Insert JiraIssue (MANUAL-xxx), Insert Task
        Database-->>TaskService: Task
    end

    TaskService-->>TasksController: Task
    TasksController-->>Browser: Redirect /Tasks/Index
    Browser-->>Leader: Danh sách task
```

---

## 7. Cập nhật trạng thái task (Update Task Status) – Assignee

**Luồng:** Thành viên được giao task chọn Status (To Do / In Progress / Done) → TasksController.UpdateStatus POST → TaskService.UpdateStatusAsync (chỉ thành công khi task.AssignedTo == currentUserId) → Database Update Task.Status → Redirect /Tasks/Index.

```mermaid
sequenceDiagram
    actor Member
    participant Browser
    participant TasksController
    participant TaskService
    participant Database

    Member->>Browser: Chọn Status (e.g. Done), Submit
    Browser->>TasksController: POST /Tasks/UpdateStatus (taskId, status)
    TasksController->>TaskService: UpdateStatusAsync(taskId, status, currentUserId)
    TaskService->>Database: Find Task by taskId
    Database-->>TaskService: Task
    TaskService->>TaskService: if (AssignedTo != currentUserId) return false
    TaskService->>Database: Update Task.Status, SaveChanges
    Database-->>TaskService: OK
    TaskService-->>TasksController: true/false
    TasksController-->>Browser: Redirect /Tasks/Index
    Browser-->>Member: Danh sách task
```

---

## 8. Kết nối Jira (Connect Jira)

**Luồng:** User mở Project Details → Connect Jira, nhập Jira Project Key và Jira Token → ProjectsController.ConnectJira POST → kiểm tra quyền project → ProjectService.SetJiraProjectKeyAsync, SaveApiIntegrationAsync → Database → Redirect /Projects/Details.

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant ProjectsController
    participant ProjectService
    participant Database

    User->>Browser: Nhập Jira Project Key, Jira Token, Submit
    Browser->>ProjectsController: POST /Projects/ConnectJira (projectId, jiraProjectKey, jiraToken)
    ProjectsController->>ProjectService: GetByIdAsync(projectId)
    ProjectService->>Database: Get Project
    Database-->>ProjectService: Project
    ProjectService-->>ProjectsController: Project
    ProjectsController->>ProjectsController: GetUserGroupIdsAsync(), check project.GroupId in groupIds
    alt Forbid
        ProjectsController-->>Browser: Forbid()
    else OK
        ProjectsController->>ProjectService: SetJiraProjectKeyAsync(projectId, jiraProjectKey)
        ProjectService->>Database: Update Project.jira_project_key
        ProjectsController->>ProjectService: SaveApiIntegrationAsync(projectId, jiraToken, null)
        ProjectService->>Database: Upsert ApiIntegration
        Database-->>ProjectService: OK
        ProjectsController-->>Browser: Redirect /Projects/Details/id
        Browser-->>User: Chi tiết dự án
    end
```

---

## Tổng hợp

| STT | Luồng | Actor | Controller | Service |
|-----|-------|-------|------------|---------|
| 1 | Login | User | AccountController | AuthService |
| 2 | Register | User | AccountController | AuthService |
| 3 | Create Group | Admin/Leader | GroupsController | GroupService |
| 4 | Assign Lecturer | Admin | GroupsController | GroupService |
| 5 | Create Project | User | ProjectsController | GroupService, ProjectService |
| 6 | Create Task | Leader | TasksController | GroupService, ProjectService, TaskService |
| 7 | Update Task Status | Member (Assignee) | TasksController | TaskService |
| 8 | Connect Jira | User | ProjectsController | ProjectService |

Tài liệu và sơ đồ được sinh theo đúng code web hiện tại (AccountController, GroupsController, ProjectsController, TasksController và các service tương ứng).
