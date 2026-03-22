# IV. Design Specification

Tài liệu mô tả thiết kế hệ thống SWP Tracker (Web SWD1813): **IV.1 Integrated Communication Diagrams** và **IV.2 System High-Level Design**.

**Cách xem diagram (hình vẽ):**
- Các khối ` ```mermaid ` trong file này là **code Mermaid**. Để thấy **hình vẽ**, bạn có thể:
  1. **Copy từng khối code** (từ `flowchart` đến hết khối) → dán vào [Mermaid Live Editor](https://mermaid.live) → xem và export PNG/SVG.
  2. Mở file này bằng **VS Code** và cài extension **Mermaid** (Markdown Preview Mermaid Support) → xem Preview (Ctrl+Shift+V) để render diagram.
  3. Đẩy lên **GitHub** rồi mở file .md trên web → GitHub tự render Mermaid.
- Ảnh PNG có sẵn (nếu có): xem ở thư mục `docs/` hoặc `assets/` (file `IV1-Integrated-Communication-Diagram.png`, `IV2-System-High-Level-Design.png`).

---

## IV.1 Integrated Communication Diagrams

Sơ đồ mô tả **luồng tương tác (communication)** giữa các thành phần chính: Actor, View, Controller, Services, Database và hệ thống ngoài (Jira, GitHub). Theo mẫu: actor bên trái, các bước đánh số (1, 2, 3, …) trên từng đường nối, có tiêu đề use case.

**Hình IV.1 – Integrated Communication Diagram (PNG):**

![IV.1 Integrated Communication Diagram](IV1-Integrated-Communication-Diagram.png)

### IV.1.0 Theo mẫu – UC Configure System Integration (User/Admin)

Luồng khi User/Admin cấu hình tích hợp Jira và GitHub cho dự án: mở màn hình Connect Jira / Connect GitHub, gửi thông tin, Controller xác thực và phân quyền, Service kiểm tra và lưu cấu hình, lưu DB, (có thể test kết nối) Jira API / GitHub API.

```mermaid
flowchart LR
    Admin((Admin))
    V[ConnectJiraView\n/ ConnectGitHubView]
    C[ProjectsController]
    A[AuthService]
    R[GroupService\nAuthorize access]
    I[ProjectService]
    T[(ApiIntegration\n/ DB)]
    J[Jira_API]
    G[GitHub_API]

    Admin -->|1 Open Integration Settings| V
    V -->|2 Submit credentials\nprojectId, jiraProjectKey, jiraToken| C
    C -->|3 Authenticate\nsession| A
    C -->|4 Authorize\nproject in user groups| R
    C -->|5 Validate and save| I
    A -->|6 Session valid| C
    I -->|7 Save config\nprojects, api_integrations| T
    I -.->|8 Test connection| J
    I -.->|9 Test connection| G
    I -->|10 Redirect to Details| C
    C -->|11| V
    V -->|12| Admin
```

*Title: **UC Configure System Integration (Admin/User)** – SWP Tracker*

---

### IV.1.0b Theo mẫu – UC Login (Guest)

```mermaid
flowchart LR
    Guest((Guest))
    V[LoginView]
    C[AccountController]
    A[AuthService]
    DB[(Database\nUsers)]

    Guest -->|1 Open Login| V
    V -->|2 Submit email, password| C
    C -->|3 Authenticate| A
    A -->|4 Query User by email| DB
    DB -->|5 User or null| A
    A -->|6 Verify password\nBCrypt| A
    A -->|7 User or null| C
    C -->|8 SignIn Cookie\nor View error| V
    V -->|9 Redirect Dashboard\nor display error| Guest
```

*Title: **UC Login (Guest)** – SWP Tracker*

---

### IV.1.1 Communication – Đăng nhập / Đăng ký

```mermaid
flowchart LR
    subgraph Client
        User((User))
        Browser[Browser]
    end
    subgraph Server
        AC[AccountController]
        AS[AuthService]
    end
    DB[(Database)]

    User -->|1. Submit| Browser
    Browser -->|2. POST Login/Register| AC
    AC -->|3. ValidateUserAsync / RegisterAsync| AS
    AS -->|4. Query / Insert User| DB
    DB -->|5. Result| AS
    AS -->|6. User / Error| AC
    AC -->|7. SignIn / View| Browser
    Browser -->|8. Redirect / Display| User
```

### IV.1.2 Communication – Nhóm (Groups)

```mermaid
flowchart LR
    User((User))
    Browser[Browser]
    GC[GroupsController]
    GS[GroupService]
    DB[(Database)]

    User -->|1| Browser
    Browser -->|2. Request| GC
    GC -->|3. GetAllAsync / CreateAsync / AssignLecturerAsync ...| GS
    GS -->|4. Query / Update| DB
    DB -->|5| GS
    GS -->|6| GC
    GC -->|7. View / Redirect| Browser
    Browser -->|8| User
```

### IV.1.3 Communication – Dự án (Projects) và tích hợp Jira/GitHub

```mermaid
flowchart LR
    User((User))
    Browser[Browser]
    PC[ProjectsController]
    PS[ProjectService]
    DB[(Database)]
    Jira[Jira API]
    GitHub[GitHub API]

    User -->|1| Browser
    Browser -->|2| PC
    PC -->|3. GetUserGroupIdsAsync| GS[GroupService]
    PC -->|4. GetAllAsync / CreateAsync / SetJiraProjectKeyAsync ...| PS
    PS -->|5. Query / Update| DB
    PS -.->|6. Sync issues| Jira
    PS -.->|7. Repo/commits| GitHub
    DB -->|8| PS
    PS -->|9| PC
    PC -->|10| Browser
    Browser -->|11| User
```

### IV.1.4 Communication – Task

```mermaid
flowchart LR
    User((User))
    Browser[Browser]
    TC[TasksController]
    GS[GroupService]
    TS[TaskService]
    DB[(Database)]

    User -->|1| Browser
    Browser -->|2| TC
    TC -->|3. GetGroupIdsUserParticipatesInAsync| GS
    TC -->|4. GetByProjectAsync / CreateTaskAsync / UpdateStatusAsync ...| TS
    TS -->|5. Query / Insert / Update| DB
    DB -->|6| TS
    TS -->|7| TC
    TC -->|8| Browser
    Browser -->|9| User
```

### IV.1.5 Tổng hợp Integrated Communication (một sơ đồ tổng)

```mermaid
flowchart TB
    subgraph Client["Client"]
        User((User))
        Browser[Browser]
    end

    subgraph Presentation["Presentation Layer"]
        AC[AccountController]
        GC[GroupsController]
        PC[ProjectsController]
        TC[TasksController]
        DC[DashboardController]
        RC[ReportsController]
        SC[SrsController]
    end

    subgraph Business["Business Layer - Services"]
        AuthS[AuthService]
        GroupS[GroupService]
        ProjectS[ProjectService]
        TaskS[TaskService]
        DashboardS[DashboardService]
        SrsS[SrsService]
    end

    subgraph Data["Data & External"]
        DB[(SQL Server\nProjectManagementContext)]
        Jira[Jira Cloud API]
        GitHub[GitHub API]
    end

    User <--> Browser
    Browser <--> AC
    Browser <--> GC
    Browser <--> PC
    Browser <--> TC
    Browser <--> DC
    Browser <--> RC
    Browser <--> SC

    AC <--> AuthS
    GC <--> GroupS
    PC <--> GroupS
    PC <--> ProjectS
    TC <--> GroupS
    TC <--> ProjectS
    TC <--> TaskS
    DC <--> DashboardS
    RC <--> ProjectS
    SC <--> SrsS
    SC <--> ProjectS

    AuthS <--> DB
    GroupS <--> DB
    ProjectS <--> DB
    TaskS <--> DB
    DashboardS <--> DB
    SrsS <--> DB
    ProjectS -.-> Jira
    ProjectS -.-> GitHub
```

---

## IV.2 System High-Level Design

Kiến trúc tổng quan hệ thống: các tầng (layer) và thành phần chính.

**Hình IV.2 – System High-Level Design (PNG):**

![IV.2 System High-Level Design](../assets/IV2-System-High-Level-Design.png)

### IV.2.1 Sơ đồ High-Level Design (layers)

```mermaid
flowchart TB
    subgraph User["User / Actor"]
        U((User))
    end

    subgraph Presentation["Presentation Layer"]
        direction TB
        V[Razor Views]
        C[Controllers\nAccount, Groups, Projects, Tasks,\nDashboard, Reports, Srs, Home]
        V <--> C
    end

    subgraph Business["Business Logic Layer"]
        direction TB
        S[Services\nAuthService, GroupService, ProjectService,\nTaskService, DashboardService, SrsService]
    end

    subgraph Data["Data Access & Storage"]
        direction TB
        EF[Entity Framework Core\nDbContext]
        DB[(SQL Server\nDatabase)]
        EF <--> DB
    end

    subgraph External["External Systems"]
        Jira[Jira Cloud API]
        GitHub[GitHub API]
    end

    U <--> Presentation
    Presentation <--> Business
    Business <--> Data
    Business -.-> External
```

### IV.2.2 Sơ đồ High-Level – Component view

```mermaid
flowchart LR
    subgraph SWP["SWP Tracker System"]
        subgraph Web["ASP.NET Core MVC Web"]
            MVC[Controllers + Views]
        end
        subgraph Services["Application Services"]
            Auth[AuthService]
            Group[GroupService]
            Project[ProjectService]
            Task[TaskService]
            Dashboard[DashboardService]
            Srs[SrsService]
        end
        subgraph Data["Data"]
            Context[ProjectManagementContext]
            SQL[(SQL Server)]
        end
        MVC --> Auth
        MVC --> Group
        MVC --> Project
        MVC --> Task
        MVC --> Dashboard
        MVC --> Srs
        Auth --> Context
        Group --> Context
        Project --> Context
        Task --> Context
        Dashboard --> Context
        Srs --> Context
        Context --> SQL
        Project -.-> Jira[Jira API]
        Project -.-> GitHub[GitHub API]
    end

    User((User)) <--> Web
```

### IV.2.3 Bảng mô tả các tầng

| Tầng | Thành phần | Trách nhiệm |
|------|------------|-------------|
| **Presentation** | Razor Views, Controllers | Nhận request từ Browser, gọi Services, trả View/Redirect. |
| **Business Logic** | AuthService, GroupService, ProjectService, TaskService, DashboardService, SrsService | Nghiệp vụ: xác thực, quản lý nhóm/dự án/task, báo cáo, SRS. Gọi DbContext và (khi cần) Jira/GitHub. |
| **Data Access** | ProjectManagementContext (EF Core) | Truy vấn, thêm/sửa/xóa bảng (Users, Groups, Projects, Tasks, JiraIssues, …). |
| **Data Storage** | SQL Server | Lưu trữ dữ liệu. |
| **External** | Jira Cloud API, GitHub API | Cung cấp issues (Jira), repository và commits (GitHub). |

---

## Tóm tắt

- **IV.1 Integrated Communication Diagrams:** Thể hiện luồng tương tác (message/request) giữa User, Browser, Controllers, Services, Database và Jira/GitHub cho từng nhóm chức năng (Login/Register, Groups, Projects, Tasks) và một sơ đồ tổng hợp.
- **IV.2 System High-Level Design:** Thể hiện kiến trúc tầng (Presentation → Business → Data → External) và component chính của hệ thống SWP Tracker.

Có thể copy từng khối Mermaid vào [Mermaid Live Editor](https://mermaid.live) hoặc công cụ hỗ trợ Mermaid để xuất PNG/SVG.
