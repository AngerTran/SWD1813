using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Repository
{
    public string RepoId { get; set; } = null!;

    public string? ProjectId { get; set; }

    public string? RepoName { get; set; }

    public string? RepoUrl { get; set; }

    public string? GithubOwner { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Commit> Commits { get; set; } = new List<Commit>();

    public virtual ICollection<ContributorStat> ContributorStats { get; set; } = new List<ContributorStat>();

    public virtual Project? Project { get; set; }
}
