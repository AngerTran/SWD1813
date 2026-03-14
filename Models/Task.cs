using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Task
{
    public string TaskId { get; set; } = null!;

    public string? IssueId { get; set; }

    public string? AssignedTo { get; set; }

    public string? Status { get; set; }

    public DateOnly? Deadline { get; set; }

    public int? Progress { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual JiraIssue? Issue { get; set; }
}
