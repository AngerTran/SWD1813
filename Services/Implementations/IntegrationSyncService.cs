using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWD1813.Configuration;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class IntegrationSyncService : IIntegrationSyncService
{
    private readonly ProjectManagementContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JiraIntegrationOptions _jiraOptions;
    private readonly GitHubIntegrationOptions _githubOptions;
    private readonly ILogger<IntegrationSyncService> _logger;

    private static readonly JsonSerializerOptions JsonRelaxed = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions JiraSearchBodyJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public IntegrationSyncService(
        ProjectManagementContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<JiraIntegrationOptions> jiraOptions,
        IOptions<GitHubIntegrationOptions> githubOptions,
        ILogger<IntegrationSyncService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _jiraOptions = jiraOptions.Value;
        _githubOptions = githubOptions.Value;
        _logger = logger;
    }

    public async Task<(int Count, string? Error)> SyncJiraIssuesAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);
        if (project == null)
            return (0, "Không tìm thấy dự án.");

        if (string.IsNullOrWhiteSpace(project.JiraProjectKey))
            return (0, "Chưa cấu hình Jira Project Key (Connect Jira).");

        var jiraKey = project.JiraProjectKey.Trim();
        if (jiraKey.Contains('@', StringComparison.Ordinal))
            return (0, "Jira Project Key không phải email. Dùng mã ngắn trong URL Jira (VD: KAN từ .../projects/KAN/...). Email đặt trong appsettings.json → Jira:Email.");

        var integration = await _context.ApiIntegrations.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ProjectId == projectId, cancellationToken);
        var jiraToken = integration?.JiraToken?.Trim();
        if (string.IsNullOrWhiteSpace(jiraToken))
            return (0, "Chưa lưu Jira API Token (Connect Jira).");

        var baseUrl = (_jiraOptions.BaseUrl ?? "").Trim().TrimEnd('/');
        var email = (_jiraOptions.Email ?? "").Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return (0, "Thiếu cấu hình Jira:BaseUrl trong appsettings.json.");
        if (string.IsNullOrEmpty(email))
            return (0, "Thiếu cấu hình Jira:Email trong appsettings.json (dùng cho Basic Auth).");

        var client = _httpClientFactory.CreateClient("Jira");
        client.BaseAddress = new Uri(baseUrl + "/");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{jiraToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // JQL: project key có thể cần ngoặc kép nếu có ký tự đặc biệt
        var jql = $"project = {JiraQuoteProjectKey(jiraKey)} ORDER BY updated DESC";
        var synced = 0;
        var startAt = 0;
        const int maxResults = 50;
        const int maxPages = 40;

        try
        {
            for (var page = 0; page < maxPages; page++)
            {
                // Jira Cloud đã ngừng GET /rest/api/3/search (410 Gone) — bắt buộc POST + JSON.
                var payload = new
                {
                    jql,
                    startAt,
                    maxResults,
                    fields = new[]
                    {
                        "summary", "description", "issuetype", "priority", "status", "assignee", "created",
                        "updated"
                    }
                };
                var json = JsonSerializer.Serialize(payload, JiraSearchBodyJson);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("rest/api/3/search", content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Jira API {Status}: {Body}", response.StatusCode, body.Length > 500 ? body[..500] : body);
                    var hint = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? " Kiểm tra Jira:Email trong appsettings.json phải trùng tài khoản đã tạo API token."
                        : "";
                    return (synced,
                        $"Jira API lỗi {(int)response.StatusCode}: {response.ReasonPhrase}.{hint} Kiểm tra token, email và project key (VD: KAN).");
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (!root.TryGetProperty("issues", out var issues) || issues.ValueKind != JsonValueKind.Array)
                    break;

                var count = issues.GetArrayLength();
                if (count == 0)
                    break;

                foreach (var issue in issues.EnumerateArray())
                    await UpsertJiraIssueFromJsonAsync(projectId, issue, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                synced += count;
                startAt += maxResults;

                if (count < maxResults)
                    break;
            }

            return (synced, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync Jira failed for project {ProjectId}", projectId);
            return (0, $"Lỗi khi gọi Jira: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task UpsertJiraIssueFromJsonAsync(string projectId, JsonElement issue, CancellationToken ct)
    {
        var jiraId = issue.GetProperty("id").GetString();
        var key = issue.TryGetProperty("key", out var k) ? k.GetString() : null;
        if (string.IsNullOrEmpty(jiraId))
            return;

        var fields = issue.GetProperty("fields");
        var summary = fields.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String
            ? s.GetString()
            : null;
        var description = JiraFieldsToDescriptionText(fields);
        var issueType = fields.TryGetProperty("issuetype", out var it) && it.TryGetProperty("name", out var itn)
            ? itn.GetString()
            : null;
        var priority = fields.TryGetProperty("priority", out var pr) && pr.ValueKind != JsonValueKind.Null &&
                       pr.TryGetProperty("name", out var prn)
            ? prn.GetString()
            : null;
        var status = fields.TryGetProperty("status", out var st) && st.TryGetProperty("name", out var stn)
            ? stn.GetString()
            : null;
        string? assignee = null;
        if (fields.TryGetProperty("assignee", out var asg) && asg.ValueKind == JsonValueKind.Object &&
            asg.TryGetProperty("displayName", out var dn))
            assignee = dn.GetString();

        DateTime? createdAt = null;
        DateTime? updatedAt = null;
        if (fields.TryGetProperty("created", out var cr) && cr.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(cr.GetString(), out var cdt))
            createdAt = cdt.ToUniversalTime();
        if (fields.TryGetProperty("updated", out var up) && up.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(up.GetString(), out var udt))
            updatedAt = udt.ToUniversalTime();

        var existing = await _context.JiraIssues.FirstOrDefaultAsync(i => i.IssueId == jiraId, ct);
        if (existing != null)
        {
            existing.ProjectId = projectId;
            existing.IssueKey = key;
            existing.Summary = summary;
            existing.Description = description;
            existing.IssueType = issueType;
            existing.Priority = priority;
            existing.Status = status;
            existing.Assignee = assignee;
            existing.CreatedAt ??= createdAt;
            existing.UpdatedAt = updatedAt ?? DateTime.UtcNow;
        }
        else
        {
            _context.JiraIssues.Add(new JiraIssue
            {
                IssueId = jiraId,
                ProjectId = projectId,
                IssueKey = key,
                Summary = summary,
                Description = description,
                IssueType = issueType,
                Priority = priority,
                Status = status,
                Assignee = assignee,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                UpdatedAt = updatedAt ?? DateTime.UtcNow
            });
        }
    }

    private static string? JiraFieldsToDescriptionText(JsonElement fields)
    {
        if (!fields.TryGetProperty("description", out var desc))
            return null;
        if (desc.ValueKind == JsonValueKind.Null)
            return null;
        if (desc.ValueKind == JsonValueKind.String)
            return desc.GetString();
        if (desc.ValueKind == JsonValueKind.Object)
            return ExtractTextFromAdf(desc);
        return null;
    }

    private static string? ExtractTextFromAdf(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object)
            return null;
        var sb = new StringBuilder();
        if (el.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
            sb.Append(t.GetString());
        if (el.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in content.EnumerateArray())
            {
                var part = ExtractTextFromAdf(c);
                if (!string.IsNullOrEmpty(part))
                    sb.Append(part);
            }
        }
        var r = sb.ToString().Trim();
        return string.IsNullOrEmpty(r) ? null : r;
    }

    /// <summary>JQL: key đơn giản (KAN, PROJ1) không cần ngoặc; key có ký tự lạ thì bọc "..." .</summary>
    private static string JiraQuoteProjectKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "\"\"";
        var ok = key.All(static c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');
        return ok ? key : "\"" + key.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public async Task<(int Count, string? Error)> SyncGitHubCommitsAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var integration = await _context.ApiIntegrations.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ProjectId == projectId, cancellationToken);
        var ghToken = integration?.GithubToken?.Trim();
        if (string.IsNullOrWhiteSpace(ghToken))
            return (0, "Chưa lưu GitHub Personal Access Token (Connect GitHub).");

        var repo = await _context.Repositories
            .FirstOrDefaultAsync(r => r.ProjectId == projectId, cancellationToken);

        if (repo == null && !string.IsNullOrWhiteSpace(_githubOptions.RepoUrl) &&
            GitHubRepoParser.TryParse(_githubOptions.RepoUrl, out var o0, out var r0))
        {
            repo = new Repository
            {
                RepoId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                GithubOwner = o0,
                RepoName = r0,
                RepoUrl = $"https://github.com/{o0}/{r0}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Repositories.Add(repo);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (repo == null)
            return (0, "Chưa có repository: nhập URL repo ở Connect GitHub hoặc cấu hình GitHub:RepoUrl trong appsettings.json.");

        var owner = repo.GithubOwner;
        var name = repo.RepoName;
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(name))
        {
            if (!GitHubRepoParser.TryParse(repo.RepoUrl, out var po, out var pn))
                return (0, "Không đọc được owner/repo từ RepoUrl. Dùng dạng https://github.com/owner/repo.");
            owner = po;
            name = pn;
            repo.GithubOwner = owner;
            repo.RepoName = name;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var apiBase = (_githubOptions.ApiBaseUrl ?? "https://api.github.com").Trim().TrimEnd('/');
        var client = _httpClientFactory.CreateClient("GitHub");
        client.BaseAddress = new Uri(apiBase + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ghToken);
        // User-Agent đã cấu hình trong Program.cs cho client tên "GitHub" — không thêm lần 2 (dễ lỗi 403).
        if (!client.DefaultRequestHeaders.Accept.Any(h => h.MediaType?.Contains("github", StringComparison.OrdinalIgnoreCase) == true))
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

        var synced = 0;
        try
        {
            for (var page = 1; page <= 10; page++)
            {
                var path = $"repos/{owner}/{name}/commits?per_page=100&page={page}";
                using var response = await client.GetAsync(path, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GitHub API {Status}: {Body}", response.StatusCode, body.Length > 400 ? body[..400] : body);
                    return (synced, $"GitHub API lỗi {(int)response.StatusCode}: {response.ReasonPhrase}. Kiểm tra token và quyền repo.");
                }

                var commits = JsonSerializer.Deserialize<JsonElement>(body, JsonRelaxed);
                if (commits.ValueKind != JsonValueKind.Array || commits.GetArrayLength() == 0)
                    break;

                foreach (var c in commits.EnumerateArray())
                {
                    var sha = c.TryGetProperty("sha", out var sh) ? sh.GetString() : null;
                    if (string.IsNullOrEmpty(sha))
                        continue;

                    string? message = null;
                    string? authorName = null;
                    string? authorEmail = null;
                    DateTime? commitDate = null;

                    if (c.TryGetProperty("commit", out var commitObj) && commitObj.ValueKind == JsonValueKind.Object)
                    {
                        if (commitObj.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                            message = msg.GetString();
                        if (commitObj.TryGetProperty("author", out var auth) && auth.ValueKind == JsonValueKind.Object)
                        {
                            if (auth.TryGetProperty("name", out var an) && an.ValueKind == JsonValueKind.String)
                                authorName = an.GetString();
                            if (auth.TryGetProperty("email", out var ae) && ae.ValueKind == JsonValueKind.String)
                                authorEmail = ae.GetString();
                            if (auth.TryGetProperty("date", out var ad) && ad.ValueKind == JsonValueKind.String &&
                                DateTime.TryParse(ad.GetString(), out var parsed))
                                commitDate = parsed.ToUniversalTime();
                        }
                    }

                    // Chỉ dùng payload list commits (tránh N+1 request → rate limit GitHub). Stats có thể bổ sung sau.
                    var existing = await _context.Commits.FindAsync(new object[] { sha }, cancellationToken);
                    if (existing != null)
                    {
                        existing.RepoId = repo.RepoId;
                        existing.Message = message;
                        existing.AuthorName = authorName;
                        existing.AuthorEmail = authorEmail;
                        existing.CommitDate = commitDate;
                    }
                    else
                    {
                        _context.Commits.Add(new Commit
                        {
                            CommitId = sha,
                            RepoId = repo.RepoId,
                            Message = message,
                            AuthorName = authorName,
                            AuthorEmail = authorEmail,
                            CommitDate = commitDate
                        });
                    }

                    synced++;
                }

                await _context.SaveChangesAsync(cancellationToken);

                if (commits.GetArrayLength() < 100)
                    break;
            }

            return (synced, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync GitHub failed for project {ProjectId}", projectId);
            return (0, $"Lỗi khi gọi GitHub: {ex.Message}");
        }
    }
}
