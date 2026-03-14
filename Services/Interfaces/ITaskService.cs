using SWD1813.Models;
using TaskEntity = SWD1813.Models.Task;

namespace SWD1813.Services.Interfaces;

public interface ITaskService
{
    System.Threading.Tasks.Task<List<TaskEntity>> GetByProjectAsync(string projectId);
    System.Threading.Tasks.Task<List<TaskEntity>> GetByGroupAsync(string groupId);
    /// <summary>Tasks thuộc các project trong danh sách (để lọc theo nhóm user tham gia).</summary>
    System.Threading.Tasks.Task<List<TaskEntity>> GetByProjectIdsAsync(IEnumerable<string> projectIds);
    System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(string taskId);
    System.Threading.Tasks.Task<bool> AssignTaskAsync(string taskId, string userId, DateOnly? deadline);
    /// <summary>Create task from Jira issue and assign to member. Returns task or null.</summary>
    System.Threading.Tasks.Task<TaskEntity?> CreateTaskAsync(string issueId, string assignedTo, DateOnly? deadline);
    /// <summary>Update status. Only succeeds if currentUserId is the task's assignee.</summary>
    System.Threading.Tasks.Task<bool> UpdateStatusAsync(string taskId, string status, string? currentUserId = null);
}
