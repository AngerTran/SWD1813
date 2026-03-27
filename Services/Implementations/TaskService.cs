using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Repositories;
using SWD1813.Services.Interfaces;
using TaskEntity = SWD1813.Models.Task;

namespace SWD1813.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly IRepository<TaskEntity> _tasks;
    private readonly IRepository<JiraIssue> _jiraIssues;
    private readonly IRepository<Project> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public TaskService(
        IRepository<TaskEntity> tasks,
        IRepository<JiraIssue> jiraIssues,
        IRepository<Project> projects,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _jiraIssues = jiraIssues;
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task<List<TaskEntity>> GetByProjectAsync(string projectId)
    {
        return await _tasks.Query()
            .Where(t => t.Issue != null && t.Issue.ProjectId == projectId)
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .OrderBy(t => t.Issue!.IssueKey)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<List<TaskEntity>> GetByGroupAsync(string groupId)
    {
        return await _tasks.Query()
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
        return await _tasks.Query()
            .Where(t => t.Issue != null && t.Issue.ProjectId != null && ids.Contains(t.Issue.ProjectId))
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .OrderBy(t => t.Issue!.IssueKey)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(string taskId)
    {
        return await _tasks.Query()
            .Include(t => t.Issue)
            .Include(t => t.AssignedToNavigation)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);
    }

    public async System.Threading.Tasks.Task<bool> AssignTaskAsync(string taskId, string userId, DateOnly? deadline)
    {
        var task = await _tasks.Query().Include(t => t.Issue).ThenInclude(i => i!.Project).FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null) return false;
        task.AssignedTo = userId;
        task.Deadline = deadline;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<TaskEntity?> CreateTaskAsync(string issueId, string assignedTo, DateOnly? deadline)
    {
        var issue = await _jiraIssues.FindAsync([issueId]);
        if (issue == null) return null;
        var existing = await _tasks.Query().FirstOrDefaultAsync(t => t.IssueId == issueId);
        if (existing != null)
        {
            existing.AssignedTo = assignedTo;
            existing.Deadline = deadline;
            existing.Status = existing.Status ?? "To Do";
            await _unitOfWork.SaveChangesAsync();
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
        _tasks.Add(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async System.Threading.Tasks.Task<TaskEntity?> CreateManualTaskAsync(string projectId, string taskTitle, string assignedTo, DateOnly? deadline)
    {
        var project = await _projects.FindAsync([projectId]);
        if (project == null) return null;
        var title = (taskTitle ?? "").Trim();
        if (string.IsNullOrEmpty(title)) return null;
        var manualKey = "MANUAL-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var jiraIssue = new JiraIssue
        {
            IssueId = manualKey,
            IssueKey = manualKey,
            ProjectId = projectId,
            Summary = title,
            IssueType = "Task",
            Status = "To Do",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _jiraIssues.Add(jiraIssue);
        var task = new TaskEntity
        {
            TaskId = Guid.NewGuid().ToString(),
            IssueId = manualKey,
            AssignedTo = assignedTo,
            Status = "To Do",
            Deadline = deadline,
            Progress = 0
        };
        _tasks.Add(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async System.Threading.Tasks.Task<bool> UpdateStatusAsync(string taskId, string status, string? currentUserId = null)
    {
        var task = await _tasks.FindAsync([taskId]);
        if (task == null) return false;
        if (!string.IsNullOrEmpty(currentUserId) && task.AssignedTo != currentUserId)
            return false; // Chỉ thành viên được assign mới đổi được status
        task.Status = status;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
