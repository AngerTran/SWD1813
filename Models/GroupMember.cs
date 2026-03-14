using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class GroupMember
{
    public string Id { get; set; } = null!;

    public string? GroupId { get; set; }

    public string? UserId { get; set; }

    public string? Role { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual Group? Group { get; set; }

    public virtual User? User { get; set; }
}
