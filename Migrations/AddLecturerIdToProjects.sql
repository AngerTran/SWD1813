-- Chạy script này nếu bảng projects chưa có cột lecturer_id (DB đã tồn tại).
-- Mỗi project chỉ gán 1 giảng viên; Leader/Admin gán qua Projects/AssignLecturer.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.projects') AND name = 'lecturer_id'
)
BEGIN
    ALTER TABLE dbo.projects
    ADD lecturer_id varchar(36) NULL;

    ALTER TABLE dbo.projects
    ADD CONSTRAINT FK_projects_lecturer
    FOREIGN KEY (lecturer_id) REFERENCES dbo.lecturers(lecturer_id);
END
GO
