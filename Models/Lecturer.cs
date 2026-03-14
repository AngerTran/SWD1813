using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Lecturer
{
    public string LecturerId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? Department { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual User? User { get; set; }
}
