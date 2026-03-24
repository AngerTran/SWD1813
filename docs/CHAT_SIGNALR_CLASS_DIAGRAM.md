# Class diagram — Chat realtime (SignalR), theo chuẩn UML (mẫu tham chiếu)

Sơ đồ bên dưới **bám ký hiệu UML** như ảnh mẫu (stereotype, **Attribute** / **Operation**, quan hệ **Generalization**, **Association**, **Aggregation** thoi rỗng, **Composition** thoi đặc, **Dependency** nét đứt, **Note**).

**Ánh xạ Mermaid → UML (giống ảnh mẫu):**

| UML (ảnh mẫu) | Mermaid `classDiagram` |
|---------------|-------------------------|
| Generalization (kế thừa, tam giác rỗng về lớp cha) | `\|&lt;--` |
| Realization (interface ← class) | `\|&lt;..` |
| Association (đường liền) | `-->` |
| Aggregation (“has-a” yếu, **thoi rỗng** phía whole) | `o--` |
| Composition (“has-a” mạnh, **thoi đặc** phía whole) | `*--` |
| Dependency (nét đứt, phụ thuộc) | `..>` |
| Ghi chú (note gấp góc) | `note for Class "..."` |

---

## Sơ đồ lớp (Mermaid — bản đầy đủ)

> Render: VS Code Markdown Preview, GitHub, [mermaid.live](https://mermaid.live).

```mermaid
classDiagram
    direction TB

    class Hub {
        <<framework>>
        +ConnectionId
        +Clients
        +Groups
        +Context
    }

    class ProjectChatHub {
        <<control>>
        -IServiceScopeFactory scopeFactory
        -ILogger logger
        +JoinProjectChat(projectId) void
        +LeaveProjectChat(projectId) void
        +SendChat(projectId, message) void
        +JoinTeamChat(teamId) void
        +LeaveTeamChat(teamId) void
        +SendTeamChat(teamId, message) void
        +JoinPublicChat() void
        +LeavePublicChat() void
        +SendPublicChat(message) void
        +TeamGroupName(teamId)$ string
        +PublicGroupName$ string
    }

    class ChatController {
        <<boundary>>
        -IProjectService projectService
        -IGroupService groupService
        -IChatService chatService
        +Index() IActionResult
        +Project(id) IActionResult
        +MessagesJson(projectId) IActionResult
        +TeamMessagesJson(teamId) IActionResult
        +PublicMessagesJson() IActionResult
    }

    class ChatWidgetViewComponent {
        <<boundary>>
        -IGroupService groupService
        +InvokeAsync() IViewComponentResult
    }

    class ChatService {
        <<control>>
        -ProjectManagementContext context
        -IGroupService groupService
        +MaxContentLength$ int
        +UserCanAccessTeamAsync() bool
        +ResolveTeamIdByProjectAsync() string
        +GetRecentMessagesAsync() List
        +AddMessageAsync() ChatMessageDto
        +AddTeamMessageAsync() ChatMessageDto
        +GetRecentPublicMessagesAsync() List
        +AddPublicMessageAsync() ChatMessageDto
        +UserCanAccessProjectAsync() bool
    }

    class IChatService {
        <<interface>>
        +UserCanAccessTeamAsync(...)
        +ResolveTeamIdByProjectAsync(...)
        +GetRecentMessagesAsync(...)
        +AddMessageAsync(...)
        +GetRecentTeamMessagesAsync(...)
        +AddTeamMessageAsync(...)
        +GetRecentPublicMessagesAsync(...)
        +AddPublicMessageAsync(...)
        +UserCanAccessProjectAsync(...)
    }

    class IGroupService {
        <<interface>>
        +GetGroupIdsUserParticipatesInAsync(...)
        +GetAllAsync(...)
    }

    class IProjectService {
        <<interface>>
        +GetByIdAsync(...)
        +GetAllAsync(...)
    }

    class ProjectManagementContext {
        <<entity>>
        +ChatMessages DbSet
        +SaveChangesAsync() int
    }

    class ChatMessage {
        <<entity>>
        +MessageId string
        +ProjectId string
        +UserId string
        +Content string
        +SentAt DateTime
    }

    class ChatMessageDto {
        <<entity>>
        +MessageId string
        +UserId string
        +DisplayName string
        +Content string
        +SentAt DateTime
    }

    class User {
        <<entity>>
        +UserId string
        +Email string
        +FullName string
    }

    class ChatWidgetVm {
        <<entity>>
        +Teams List
        +CurrentUserId string
    }

    class ChatWidgetTeamOption {
        <<entity>>
        +TeamId string
        +TeamName string
    }

    %% --- Quan hệ theo UML (ảnh mẫu) ---
    Hub <|-- ProjectChatHub : Generalization
    IChatService <|.. ChatService : Realization

    ChatController --> IChatService : Association
    ChatController --> IProjectService : Association
    ChatController --> IGroupService : Association
    ChatWidgetViewComponent --> IGroupService : Association

    ChatService --> IGroupService : Association
    ChatService --> ProjectManagementContext : Association

    ProjectChatHub ..> IChatService : Dependency

    ChatService ..> ChatMessageDto : Dependency
    ChatService ..> ChatMessage : Dependency

    ProjectManagementContext "1" o-- "*" ChatMessage : Aggregation
    ChatMessage "0..*" --> "1" User : Association
    ChatWidgetVm "1" *-- "1..*" ChatWidgetTeamOption : Composition

    note for Hub "Lớp nền ASP.NET Core SignalR (Microsoft.AspNetCore.SignalR.Hub)."
    note for ProjectChatHub "Điều phối join/send; broadcast Clients.Group(team-*, public-community)."
    note for ChatController "MVC: trang chat + API JSON lịch sử."
    note for ChatWidgetViewComponent "Widget layout: danh sách nhóm (team) user tham gia."
```

---

## Giải thích khớp ảnh mẫu (từng thành phần)

| Thành phần ảnh mẫu | Áp dụng trong sơ đồ Chat |
|--------------------|---------------------------|
| **Stereotype** `<<entity>>` | `ChatMessage`, `ChatMessageDto`, `User`, `ProjectManagementContext`, `ChatWidgetVm`, `ChatWidgetTeamOption` (dữ liệu / DTO / EF) |
| **Stereotype** `<<boundary>>` | `ChatController`, `ChatWidgetViewComponent` (HTTP / UI component) |
| **Stereotype** `<<control>>` | `ProjectChatHub`, `ChatService` (điều phối + nghiệp vụ) |
| **Stereotype** `<<interface>>` | `IChatService`, `IGroupService`, `IProjectService` |
| **Generalization** | `Hub` → `ProjectChatHub` (tam giác rỗng về `Hub`) |
| **Realization** | `IChatService` → `ChatService` |
| **Association** (đường liền) | Controller / Service → interface hoặc DbContext |
| **Aggregation** (thoi **rỗng**) | `ProjectManagementContext` chứa nhiều `ChatMessage` (`1` — `*`) |
| **Composition** (thoi **đặc**) | `ChatWidgetVm` gồm các `ChatWidgetTeamOption` |
| **Dependency** (nét đứt) | Hub dùng `IChatService` qua scope; `ChatService` map/sinh `ChatMessage` / DTO |
| **Note** | Ghi chú vai trò `Hub`, Hub con, Controller, ViewComponent |

*Nhánh `ChatMessage` → `User`: association (FK); multiplicity gợi ý 0..* tin trên 1 user (tùy dữ liệu).*

---

## Đăng ký ứng dụng

- `Program.cs`: `AddSignalR()`, `MapHub~ProjectChatHub~("/hubs/projectchat")`.
- DI: `IChatService` → `ChatService` (scoped).

---

## File nguồn

| File |
|------|
| `Hubs/ProjectChatHub.cs` |
| `Controllers/ChatController.cs` |
| `Services/Interfaces/IChatService.cs` |
| `Services/Implementations/ChatService.cs` |
| `Models/ChatMessage.cs`, `Models/User.cs` |
| `Models/ViewModels/ChatMessageDto.cs`, `ChatWidgetVm.cs` |
| `ViewComponents/ChatWidgetViewComponent.cs` |

---

## Sequence (tham khảo — không thuộc class diagram)

```mermaid
sequenceDiagram
    participant C as Browser
    participant H as ProjectChatHub
    participant S as ChatService
    participant DB as ProjectManagementContext

    C->>H: SendChat(projectId, message)
    H->>S: AddTeamMessageAsync
    S->>DB: Save ChatMessage
    DB-->>S: OK
    S-->>H: ChatMessageDto
    H->>C: ReceiveChat
```
