using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Project
{
    public string ProjectId { get; set; } = null!;

    public string? ProjectName { get; set; }

    public string? GroupId { get; set; }

    public string? JiraProjectKey { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ApiIntegration> ApiIntegrations { get; set; } = new List<ApiIntegration>();

    public virtual Group? Group { get; set; }

    public virtual ICollection<JiraIssue> JiraIssues { get; set; } = new List<JiraIssue>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Repository> Repositories { get; set; } = new List<Repository>();

    public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
}
