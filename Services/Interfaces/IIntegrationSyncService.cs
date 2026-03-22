namespace SWD1813.Services.Interfaces;

/// <summary>Đồng bộ dữ liệu từ Jira Cloud và GitHub API vào database.</summary>
public interface IIntegrationSyncService
{
    /// <returns>Số issue đã upsert; Error message nếu thất bại.</returns>
    System.Threading.Tasks.Task<(int Count, string? Error)> SyncJiraIssuesAsync(string projectId, System.Threading.CancellationToken cancellationToken = default);

    /// <returns>Số commit đã upsert; Error message nếu thất bại.</returns>
    System.Threading.Tasks.Task<(int Count, string? Error)> SyncGitHubCommitsAsync(string projectId, System.Threading.CancellationToken cancellationToken = default);
}
