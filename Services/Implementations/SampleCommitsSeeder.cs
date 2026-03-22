using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SWD1813.Models;

namespace SWD1813.Services.Implementations;

/// <summary>
/// Gắn commit demo theo từng project: mỗi project có ít nhất một repo (tạo repo demo nếu chưa có)
/// và 12 commit mẫu nếu repo đó chưa có commit nào (để trang GitHubCommits luôn có biểu đồ khi demo).
/// </summary>
public static class SampleCommitsSeeder
{
    /// <summary>repo_id cố định 36 ký tự, deterministic theo project (không trùng PK giữa các project).</summary>
    private static string DemoRepoIdForProject(string projectId)
    {
        var payload = "SWD1813-seed-repo:" + projectId;
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(payload), hash);
        var guidBytes = new byte[16];
        hash[..16].CopyTo(guidBytes);
        return new Guid(guidBytes).ToString();
    }

    public static async System.Threading.Tasks.Task EnsureAsync(ProjectManagementContext context,
        CancellationToken cancellationToken = default)
    {
        var projects = await context.Projects
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var repo = await context.Repositories
                .FirstOrDefaultAsync(r => r.ProjectId == project.ProjectId, cancellationToken);

            if (repo == null)
            {
                repo = new Repository
                {
                    RepoId = DemoRepoIdForProject(project.ProjectId ?? ""),
                    ProjectId = project.ProjectId,
                    RepoName = "SWD1813",
                    RepoUrl = "https://github.com/AngerTran/SWD1813",
                    GithubOwner = "AngerTran",
                    CreatedAt = DateTime.UtcNow
                };
                context.Repositories.Add(repo);
                await context.SaveChangesAsync(cancellationToken);
            }

            var hasCommits = await context.Commits
                .AnyAsync(c => c.RepoId == repo.RepoId, cancellationToken);
            if (hasCommits)
                continue;

            var authors = new[]
            {
                ("Tran Leader", "leader@student.edu"),
                ("Nguyen Member", "member@student.edu"),
                ("Dr Nguyen Van A", "lecturer@university.edu"),
                ("System Admin", "admin@system.com"),
                ("Tran Ngoc Thoi", "thoitnse180471@fpt.edu.vn")
            };

            var messages = new[]
            {
                "feat: đồng bộ Jira issues vào DB",
                "fix: lọc task theo group/project",
                "chore: cấu hình Connect GitHub",
                "docs: cập nhật README và SRS",
                "refactor: ProjectService và IntegrationSync",
                "feat: Dashboard task completion API",
                "fix: validation Connect Jira project key",
                "test: thêm test luồng GroupService",
                "style: chỉnh UI Details project",
                "feat: trang GitHubCommits và % đóng góp",
                "merge: nhánh feature/tasks vào main",
                "fix: encoding tên assignee trên Tasks"
            };

            // Seed khác nhẹ giữa các project (vẫn deterministic khi chạy lại cùng DB).
            var pid = project.ProjectId ?? "";
            var seed = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(pid))[..4]);
            var rnd = new Random(seed);
            var baseTime = DateTime.UtcNow.AddDays(-21);
            var commits = new List<Commit>();

            for (var i = 0; i < messages.Length; i++)
            {
                var (name, email) = authors[i % authors.Length];
                var additions = rnd.Next(5, 120);
                var deletions = rnd.Next(0, 45);
                commits.Add(new Commit
                {
                    CommitId = Guid.NewGuid().ToString("N"),
                    RepoId = repo.RepoId,
                    AuthorName = name,
                    AuthorEmail = email,
                    Message = messages[i],
                    CommitDate = baseTime.AddHours(i * 18 + rnd.Next(0, 5)),
                    FilesChanged = rnd.Next(1, 8),
                    Additions = additions,
                    Deletions = deletions
                });
            }

            context.Commits.AddRange(commits);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
