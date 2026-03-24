namespace SWD1813.Configuration;

public class JiraIntegrationOptions
{
    public const string SectionName = "Jira";

    /// <summary>VD: https://your-site.atlassian.net</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Email Atlassian (dùng cho Basic Auth: email + API token).</summary>
    public string Email { get; set; } = "";

    /// <summary>
    /// Token dùng khi project chưa lưu token trong DB (Connect Jira).
    /// Chỉ nên đặt qua <c>dotnet user-secrets</c> hoặc biến môi trường <c>Jira__ApiToken</c> — không commit vào git.
    /// </summary>
    public string ApiToken { get; set; } = "";
}

public class GitHubIntegrationOptions
{
    public const string SectionName = "GitHub";

    public string ApiBaseUrl { get; set; } = "https://api.github.com";

    /// <summary>URL repo mặc định khi project chưa có bản ghi repositories (VD: https://github.com/org/repo).</summary>
    public string RepoUrl { get; set; } = "";

    /// <summary>
    /// Nếu true: luôn ưu tiên RepoUrl trong config cho mọi project khi sync tự động
    /// (phù hợp demo/local có 1 repo chính).
    /// </summary>
    public bool PreferConfiguredRepoUrl { get; set; } = true;
}

public class IntegrationAutoSyncOptions
{
    public const string SectionName = "IntegrationAutoSync";

    /// <summary>Bật/tắt đồng bộ tự động khi app khởi động.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Đợi N giây sau khi app start rồi mới sync (tránh giật lúc warm-up).</summary>
    public int StartupDelaySeconds { get; set; } = 8;

    /// <summary>Có sync Jira không.</summary>
    public bool SyncJira { get; set; } = true;

    /// <summary>Có sync GitHub không.</summary>
    public bool SyncGitHub { get; set; } = true;

    /// <summary>Giới hạn số project sync mỗi lần startup (0 = không giới hạn).</summary>
    public int MaxProjectsPerRun { get; set; } = 0;
}
