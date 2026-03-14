using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class ApiIntegration
{
    public string IntegrationId { get; set; } = null!;

    public string? ProjectId { get; set; }

    public string? JiraToken { get; set; }

    public string? GithubToken { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Project? Project { get; set; }
}
