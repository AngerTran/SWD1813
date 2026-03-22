namespace SWD1813.Models.ViewModels;

/// <summary>Dữ liệu trang commit GitHub + phân bổ đóng góp theo tác giả.</summary>
public class GitHubCommitsPageVm
{
    public string ProjectId { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public IReadOnlyList<CommitRowVm> Commits { get; set; } = Array.Empty<CommitRowVm>();
    public IReadOnlyList<CommitAuthorShareVm> AuthorShares { get; set; } = Array.Empty<CommitAuthorShareVm>();
    public int TotalCommits { get; set; }
    public long TotalLinesTouched { get; set; }
}

public class CommitRowVm
{
    public string ShaShort { get; set; } = "";
    public string? MessageFirstLine { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public DateTime? CommitDate { get; set; }
    public string? RepoName { get; set; }
    public int? Additions { get; set; }
    public int? Deletions { get; set; }
}

public class CommitAuthorShareVm
{
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public int CommitCount { get; set; }
    /// <summary>% theo số commit (luôn có ý nghĩa khi TotalCommits &gt; 0).</summary>
    public double PercentByCommits { get; set; }
    public long LinesTouched { get; set; }
    /// <summary>% theo tổng dòng (additions+deletions) khi có dữ liệu; nếu không có stats thì 0.</summary>
    public double PercentByLines { get; set; }
}
