namespace SWD1813.Models.ViewModels;

/// <summary>Dữ liệu mẫu cố định cho trang demo (không đọc DB).</summary>
public static class GitHubCommitsDemoData
{
    public static GitHubCommitsPageVm Create()
    {
        var baseTime = DateTime.UtcNow.AddDays(-18).Date;
        var rows = new List<CommitRowVm>
        {
            new()
            {
                ShaShort = "f4a21be",
                MessageFirstLine = "feat: đồng bộ Jira issues vào DB",
                AuthorName = "Tran Ngoc Thoi",
                AuthorEmail = "thoitnse180471@fpt.edu.vn",
                CommitDate = baseTime.AddHours(10),
                RepoName = "SWD1813",
                Additions = 112,
                Deletions = 24
            },
            new()
            {
                ShaShort = "9c03d71",
                MessageFirstLine = "fix: lọc task theo group/project",
                AuthorName = "Tran Ngoc Thoi",
                AuthorEmail = "thoitnse180471@fpt.edu.vn",
                CommitDate = baseTime.AddDays(1).AddHours(14),
                RepoName = "SWD1813",
                Additions = 38,
                Deletions = 41
            },
            new()
            {
                ShaShort = "2b88e4a",
                MessageFirstLine = "chore: cấu hình Connect GitHub",
                AuthorName = "Nguyen Member",
                AuthorEmail = "member@student.edu",
                CommitDate = baseTime.AddDays(2).AddHours(9),
                RepoName = "SWD1813",
                Additions = 22,
                Deletions = 6
            },
            new()
            {
                ShaShort = "7d105cc",
                MessageFirstLine = "docs: cập nhật README và SRS",
                AuthorName = "Dr Nguyen Van A",
                AuthorEmail = "lecturer@university.edu",
                CommitDate = baseTime.AddDays(3).AddHours(16),
                RepoName = "SWD1813",
                Additions = 156,
                Deletions = 12
            },
            new()
            {
                ShaShort = "e551902",
                MessageFirstLine = "refactor: ProjectService và IntegrationSync",
                AuthorName = "Tran Leader",
                AuthorEmail = "leader@student.edu",
                CommitDate = baseTime.AddDays(4).AddHours(11),
                RepoName = "SWD1813",
                Additions = 89,
                Deletions = 67
            },
            new()
            {
                ShaShort = "31aa77f",
                MessageFirstLine = "feat: Dashboard task completion API",
                AuthorName = "Tran Leader",
                AuthorEmail = "leader@student.edu",
                CommitDate = baseTime.AddDays(5).AddHours(8),
                RepoName = "SWD1813",
                Additions = 64,
                Deletions = 9
            },
            new()
            {
                ShaShort = "c8b4d10",
                MessageFirstLine = "fix: validation Connect Jira project key",
                AuthorName = "Nguyen Member",
                AuthorEmail = "member@student.edu",
                CommitDate = baseTime.AddDays(6).AddHours(15),
                RepoName = "SWD1813",
                Additions = 27,
                Deletions = 18
            },
            new()
            {
                ShaShort = "4f9923d",
                MessageFirstLine = "test: thêm test luồng GroupService",
                AuthorName = "System Admin",
                AuthorEmail = "admin@system.com",
                CommitDate = baseTime.AddDays(7).AddHours(10),
                RepoName = "SWD1813",
                Additions = 201,
                Deletions = 3
            },
            new()
            {
                ShaShort = "a7762c1",
                MessageFirstLine = "style: chỉnh UI Details project",
                AuthorName = "Tran Ngoc Thoi",
                AuthorEmail = "thoitnse180471@fpt.edu.vn",
                CommitDate = baseTime.AddDays(8).AddHours(13),
                RepoName = "SWD1813",
                Additions = 45,
                Deletions = 22
            },
            new()
            {
                ShaShort = "55e8bb0",
                MessageFirstLine = "feat: trang GitHubCommits và % đóng góp",
                AuthorName = "Tran Ngoc Thoi",
                AuthorEmail = "thoitnse180471@fpt.edu.vn",
                CommitDate = baseTime.AddDays(9).AddHours(17),
                RepoName = "SWD1813",
                Additions = 178,
                Deletions = 44
            },
            new()
            {
                ShaShort = "12d9f8e",
                MessageFirstLine = "merge: nhánh feature/tasks vào main",
                AuthorName = "Tran Leader",
                AuthorEmail = "leader@student.edu",
                CommitDate = baseTime.AddDays(10).AddHours(9),
                RepoName = "SWD1813",
                Additions = 12,
                Deletions = 8
            },
            new()
            {
                ShaShort = "b0034aa",
                MessageFirstLine = "fix: encoding tên assignee trên Tasks",
                AuthorName = "Nguyen Member",
                AuthorEmail = "member@student.edu",
                CommitDate = baseTime.AddDays(11).AddHours(14),
                RepoName = "SWD1813",
                Additions = 33,
                Deletions = 29
            }
        };

        var ordered = rows.OrderByDescending(c => c.CommitDate).ToList();
        var totalCommits = ordered.Count;
        var totalLines = ordered.Sum(c => (long)(c.Additions ?? 0) + (c.Deletions ?? 0));

        static string AuthorGroupKey(CommitRowVm c)
        {
            var email = (c.AuthorEmail ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email)) return "e:" + email;
            var name = (c.AuthorName ?? "").Trim();
            if (!string.IsNullOrEmpty(name)) return "n:" + name.ToLowerInvariant();
            return "?:";
        }

        var shares = new List<CommitAuthorShareVm>();
        foreach (var g in ordered.GroupBy(AuthorGroupKey))
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
            ProjectId = "",
            ProjectName = "Demo — Commit & đóng góp (dữ liệu mẫu)",
            Commits = ordered,
            AuthorShares = shares,
            TotalCommits = totalCommits,
            TotalLinesTouched = totalLines
        };
    }
}
