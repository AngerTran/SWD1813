# Commit mẫu (seed)

## Tự động khi chạy app

`Program.cs` gọi `SampleCommitsSeeder.EnsureAsync` sau khi seed user mặc định.

- **Phạm vi:** **mọi** `projects` (theo thứ tự `created_at`).
- **Với từng project:**
  1. Nếu chưa có dòng nào trong `repositories` → tạo **repo demo** (`SWD1813` / `AngerTran`), `repo_id` sinh **deterministic** theo `project_id` (36 ký tự GUID).
  2. Nếu **repo đầu tiên** của project đó **chưa có commit** trong `commits` → thêm **12 commit** mẫu (author, email, message, additions/deletions) để trang **GitHubCommits** có **danh sách + % đóng góp**.
- Nếu project đã có commit thật (đồng bộ GitHub) trên repo đó → **không** thêm commit giả.

**Lưu ý:** Cần **khởi động lại** ứng dụng một lần sau khi cập nhật code (hoặc gọi lại `EnsureAsync`) để các project trước đây bị “bỏ rơi” được seed. Muốn xóa dữ liệu demo: xóa các dòng `commits` gắn repo demo (hoặc xóa repo demo rồi chạy lại app nếu muốn seed lại).

## File code

- `Services/Implementations/SampleCommitsSeeder.cs`
