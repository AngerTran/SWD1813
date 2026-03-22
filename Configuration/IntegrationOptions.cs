namespace SWD1813.Configuration;

public class JiraIntegrationOptions
{
    public const string SectionName = "Jira";

    /// <summary>VD: https://your-site.atlassian.net</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Email Atlassian (dùng cho Basic Auth: email + API token).</summary>
    public string Email { get; set; } = "";
}

public class GitHubIntegrationOptions
{
    public const string SectionName = "GitHub";

    public string ApiBaseUrl { get; set; } = "https://api.github.com";

    /// <summary>URL repo mặc định khi project chưa có bản ghi repositories (VD: https://github.com/org/repo).</summary>
    public string RepoUrl { get; set; } = "";
}
