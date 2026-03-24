# Jira trên localhost — token an toàn (User Secrets)

Không đưa API token vào chat hay commit git. Có 2 cách:

## 1) User Secrets (khuyến nghị)

Trong thư mục project `SWD1813`:

```powershell
cd d:\Netcore\SWD1813\SWD1813
dotnet user-secrets set "Jira:ApiToken" "DÁN_TOKEN_MỚI_Ở_ĐÂY"
```

`appsettings.Development.json` vẫn giữ `Jira:BaseUrl` và `Jira:Email`. Token chỉ nằm trong user-secrets (máy bạn).

## 2) Biến môi trường

```powershell
$env:Jira__ApiToken = "token-của-bạn"
dotnet run
```

## Hành vi app

- **Ưu tiên:** token đã lưu qua **Connect Jira** (DB).
- **Fallback:** nếu DB chưa có token → dùng `Jira:ApiToken` từ user-secrets / env.
- **Auto-sync:** nếu có `Jira:ApiToken` global, mọi dự án đã có **Jira Project Key** sẽ được thử đồng bộ Jira khi startup (khi `IntegrationAutoSync:SyncJira` = true).

## Sau khi cấu hình

1. Trên web: **Connect Jira** — nhập **Project Key** (vd. `KAN`); ô token có thể để trống nếu đã set user-secrets.
2. **Đồng bộ Jira** hoặc restart app để auto-sync chạy.

**Lưu ý:** Token đã từng dán lên chat nên cần **thu hồi và tạo mới** trên [Atlassian API tokens](https://id.atlassian.com/manage-profile/security/api-tokens).
