# Chat realtime (SignalR)

## Chức năng

- Mỗi **dự án** có một phòng chat: `/Chat/Project/{projectId}`.
- Tin nhắn lưu bảng **`chat_messages`**; đồng thời broadcast tới mọi client đang mở trang (nhóm SignalR `project-{projectId}`).
- **Phân quyền:** cùng logic nhóm với chi tiết dự án (`IGroupService.GetGroupIdsUserParticipatesInAsync`).

## File chính

| Thành phần | File |
|------------|------|
| Hub | `Hubs/ProjectChatHub.cs` (`LeaveProjectChat` khi đổi dự án trong widget) |
| Service | `Services/Implementations/ChatService.cs`, `IChatService.cs` |
| Controller | `Controllers/ChatController.cs` (+ `MessagesJson` cho widget) |
| Trang chat đầy đủ | `Views/Chat/Project.cshtml` |
| **Widget góc trái dưới** | `ViewComponents/ChatWidgetViewComponent.cs`, `Views/Shared/Components/ChatWidget/Default.cshtml`, CSS `wwwroot/css/site.css` (`.swd-chat-widget`) |
| Layout | `_Layout.cshtml` — load SignalR + `@await Component.InvokeAsync("ChatWidget")` khi đã đăng nhập |
| Model | `Models/ChatMessage.cs` |
| Đăng ký | `Program.cs` — `AddSignalR()`, `MapHub<ProjectChatHub>("/hubs/projectchat")` |

## Cập nhật database

**Cách 1 (khuyến nghị khi DB đã có sẵn bảng, lịch sử EF trống):** Không cần làm gì — khi chạy app, `DatabaseSchemaEnsure.EnsureChatMessagesTableAsync` trong `Program.cs` sẽ tạo `chat_messages` nếu chưa có.

**Cách 2:** Migration `20260322091245_AddChatMessages` — chỉ dùng `dotnet ef database update` khi `__EFMigrationsHistory` đã đồng bộ với DB (tránh lỗi tạo lại bảng `users`).

```bash
dotnet ef database update --project SWD1813.csproj
```

## Ghi chú

- Client dùng **cookie đăng nhập** (cùng origin) — không cần token riêng cho SignalR.
- Nội dung tối đa **2000** ký tự (`ChatService.MaxContentLength`).
