using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Sprint
{
    public string SprintId { get; set; } = null!;

    public string? ProjectId { get; set; }

    public string? SprintName { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual Project? Project { get; set; }
}
