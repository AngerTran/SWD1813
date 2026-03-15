-- Chạy 1 lần nếu DB của bạn ĐÃ CÓ SẴN (users, groups, projects...) nhưng chưa dùng EF migrations.
-- Script này đánh dấu migration đầu (tạo toàn bộ bảng) là đã áp dụng, để lần chạy "dotnet ef database update" chỉ áp dụng migration thêm cột lecturer_id.

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260314110041_AddLecturerToProject')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260314110041_AddLecturerToProject', N'9.0.0');
END
GO
