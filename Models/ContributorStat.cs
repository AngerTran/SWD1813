using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class ContributorStat
{
    public string StatId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? RepoId { get; set; }

    public int? TotalCommits { get; set; }

    public int? TotalAdditions { get; set; }

    public int? TotalDeletions { get; set; }

    public DateTime? LastCommit { get; set; }

    public virtual Repository? Repo { get; set; }

    public virtual User? User { get; set; }
}
