using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class GroupService : IGroupService
{
    private readonly ProjectManagementContext _context;

    public GroupService(ProjectManagementContext context)
    {
        _context = context;
    }

    public async Task<List<Group>> GetAllAsync(string? userId = null, string? userRole = null)
    {
        var q = _context.Groups
            .Include(g => g.Lecturer)
            .ThenInclude(l => l!.User)
            .Include(g => g.GroupMembers)
            .AsQueryable();
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userRole))
            return await q.OrderBy(g => g.GroupName).ToListAsync();
        var role = (userRole ?? "").Trim();
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return await q.OrderBy(g => g.GroupName).ToListAsync();
        if (string.Equals(role, "Lecturer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userId))
        {
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId);
            if (lecturer == null) return new List<Group>();
            q = q.Where(g => g.LecturerId == lecturer.LecturerId);
            return await q.OrderBy(g => g.GroupName).ToListAsync();
        }
        if (string.Equals(role, "Leader", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userId))
        {
            q = q.Where(g => g.GroupMembers.Any(m => m.UserId == userId && (m.Role == "Leader" || m.Role == "LEADER")));
            return await q.OrderBy(g => g.GroupName).ToListAsync();
        }
        if (string.Equals(role, "Member", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userId))
        {
            q = q.Where(g => g.GroupMembers.Any(m => m.UserId == userId));
            return await q.OrderBy(g => g.GroupName).ToListAsync();
        }
        return await q.OrderBy(g => g.GroupName).ToListAsync();
    }

    public async Task<List<string>> GetGroupIdsUserParticipatesInAsync(string? userId, string? userRole)
    {
        var role = (userRole ?? "").Trim();
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return await _context.Groups.Select(g => g.GroupId).ToListAsync();
        if (string.IsNullOrEmpty(userId)) return new List<string>();
        if (string.Equals(role, "Lecturer", StringComparison.OrdinalIgnoreCase))
        {
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId);
            if (lecturer == null) return new List<string>();
            return await _context.Groups.Where(g => g.LecturerId == lecturer.LecturerId).Select(g => g.GroupId).ToListAsync();
        }
        if (string.Equals(role, "Leader", StringComparison.OrdinalIgnoreCase))
            return await _context.GroupMembers.Where(m => m.UserId == userId && (m.Role == "Leader" || m.Role == "LEADER") && m.GroupId != null).Select(m => m.GroupId!).Distinct().ToListAsync();
        if (string.Equals(role, "Member", StringComparison.OrdinalIgnoreCase))
            return await _context.GroupMembers.Where(m => m.UserId == userId && m.GroupId != null).Select(m => m.GroupId!).Distinct().ToListAsync();
        return new List<string>();
    }

    public async Task<bool> IsLeaderOfGroupAsync(string groupId, string userId)
    {
        return await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId && (m.Role == "Leader" || m.Role == "LEADER"));
    }

    public async Task<Group?> GetByIdAsync(string id)
    {
        return await _context.Groups
            .Include(g => g.Lecturer)
            .ThenInclude(l => l!.User)
            .Include(g => g.GroupMembers)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.GroupId == id);
    }

    public async Task<Group> CreateAsync(string groupName, string? createdByUserId = null)
    {
        var group = new Group
        {
            GroupId = Guid.NewGuid().ToString(),
            GroupName = groupName,
            CreatedAt = DateTime.UtcNow
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        if (!string.IsNullOrEmpty(createdByUserId))
        {
            _context.GroupMembers.Add(new GroupMember
            {
                Id = Guid.NewGuid().ToString(),
                GroupId = group.GroupId,
                UserId = createdByUserId,
                Role = "Leader",
                JoinedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        return group;
    }

    public async Task<bool> UpdateAsync(string id, string groupName)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null) return false;
        group.GroupName = groupName;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var group = await _context.Groups
            .Include(g => g.GroupMembers)
            .Include(g => g.Projects)
            .FirstOrDefaultAsync(g => g.GroupId == id);
        if (group == null) return false;
        if (group.Projects.Any())
            return false; // has projects
        foreach (var m in group.GroupMembers.ToList())
            _context.GroupMembers.Remove(m);
        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignLecturerAsync(string groupId, string lecturerId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) return false;
        var lecturer = await _context.Lecturers.FindAsync(lecturerId);
        if (lecturer == null) return false;
        group.LecturerId = lecturerId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddMemberAsync(string groupId, string userId, string role)
    {
        var alreadyInGroup = await _context.GroupMembers.AnyAsync(m => m.UserId == userId && m.GroupId != groupId);
        if (alreadyInGroup) return false; // BR5: one student one group
        var hasLeader = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.Role == "Leader");
        if (role != "Leader" && !hasLeader) return false; // BR4: need at least one leader
        var existing = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (existing != null) return false;
        _context.GroupMembers.Add(new GroupMember
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(string groupId, string userId)
    {
        var member = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member == null) return false;
        if (member.Role == "Leader")
        {
            var otherLeaders = await _context.GroupMembers.CountAsync(m => m.GroupId == groupId && m.Role == "Leader" && m.UserId != userId);
            if (otherLeaders == 0)
            {
                var total = await _context.GroupMembers.CountAsync(m => m.GroupId == groupId);
                if (total <= 1) return false; // cannot remove last member
                // Or allow and leave group without leader - for simplicity allow
            }
        }
        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<GroupMember>> GetMembersAsync(string groupId)
    {
        return await _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Include(m => m.User)
            .OrderBy(m => m.Role)
            .ToListAsync();
    }

    public async Task<List<Lecturer>> GetLecturersAsync()
    {
        return await _context.Lecturers.Include(l => l.User).OrderBy(l => l.User!.FullName).ToListAsync();
    }

    public async Task<List<User>> GetUsersNotInAnyGroupAsync()
    {
        var inGroup = await _context.GroupMembers.Select(m => m.UserId).Distinct().ToListAsync();
        var roles = new[] { "Member", "MEMBER", "Leader", "LEADER" };
        return await _context.Users
            .Where(u => !inGroup.Contains(u.UserId) && roles.Contains(u.Role))
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}
