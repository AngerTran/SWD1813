using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Commit
{
    public string CommitId { get; set; } = null!;

    public string? RepoId { get; set; }

    public string? AuthorName { get; set; }

    public string? AuthorEmail { get; set; }

    public string? Message { get; set; }

    public DateTime? CommitDate { get; set; }

    public int? FilesChanged { get; set; }

    public int? Additions { get; set; }

    public int? Deletions { get; set; }

    public virtual Repository? Repo { get; set; }
}
