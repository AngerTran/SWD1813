using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class Group
{
    public string GroupId { get; set; } = null!;

    public string? GroupName { get; set; }

    public string? LecturerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual Lecturer? Lecturer { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
