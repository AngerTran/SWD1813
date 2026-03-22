using SWD1813.Models;
using SWD1813.Models.ViewModels;

namespace SWD1813.Services.Interfaces;

public interface IProjectService
{
    System.Threading.Tasks.Task<List<Project>> GetAllAsync(string? groupId = null);
    System.Threading.Tasks.Task<Project?> GetByIdAsync(string id);
    System.Threading.Tasks.Task<Project> CreateAsync(string projectName, string groupId, DateOnly? startDate, DateOnly? endDate, string? jiraProjectKey = null);
    System.Threading.Tasks.Task<bool> UpdateAsync(string id, string projectName, DateOnly? startDate, DateOnly? endDate);
    System.Threading.Tasks.Task<bool> SetJiraProjectKeyAsync(string projectId, string jiraProjectKey);
    System.Threading.Tasks.Task<ApiIntegration?> GetApiIntegrationAsync(string projectId);
    System.Threading.Tasks.Task SaveApiIntegrationAsync(string projectId, string? jiraToken, string? githubToken);
    System.Threading.Tasks.Task<List<JiraIssue>> GetJiraIssuesByProjectAsync(string projectId);

    /// <summary>Lưu / cập nhật bản ghi repositories từ URL GitHub (https://github.com/owner/repo).</summary>
    System.Threading.Tasks.Task<bool> UpsertGitHubRepositoryAsync(string projectId, string? githubRepoUrl);

    /// <summary>Commits của mọi repo thuộc project + thống kê % đóng góp theo tác giả.</summary>
    System.Threading.Tasks.Task<GitHubCommitsPageVm?> GetGitHubCommitsPageAsync(string projectId);
}
