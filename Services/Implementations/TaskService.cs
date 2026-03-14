using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;
using TaskEntity = SWD1813.Models.Task;

namespace SWD1813.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly ProjectManagementContext _context;

    public TaskService(ProjectManagementContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<List<TaskEntity>> GetByProjectAsync(string projectId)
    {
        return await _context.Tasks
            .Where(t => t.Issue != null && t.Issue.ProjectId == projectId)
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .OrderBy(t => t.Issue!.IssueKey)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<List<TaskEntity>> GetByGroupAsync(string groupId)
    {
        return await _context.Tasks
            .Where(t => t.Issue != null && t.Issue.Project != null && t.Issue.Project.GroupId == groupId)
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .OrderBy(t => t.Issue!.IssueKey)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<List<TaskEntity>> GetByProjectIdsAsync(IEnumerable<string> projectIds)
    {
        var ids = projectIds?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? new List<string>();
        if (ids.Count == 0) return new List<TaskEntity>();
        return await _context.Tasks
            .Where(t => t.Issue != null && t.Issue.ProjectId != null && ids.Contains(t.Issue.ProjectId))
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .OrderBy(t => t.Issue!.IssueKey)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(string taskId)
    {
        return await _context.Tasks
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);
    }

    public async System.Threading.Tasks.Task<bool> AssignTaskAsync(string taskId, string userId, DateOnly? deadline)
    {
        var task = await _context.Tasks.Include(t => t.Issue).ThenInclude(i => i!.Project).FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null) return false;
        task.AssignedTo = userId;
        task.Deadline = deadline;
        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<TaskEntity?> CreateTaskAsync(string issueId, string assignedTo, DateOnly? deadline)
    {
        var issue = await _context.JiraIssues.FindAsync(issueId);
        if (issue == null) return null;
        var existing = await _context.Tasks.FirstOrDefaultAsync(t => t.IssueId == issueId);
        if (existing != null)
        {
            existing.AssignedTo = assignedTo;
            existing.Deadline = deadline;
            existing.Status = existing.Status ?? "To Do";
            await _context.SaveChangesAsync();
            return existing;
        }
        var task = new TaskEntity
        {
            TaskId = Guid.NewGuid().ToString(),
            IssueId = issueId,
            AssignedTo = assignedTo,
            Status = "To Do",
            Deadline = deadline,
            Progress = 0
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async System.Threading.Tasks.Task<bool> UpdateStatusAsync(string taskId, string status, string? currentUserId = null)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return false;
        if (!string.IsNullOrEmpty(currentUserId) && task.AssignedTo != currentUserId)
            return false; // Chỉ thành viên được assign mới đổi được status
        task.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }
}
