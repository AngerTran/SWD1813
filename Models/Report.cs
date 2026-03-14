using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Report
{
    public string ReportId { get; set; } = null!;

    public string? ProjectId { get; set; }

    public string? ReportType { get; set; }

    public string? GeneratedBy { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public virtual User? GeneratedByNavigation { get; set; }

    public virtual Project? Project { get; set; }
}
