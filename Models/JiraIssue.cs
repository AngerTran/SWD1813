using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class JiraIssue
{
    public string IssueId { get; set; } = null!;

    public string? ProjectId { get; set; }

    public string? IssueKey { get; set; }

    public string? Summary { get; set; }

    public string? Description { get; set; }

    public string? IssueType { get; set; }

    public string? Priority { get; set; }

    public string? Status { get; set; }

    public string? Assignee { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
