/*
================================================================================
SWD1813 — Script SQL Server đầy đủ (schema + bảng __EFMigrationsHistory)
================================================================================
Nguồn: dotnet ef migrations script 0 -o Database/SWD1813_SQLServer_Full.sql --idempotent
Khớp với: ProjectManagementContext + toàn bộ migration trong thư mục Migrations/

Cách dùng:
  1. Tạo database trống (hoặc bỏ comment khối CREATE DATABASE bên dưới rồi sửa tên DB).
  2. Trong SSMS / sqlcmd: chọn đúng database, mở file này, Execute.
  3. Script idempotent: chạy lại an toàn (đã áp dụng migration sẽ bỏ qua).

Cập nhật script sau khi thêm migration mới:
  cd <thư mục project SWD1813>
  dotnet ef migrations script 0 -o Database/SWD1813_SQLServer_Full.sql --idempotent

Connection string mặc định app: xem appsettings.json → DefaultConnection

Dữ liệu mẫu (tách riêng): chạy tiếp file Database/SWD1813_SeedData.sql sau khi schema xong.
================================================================================
*/

/*
-- Tùy chọn: tạo DB mới (sửa tên cho đúng máy bạn)
CREATE DATABASE [swp391_project_management];
GO
USE [swp391_project_management];
GO
*/

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [users] (
        [user_id] varchar(36) NOT NULL,
        [email] varchar(255) NOT NULL,
        [password_hash] varchar(255) NOT NULL,
        [full_name] varchar(255) NOT NULL,
        [role] varchar(20) NOT NULL,
        [created_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__users__B9BE370F99F0A1FB] PRIMARY KEY ([user_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [lecturers] (
        [lecturer_id] varchar(36) NOT NULL,
        [user_id] varchar(36) NULL,
        [department] varchar(255) NULL,
        [created_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__lecturer__D4D1DAB15638DEF7] PRIMARY KEY ([lecturer_id]),
        CONSTRAINT [FK__lecturers__user___59FA5E80] FOREIGN KEY ([user_id]) REFERENCES [users] ([user_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [groups] (
        [group_id] varchar(36) NOT NULL,
        [group_name] varchar(255) NULL,
        [lecturer_id] varchar(36) NULL,
        [created_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__groups__D57795A0A10388A7] PRIMARY KEY ([group_id]),
        CONSTRAINT [FK__groups__lecturer__5DCAEF64] FOREIGN KEY ([lecturer_id]) REFERENCES [lecturers] ([lecturer_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [group_members] (
        [id] varchar(36) NOT NULL,
        [group_id] varchar(36) NULL,
        [user_id] varchar(36) NULL,
        [role] varchar(20) NULL,
        [joined_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__group_me__3213E83FE9BCBDC5] PRIMARY KEY ([id]),
        CONSTRAINT [FK__group_mem__group__619B8048] FOREIGN KEY ([group_id]) REFERENCES [groups] ([group_id]),
        CONSTRAINT [FK__group_mem__user___628FA481] FOREIGN KEY ([user_id]) REFERENCES [users] ([user_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [projects] (
        [project_id] varchar(36) NOT NULL,
        [project_name] varchar(255) NULL,
        [group_id] varchar(36) NULL,
        [jira_project_key] varchar(50) NULL,
        [start_date] date NULL,
        [end_date] date NULL,
        [created_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__projects__BC799E1FC8A80DF7] PRIMARY KEY ([project_id]),
        CONSTRAINT [FK__projects__group___66603565] FOREIGN KEY ([group_id]) REFERENCES [groups] ([group_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [api_integrations] (
        [integration_id] varchar(36) NOT NULL,
        [project_id] varchar(36) NULL,
        [jira_token] nvarchar(max) NULL,
        [github_token] nvarchar(max) NULL,
        [created_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__api_inte__B403D887F56342C5] PRIMARY KEY ([integration_id]),
        CONSTRAINT [FK__api_integ__proje__06CD04F7] FOREIGN KEY ([project_id]) REFERENCES [projects] ([project_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [jira_issues] (
        [issue_id] varchar(50) NOT NULL,
        [project_id] varchar(36) NULL,
        [issue_key] varchar(50) NULL,
        [summary] nvarchar(max) NULL,
        [description] nvarchar(max) NULL,
        [issue_type] varchar(50) NULL,
        [priority] varchar(50) NULL,
        [status] varchar(50) NULL,
        [assignee] varchar(255) NULL,
        [created_at] datetime NULL,
        [updated_at] datetime NULL,
        CONSTRAINT [PK__jira_iss__D6185C3980FAD455] PRIMARY KEY ([issue_id]),
        CONSTRAINT [FK__jira_issu__proje__6D0D32F4] FOREIGN KEY ([project_id]) REFERENCES [projects] ([project_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [reports] (
        [report_id] varchar(36) NOT NULL,
        [project_id] varchar(36) NULL,
        [report_type] varchar(50) NULL,
        [generated_by] varchar(36) NULL,
        [file_url] varchar(500) NULL,
        [generated_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__reports__779B7C589DA9364A] PRIMARY KEY ([report_id]),
        CONSTRAINT [FK__reports__generat__02FC7413] FOREIGN KEY ([generated_by]) REFERENCES [users] ([user_id]),
        CONSTRAINT [FK__reports__project__02084FDA] FOREIGN KEY ([project_id]) REFERENCES [projects] ([project_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [repositories] (
        [repo_id] varchar(36) NOT NULL,
        [project_id] varchar(36) NULL,
        [repo_name] varchar(255) NULL,
        [repo_url] varchar(500) NULL,
        [github_owner] varchar(255) NULL,
        [created_at] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK__reposito__E2D3BC802CCE93E8] PRIMARY KEY ([repo_id]),
        CONSTRAINT [FK__repositor__proje__6A30C649] FOREIGN KEY ([project_id]) REFERENCES [projects] ([project_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [sprints] (
        [sprint_id] varchar(36) NOT NULL,
        [project_id] varchar(36) NULL,
        [sprint_name] varchar(255) NULL,
        [start_date] date NULL,
        [end_date] date NULL,
        CONSTRAINT [PK__sprints__396C18028DF547C0] PRIMARY KEY ([sprint_id]),
        CONSTRAINT [FK__sprints__project__74AE54BC] FOREIGN KEY ([project_id]) REFERENCES [projects] ([project_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [tasks] (
        [task_id] varchar(36) NOT NULL,
        [issue_id] varchar(50) NULL,
        [assigned_to] varchar(36) NULL,
        [status] varchar(20) NULL,
        [deadline] date NULL,
        [progress] int NULL DEFAULT 0,
        CONSTRAINT [PK__tasks__0492148D0EA24106] PRIMARY KEY ([task_id]),
        CONSTRAINT [FK__tasks__assigned___71D1E811] FOREIGN KEY ([assigned_to]) REFERENCES [users] ([user_id]),
        CONSTRAINT [FK__tasks__issue_id__70DDC3D8] FOREIGN KEY ([issue_id]) REFERENCES [jira_issues] ([issue_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [commits] (
        [commit_id] varchar(100) NOT NULL,
        [repo_id] varchar(36) NULL,
        [author_name] varchar(255) NULL,
        [author_email] varchar(255) NULL,
        [message] nvarchar(max) NULL,
        [commit_date] datetime NULL,
        [files_changed] int NULL,
        [additions] int NULL,
        [deletions] int NULL,
        CONSTRAINT [PK__commits__1C807873FB86E5C7] PRIMARY KEY ([commit_id]),
        CONSTRAINT [FK__commits__repo_id__778AC167] FOREIGN KEY ([repo_id]) REFERENCES [repositories] ([repo_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE TABLE [contributor_stats] (
        [stat_id] varchar(36) NOT NULL,
        [user_id] varchar(36) NULL,
        [repo_id] varchar(36) NULL,
        [total_commits] int NULL DEFAULT 0,
        [total_additions] int NULL DEFAULT 0,
        [total_deletions] int NULL DEFAULT 0,
        [last_commit] datetime NULL,
        CONSTRAINT [PK__contribu__B8A52560876E2F63] PRIMARY KEY ([stat_id]),
        CONSTRAINT [FK__contribut__repo___7E37BEF6] FOREIGN KEY ([repo_id]) REFERENCES [repositories] ([repo_id]),
        CONSTRAINT [FK__contribut__user___7D439ABD] FOREIGN KEY ([user_id]) REFERENCES [users] ([user_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_api_integrations_project_id] ON [api_integrations] ([project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_commits_repo_id] ON [commits] ([repo_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_contributor_stats_repo_id] ON [contributor_stats] ([repo_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_contributor_stats_user_id] ON [contributor_stats] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_group_members_group_id] ON [group_members] ([group_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_group_members_user_id] ON [group_members] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_groups_lecturer_id] ON [groups] ([lecturer_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_jira_issues_project_id] ON [jira_issues] ([project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ__lecturer__B9BE370EB3C91E72] ON [lecturers] ([user_id]) WHERE [user_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_projects_group_id] ON [projects] ([group_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_reports_generated_by] ON [reports] ([generated_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_reports_project_id] ON [reports] ([project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_repositories_project_id] ON [repositories] ([project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_sprints_project_id] ON [sprints] ([project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_tasks_assigned_to] ON [tasks] ([assigned_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE INDEX [IX_tasks_issue_id] ON [tasks] ([issue_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__users__AB6E616418C614EB] ON [users] ([email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110041_AddLecturerToProject'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260314110041_AddLecturerToProject', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110520_AddLecturerIdToProjectsTable'
)
BEGIN
    ALTER TABLE [projects] ADD [lecturer_id] varchar(36) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110520_AddLecturerIdToProjectsTable'
)
BEGIN
    CREATE INDEX [IX_projects_lecturer_id] ON [projects] ([lecturer_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110520_AddLecturerIdToProjectsTable'
)
BEGIN
    ALTER TABLE [projects] ADD CONSTRAINT [FK_projects_lecturers_lecturer_id] FOREIGN KEY ([lecturer_id]) REFERENCES [lecturers] ([lecturer_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314110520_AddLecturerIdToProjectsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260314110520_AddLecturerIdToProjectsTable', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322091245_AddChatMessages'
)
BEGIN
    CREATE TABLE [chat_messages] (
        [message_id] varchar(36) NOT NULL,
        [project_id] varchar(36) NULL,
        [user_id] varchar(36) NULL,
        [content] nvarchar(2000) NOT NULL,
        [sent_at] datetime NOT NULL,
        CONSTRAINT [PK__chat_messages] PRIMARY KEY ([message_id]),
        CONSTRAINT [FK_chat_messages_projects] FOREIGN KEY ([project_id]) REFERENCES [projects] ([project_id]),
        CONSTRAINT [FK_chat_messages_users] FOREIGN KEY ([user_id]) REFERENCES [users] ([user_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322091245_AddChatMessages'
)
BEGIN
    CREATE INDEX [IX_chat_messages_project_id] ON [chat_messages] ([project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322091245_AddChatMessages'
)
BEGIN
    CREATE INDEX [IX_chat_messages_sent_at] ON [chat_messages] ([sent_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322091245_AddChatMessages'
)
BEGIN
    CREATE INDEX [IX_chat_messages_user_id] ON [chat_messages] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260322091245_AddChatMessages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260322091245_AddChatMessages', N'9.0.13');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324021853_FixUnicodeNamesNvarchar'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'full_name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [users] ALTER COLUMN [full_name] nvarchar(255) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324021853_FixUnicodeNamesNvarchar'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[projects]') AND [c].[name] = N'project_name');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [projects] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [projects] ALTER COLUMN [project_name] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324021853_FixUnicodeNamesNvarchar'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[groups]') AND [c].[name] = N'group_name');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [groups] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [groups] ALTER COLUMN [group_name] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324021853_FixUnicodeNamesNvarchar'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[commits]') AND [c].[name] = N'author_name');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [commits] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [commits] ALTER COLUMN [author_name] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324021853_FixUnicodeNamesNvarchar'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260324021853_FixUnicodeNamesNvarchar', N'9.0.13');
END;

COMMIT;
GO

