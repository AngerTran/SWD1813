using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Models.ViewModels;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly ProjectManagementContext _context;

    public ProjectService(ProjectManagementContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<List<Project>> GetAllAsync(string? groupId = null)
    {
        var q = _context.Projects
            .Include(p => p.Group)
            .AsQueryable();
        if (!string.IsNullOrEmpty(groupId))
            q = q.Where(p => p.GroupId == groupId);
        return await q.OrderBy(p => p.ProjectName).ToListAsync();
    }

    public async System.Threading.Tasks.Task<Project?> GetByIdAsync(string id)
    {
        return await _context.Projects
            .Include(p => p.Group)
            .ThenInclude(g => g!.Lecturer)
            .ThenInclude(l => l!.User)
            .Include(p => p.JiraIssues)
            .Include(p => p.Repositories)
            .Include(p => p.ApiIntegrations)
            .FirstOrDefaultAsync(p => p.ProjectId == id);
    }

    public async System.Threading.Tasks.Task<Project> CreateAsync(string projectName, string groupId, DateOnly? startDate, DateOnly? endDate, string? jiraProjectKey = null)
    {
        var project = new Project
        {
            ProjectId = Guid.NewGuid().ToString(),
            ProjectName = projectName,
            GroupId = groupId,
            JiraProjectKey = jiraProjectKey,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async System.Threading.Tasks.Task<bool> UpdateAsync(string id, string projectName, DateOnly? startDate, DateOnly? endDate)
    {
        var p = await _context.Projects.FindAsync(id);
        if (p == null) return false;
        p.ProjectName = projectName;
        p.StartDate = startDate;
        p.EndDate = endDate;
        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<bool> SetJiraProjectKeyAsync(string projectId, string jiraProjectKey)
    {
        var p = await _context.Projects.FindAsync(projectId);
        if (p == null) return false;
        p.JiraProjectKey = jiraProjectKey;
        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<ApiIntegration?> GetApiIntegrationAsync(string projectId)
    {
        return await _context.ApiIntegrations.FirstOrDefaultAsync(a => a.ProjectId == projectId);
    }

    public async System.Threading.Tasks.Task SaveApiIntegrationAsync(string projectId, string? jiraToken, string? githubToken)
    {
        var existing = await _context.ApiIntegrations.FirstOrDefaultAsync(a => a.ProjectId == projectId);
        if (existing != null)
        {
            // Chỉ cập nhật khi có giá trị thật — tránh ghi chuỗi rỗng làm mất token đã lưu.
            if (!string.IsNullOrWhiteSpace(jiraToken)) existing.JiraToken = jiraToken.Trim();
            if (!string.IsNullOrWhiteSpace(githubToken)) existing.GithubToken = githubToken.Trim();
        }
        else
        {
            _context.ApiIntegrations.Add(new ApiIntegration
            {
                IntegrationId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                JiraToken = string.IsNullOrWhiteSpace(jiraToken) ? null : jiraToken.Trim(),
                GithubToken = string.IsNullOrWhiteSpace(githubToken) ? null : githubToken.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task<List<JiraIssue>> GetJiraIssuesByProjectAsync(string projectId)
    {
        return await _context.JiraIssues
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.IssueKey)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<bool> UpsertGitHubRepositoryAsync(string projectId, string? githubRepoUrl)
    {
        if (string.IsNullOrWhiteSpace(githubRepoUrl))
            return false;
        var p = await _context.Projects.FindAsync(projectId);
        if (p == null) return false;
        if (!GitHubRepoParser.TryParse(githubRepoUrl, out var owner, out var repoName))
            return false;

        var normalizedUrl = $"https://github.com/{owner}/{repoName}";
        var existing = await _context.Repositories
            .FirstOrDefaultAsync(r => r.ProjectId == projectId);
        if (existing != null)
        {
            existing.GithubOwner = owner;
            existing.RepoName = repoName;
            existing.RepoUrl = normalizedUrl;
        }
        else
        {
            _context.Repositories.Add(new Repository
            {
                RepoId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                GithubOwner = owner,
                RepoName = repoName,
                RepoUrl = normalizedUrl,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<GitHubCommitsPageVm?> GetGitHubCommitsPageAsync(string projectId)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId);
        if (project == null) return null;

        var repoIds = await _context.Repositories.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .Select(r => r.RepoId)
            .ToListAsync();
        if (repoIds.Count == 0)
        {
            return new GitHubCommitsPageVm
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName ?? projectId,
                Commits = Array.Empty<CommitRowVm>(),
                AuthorShares = Array.Empty<CommitAuthorShareVm>()
            };
        }

        var commits = await _context.Commits.AsNoTracking()
            .Where(c => c.RepoId != null && repoIds.Contains(c.RepoId))
            .Include(c => c.Repo)
            .OrderByDescending(c => c.CommitDate)
            .ToListAsync();

        var rows = commits.Select(c =>
        {
            var msg = c.Message;
            var first = string.IsNullOrEmpty(msg) ? null : msg.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            var sha = c.CommitId ?? "";
            return new CommitRowVm
            {
                ShaShort = sha.Length > 7 ? sha[..7] : sha,
                MessageFirstLine = string.IsNullOrEmpty(first) ? msg : first,
                AuthorName = c.AuthorName,
                AuthorEmail = c.AuthorEmail,
                CommitDate = c.CommitDate,
                RepoName = c.Repo?.RepoName ?? c.Repo?.RepoUrl,
                Additions = c.Additions,
                Deletions = c.Deletions
            };
        }).ToList();

        var totalCommits = commits.Count;
        var totalLines = commits.Sum(c => (long)(c.Additions ?? 0) + (c.Deletions ?? 0));

        static string AuthorGroupKey(Commit c)
        {
            var email = (c.AuthorEmail ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email)) return "e:" + email;
            var name = (c.AuthorName ?? "").Trim();
            if (!string.IsNullOrEmpty(name)) return "n:" + name.ToLowerInvariant();
            return "?:";
        }

        var shares = new List<CommitAuthorShareVm>();
        foreach (var g in commits.GroupBy(AuthorGroupKey))
        {
            var list = g.ToList();
            var first = list[0];
            var emailNorm = (first.AuthorEmail ?? "").Trim().ToLowerInvariant();
            var display = !string.IsNullOrEmpty(first.AuthorName?.Trim())
                ? first.AuthorName!.Trim()
                : (!string.IsNullOrEmpty(emailNorm) ? emailNorm : "(Không rõ tác giả)");
            var count = list.Count;
            var lines = list.Sum(c => (long)(c.Additions ?? 0) + (c.Deletions ?? 0));
            shares.Add(new CommitAuthorShareVm
            {
                DisplayName = display,
                Email = string.IsNullOrEmpty(emailNorm) ? null : first.AuthorEmail?.Trim(),
                CommitCount = count,
                PercentByCommits = totalCommits == 0 ? 0 : Math.Round(100.0 * count / totalCommits, 1),
                LinesTouched = lines,
                PercentByLines = totalLines == 0 ? 0 : Math.Round(100.0 * lines / totalLines, 1)
            });
        }

        shares.Sort((a, b) => b.CommitCount.CompareTo(a.CommitCount));

        return new GitHubCommitsPageVm
        {
            ProjectId = projectId,
            ProjectName = project.ProjectName ?? projectId,
            Commits = rows,
            AuthorShares = shares,
            TotalCommits = totalCommits,
            TotalLinesTouched = totalLines
        };
    }
}
