using Microsoft.EntityFrameworkCore;
using SWD1813.Models;

namespace SWD1813.Services.Implementations;

/// <summary>
/// Đảm bảo schema tối thiểu khi DB đã tồn tại nhưng lịch sử migration EF trống/lệch
/// (tránh lỗi "There is already an object named 'users'" khi chạy dotnet ef database update).
/// </summary>
public static class DatabaseSchemaEnsure
{
    /// <summary>Tạo bảng chat_messages + FK nếu chưa có.</summary>
    public static async System.Threading.Tasks.Task EnsureChatMessagesTableAsync(ProjectManagementContext db,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[dbo].[chat_messages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[chat_messages](
                    [message_id] varchar(36) NOT NULL,
                    [project_id] varchar(36) NULL,
                    [user_id] varchar(36) NULL,
                    [content] nvarchar(2000) NOT NULL,
                    [sent_at] datetime NOT NULL,
                    CONSTRAINT [PK__chat_messages] PRIMARY KEY ([message_id])
                );
                CREATE INDEX [IX_chat_messages_project_id] ON [dbo].[chat_messages]([project_id]);
                CREATE INDEX [IX_chat_messages_sent_at] ON [dbo].[chat_messages]([sent_at]);
                CREATE INDEX [IX_chat_messages_user_id] ON [dbo].[chat_messages]([user_id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_chat_messages_projects')
                    ALTER TABLE [dbo].[chat_messages] ADD CONSTRAINT [FK_chat_messages_projects]
                    FOREIGN KEY ([project_id]) REFERENCES [dbo].[projects]([project_id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_chat_messages_users')
                    ALTER TABLE [dbo].[chat_messages] ADD CONSTRAINT [FK_chat_messages_users]
                    FOREIGN KEY ([user_id]) REFERENCES [dbo].[users]([user_id]);
            END
            """,
            cancellationToken);
    }
}
