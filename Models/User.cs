using System;
using System.Collections.Generic;

namespace SWD1813.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ContributorStat> ContributorStats { get; set; } = new List<ContributorStat>();

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual Lecturer? Lecturer { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
