/*
================================================================================
SWD1813 — Dữ liệu mẫu (SQL Server) cho SSMS
================================================================================
Chạy SAU khi đã tạo schema: Database/SWD1813_SQLServer_Full.sql (tách riêng) hoặc dotnet ef database update.

Đặc điểm:
  - Tạo / bổ sung user theo email nếu chưa có (mật khẩu plain: hash123 — giống seed app).
  - Tạo nhóm, thành viên, dự án, repo, Jira issue mẫu, task, commit mẫu, 1 dòng api_integrations (token NULL).
  - Có thể chạy lại: bỏ qua phần đã tồn tại (theo project_id / group_id / email).

Trước khi chạy: sửa USE [tên_database] cho đúng máy bạn.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- USE [swp391_project_management];
-- GO

BEGIN TRY
    BEGIN TRAN;

    /* ========== 1) Users (theo email; không trùng UQ users.email) ========== */
    DECLARE @u_admin    varchar(36), @u_lecturer varchar(36), @u_leader varchar(36), @u_member varchar(36);
    DECLARE @u_thoi     varchar(36), @u_m14 varchar(36), @u_anger varchar(36), @u_m21 varchar(36);

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'admin@system.com')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000001', N'admin@system.com', N'hash123', N'System Admin', N'ADMIN', GETUTCDATE());
    SELECT @u_admin = [user_id] FROM [users] WHERE [email] = N'admin@system.com';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'lecturer@university.edu')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000002', N'lecturer@university.edu', N'hash123', N'Dr Nguyen Van A', N'LECTURER', GETUTCDATE());
    SELECT @u_lecturer = [user_id] FROM [users] WHERE [email] = N'lecturer@university.edu';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'leader@student.edu')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000003', N'leader@student.edu', N'hash123', N'Tran Leader', N'LEADER', GETUTCDATE());
    SELECT @u_leader = [user_id] FROM [users] WHERE [email] = N'leader@student.edu';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'member@student.edu')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000004', N'member@student.edu', N'hash123', N'Nguyen Member', N'MEMBER', GETUTCDATE());
    SELECT @u_member = [user_id] FROM [users] WHERE [email] = N'member@student.edu';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'thoitnse180471@fpt.edu.vn')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000005', N'thoitnse180471@fpt.edu.vn', N'hash123', N'Trần Ngọc Thái', N'MEMBER', GETUTCDATE());
    SELECT @u_thoi = [user_id] FROM [users] WHERE [email] = N'thoitnse180471@fpt.edu.vn';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'member14@student.edu')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000006', N'member14@student.edu', N'hash123', N'Member 14', N'MEMBER', GETUTCDATE());
    SELECT @u_m14 = [user_id] FROM [users] WHERE [email] = N'member14@student.edu';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'anger@student.edu')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000007', N'anger@student.edu', N'hash123', N'Anger Tran', N'MEMBER', GETUTCDATE());
    SELECT @u_anger = [user_id] FROM [users] WHERE [email] = N'anger@student.edu';

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [email] = N'member21@student.edu')
        INSERT INTO [users] ([user_id], [email], [password_hash], [full_name], [role], [created_at])
        VALUES (N'a0000001-0000-0000-0000-000000000008', N'member21@student.edu', N'hash123', N'Member 21', N'MEMBER', GETUTCDATE());
    SELECT @u_m21 = [user_id] FROM [users] WHERE [email] = N'member21@student.edu';

    /* ========== 2) Lecturer (1 dòng gắn user giảng viên) ========== */
    DECLARE @lec_id varchar(36);
    IF NOT EXISTS (SELECT 1 FROM [lecturers] WHERE [user_id] = @u_lecturer)
        INSERT INTO [lecturers] ([lecturer_id], [user_id], [department], [created_at])
        VALUES (CAST(NEWID() AS varchar(36)), @u_lecturer, N'SE / SWD', GETUTCDATE());
    SELECT @lec_id = [lecturer_id] FROM [lecturers] WHERE [user_id] = @u_lecturer;

    /* ========== 3) Group ========== */
    DECLARE @gid varchar(36) = N'c0000001-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM [groups] WHERE [group_id] = @gid)
        INSERT INTO [groups] ([group_id], [group_name], [lecturer_id], [created_at])
        VALUES (@gid, N'Nhóm SWD1813', @lec_id, GETUTCDATE());
    ELSE IF @lec_id IS NOT NULL
        UPDATE [groups] SET [lecturer_id] = @lec_id, [group_name] = N'Nhóm SWD1813' WHERE [group_id] = @gid AND ([lecturer_id] IS NULL OR [lecturer_id] <> @lec_id);

    /* ========== 4) Group members (Leader + Member) ========== */
    IF NOT EXISTS (SELECT 1 FROM [group_members] WHERE [group_id] = @gid AND [user_id] = @u_leader)
        INSERT INTO [group_members] ([id], [group_id], [user_id], [role], [joined_at])
        VALUES (CAST(NEWID() AS varchar(36)), @gid, @u_leader, N'Leader', GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM [group_members] WHERE [group_id] = @gid AND [user_id] = @u_member)
        INSERT INTO [group_members] ([id], [group_id], [user_id], [role], [joined_at])
        VALUES (CAST(NEWID() AS varchar(36)), @gid, @u_member, N'Member', GETUTCDATE());

    IF @u_thoi IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [group_members] WHERE [group_id] = @gid AND [user_id] = @u_thoi)
        INSERT INTO [group_members] ([id], [group_id], [user_id], [role], [joined_at])
        VALUES (CAST(NEWID() AS varchar(36)), @gid, @u_thoi, N'Member', GETUTCDATE());

    IF @u_m14 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [group_members] WHERE [group_id] = @gid AND [user_id] = @u_m14)
        INSERT INTO [group_members] ([id], [group_id], [user_id], [role], [joined_at])
        VALUES (CAST(NEWID() AS varchar(36)), @gid, @u_m14, N'Member', GETUTCDATE());

    IF @u_anger IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [group_members] WHERE [group_id] = @gid AND [user_id] = @u_anger)
        INSERT INTO [group_members] ([id], [group_id], [user_id], [role], [joined_at])
        VALUES (CAST(NEWID() AS varchar(36)), @gid, @u_anger, N'Member', GETUTCDATE());

    IF @u_m21 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [group_members] WHERE [group_id] = @gid AND [user_id] = @u_m21)
        INSERT INTO [group_members] ([id], [group_id], [user_id], [role], [joined_at])
        VALUES (CAST(NEWID() AS varchar(36)), @gid, @u_m21, N'Member', GETUTCDATE());

    /* ========== 5) Project ========== */
    DECLARE @pid varchar(36) = N'd0000001-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM [projects] WHERE [project_id] = @pid)
        INSERT INTO [projects] ([project_id], [project_name], [group_id], [jira_project_key], [start_date], [end_date], [created_at], [lecturer_id])
        VALUES (@pid, N'Dự án Quản lý dự án SWD1813', @gid, N'KAN', DATEFROMPARTS(2025, 9, 1), DATEFROMPARTS(2026, 6, 30), GETUTCDATE(), @lec_id);
    ELSE
        UPDATE [projects]
        SET [group_id] = @gid, [jira_project_key] = N'KAN', [lecturer_id] = @lec_id
        WHERE [project_id] = @pid;

    /* ========== 6) Repository ========== */
    DECLARE @rid varchar(36) = N'e0000001-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM [repositories] WHERE [repo_id] = @rid)
        INSERT INTO [repositories] ([repo_id], [project_id], [repo_name], [repo_url], [github_owner], [created_at])
        VALUES (@rid, @pid, N'SWD1813', N'https://github.com/AngerTran/SWD1813', N'AngerTran', GETUTCDATE());

    /* ========== 7) api_integrations (token để trống — cấu hình trên web / user-secrets) ========== */
    DECLARE @integ varchar(36) = N'f0000001-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM [api_integrations] WHERE [project_id] = @pid)
        INSERT INTO [api_integrations] ([integration_id], [project_id], [jira_token], [github_token], [created_at])
        VALUES (@integ, @pid, NULL, NULL, GETUTCDATE());

    /* ========== 8) Jira issues mẫu (issue_id là khóa chính — chuỗi nội bộ) ========== */
    IF NOT EXISTS (SELECT 1 FROM [jira_issues] WHERE [issue_id] = N'seed-KAN-001')
        INSERT INTO [jira_issues] ([issue_id], [project_id], [issue_key], [summary], [description], [issue_type], [priority], [status], [assignee], [created_at], [updated_at])
        VALUES (N'seed-KAN-001', @pid, N'KAN-1', N'Tích hợp Jira', N'Kết nối và đồng bộ issue từ Jira Cloud.', N'Task', N'High', N'In Progress', NULL, GETUTCDATE(), GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM [jira_issues] WHERE [issue_id] = N'seed-KAN-005')
        INSERT INTO [jira_issues] ([issue_id], [project_id], [issue_key], [summary], [description], [issue_type], [priority], [status], [assignee], [created_at], [updated_at])
        VALUES (N'seed-KAN-005', @pid, N'KAN-5', N'Tích hợp GitHub', N'Đồng bộ commit và thống kê đóng góp.', N'Story', N'Medium', N'To Do', NULL, GETUTCDATE(), GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM [jira_issues] WHERE [issue_id] = N'seed-KAN-010')
        INSERT INTO [jira_issues] ([issue_id], [project_id], [issue_key], [summary], [description], [issue_type], [priority], [status], [assignee], [created_at], [updated_at])
        VALUES (N'seed-KAN-010', @pid, N'KAN-10', N'GitHub API client', N'Client gọi API repos/commits.', N'Task', N'Low', N'Done', NULL, GETUTCDATE(), GETUTCDATE());

    /* ========== 9) Tasks mẫu ========== */
    IF NOT EXISTS (SELECT 1 FROM [tasks] WHERE [task_id] = N't0000001-0000-0000-0000-000000000001')
        INSERT INTO [tasks] ([task_id], [issue_id], [assigned_to], [status], [deadline], [progress])
        VALUES (N't0000001-0000-0000-0000-000000000001', N'seed-KAN-001', @u_thoi, N'In Progress', DATEFROMPARTS(2026, 4, 15), 40);

    IF NOT EXISTS (SELECT 1 FROM [tasks] WHERE [task_id] = N't0000001-0000-0000-0000-000000000002')
        INSERT INTO [tasks] ([task_id], [issue_id], [assigned_to], [status], [deadline], [progress])
        VALUES (N't0000001-0000-0000-0000-000000000002', N'seed-KAN-005', @u_member, N'To Do', DATEFROMPARTS(2026, 4, 20), 0);

    /* ========== 10) Commits mẫu (12 dòng — chỉ khi repo chưa có commit) ========== */
    IF NOT EXISTS (SELECT 1 FROM [commits] WHERE [repo_id] = @rid)
    BEGIN
        INSERT INTO [commits] ([commit_id], [repo_id], [author_name], [author_email], [message], [commit_date], [files_changed], [additions], [deletions]) VALUES
        (N'seedc01a1b2c3d4e5f678901234567890abcdef01', @rid, N'Tran Leader', N'leader@student.edu', N'feat: Dashboard task completion API', DATEADD(DAY, -14, GETUTCDATE()), 5, 120, 12),
        (N'seedc02a1b2c3d4e5f678901234567890abcdef02', @rid, N'Nguyen Member', N'member@student.edu', N'fix: validation Connect Jira project key', DATEADD(DAY, -13, GETUTCDATE()), 3, 45, 8),
        (N'seedc03a1b2c3d4e5f678901234567890abcdef03', @rid, N'Anger Tran', N'anger@student.edu', N'chore: cấu hình Connect GitHub', DATEADD(DAY, -12, GETUTCDATE()), 2, 22, 4),
        (N'seedc04a1b2c3d4e5f678901234567890abcdef04', @rid, N'System Admin', N'admin@system.com', N'docs: cập nhật README và SRS', DATEADD(DAY, -11, GETUTCDATE()), 4, 88, 30),
        (N'seedc05a1b2c3d4e5f678901234567890abcdef05', @rid, N'Trần Ngọc Thái', N'thoitnse180471@fpt.edu.vn', N'refactor: ProjectService và IntegrationSync', DATEADD(DAY, -10, GETUTCDATE()), 6, 95, 40),
        (N'seedc06a1b2c3d4e5f678901234567890abcdef06', @rid, N'Dr Nguyen Van A', N'lecturer@university.edu', N'test: thêm test luồng GroupService', DATEADD(DAY, -9, GETUTCDATE()), 2, 30, 5),
        (N'seedc07a1b2c3d4e5f678901234567890abcdef07', @rid, N'System Admin', N'admin@system.com', N'style: chỉnh UI Details project', DATEADD(DAY, -8, GETUTCDATE()), 7, 104, 22),
        (N'seedc08a1b2c3d4e5f678901234567890abcdef08', @rid, N'Trần Ngọc Thái', N'thoitnse180471@fpt.edu.vn', N'feat: trang GitHubCommits và % đóng góp', DATEADD(DAY, -7, GETUTCDATE()), 5, 78, 15),
        (N'seedc09a1b2c3d4e5f678901234567890abcdef09', @rid, N'Tran Leader', N'leader@student.edu', N'merge: nhánh feature/tasks vào main', DATEADD(DAY, -6, GETUTCDATE()), 1, 12, 2),
        (N'seedc10a1b2c3d4e5f678901234567890abcdef10', @rid, N'Nguyen Member', N'member@student.edu', N'fix: encoding tên assignee trên Tasks', DATEADD(DAY, -5, GETUTCDATE()), 3, 55, 18),
        (N'seedc11a1b2c3d4e5f678901234567890abcdef11', @rid, N'Member 14', N'member14@student.edu', N'feat: chat realtime SignalR', DATEADD(DAY, -4, GETUTCDATE()), 4, 66, 10),
        (N'seedc12a1b2c3d4e5f678901234567890abcdef12', @rid, N'Member 21', N'member21@student.edu', N'fix: lọc task theo group/project', DATEADD(DAY, -3, GETUTCDATE()), 2, 40, 6);
    END

    /* ========== 11) Chat mẫu ========== */
    IF NOT EXISTS (SELECT 1 FROM [chat_messages] WHERE [message_id] = N'msg00001-0000-0000-0000-000000000001')
        INSERT INTO [chat_messages] ([message_id], [project_id], [user_id], [content], [sent_at])
        VALUES (N'msg00001-0000-0000-0000-000000000001', @pid, @u_leader, N'Chào nhóm — dùng chat để trao đổi nhanh trên dự án.', DATEADD(MINUTE, -30, GETUTCDATE()));

    IF NOT EXISTS (SELECT 1 FROM [chat_messages] WHERE [message_id] = N'msg00001-0000-0000-0000-000000000002')
        INSERT INTO [chat_messages] ([message_id], [project_id], [user_id], [content], [sent_at])
        VALUES (N'msg00001-0000-0000-0000-000000000002', @pid, @u_thoi, N'Đã đồng bộ Jira, mọi người xem issue KAN-1.', DATEADD(MINUTE, -5, GETUTCDATE()));

    COMMIT;
    PRINT N'SWD1813_SeedData: hoàn tất (nhóm + dự án + issue/task/commit/chat mẫu).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
