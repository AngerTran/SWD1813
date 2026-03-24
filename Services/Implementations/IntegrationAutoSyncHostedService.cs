using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWD1813.Configuration;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

/// <summary>
/// Tự đồng bộ Jira/GitHub khi app start (có thể tắt bằng config IntegrationAutoSync:Enabled).
/// </summary>
public class IntegrationAutoSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IntegrationAutoSyncOptions _options;
    private readonly JiraIntegrationOptions _jiraOptions;
    private readonly ILogger<IntegrationAutoSyncHostedService> _logger;

    public IntegrationAutoSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<IntegrationAutoSyncOptions> options,
        IOptions<JiraIntegrationOptions> jiraOptions,
        ILogger<IntegrationAutoSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _jiraOptions = jiraOptions.Value;
        _logger = logger;
    }

    protected override async global::System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("IntegrationAutoSync disabled.");
            return;
        }

        try
        {
            if (_options.StartupDelaySeconds > 0)
                await global::System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ProjectManagementContext>();
            var sync = scope.ServiceProvider.GetRequiredService<IIntegrationSyncService>();

            var projectIdSet = new HashSet<string>(StringComparer.Ordinal);

            if (_options.SyncJira)
            {
                var globalJiraToken = !string.IsNullOrWhiteSpace(_jiraOptions.ApiToken);
                var withJiraKey = await db.Projects.AsNoTracking()
                    .Where(p => !string.IsNullOrWhiteSpace(p.JiraProjectKey))
                    .Select(p => p.ProjectId)
                    .ToListAsync(stoppingToken);
                var dbJiraTokenIds = await db.ApiIntegrations.AsNoTracking()
                    .Where(a => !string.IsNullOrWhiteSpace(a.JiraToken))
                    .Select(a => a.ProjectId!)
                    .ToListAsync(stoppingToken);
                var dbJiraTokens = dbJiraTokenIds.ToHashSet(StringComparer.Ordinal);
                foreach (var pid in withJiraKey)
                {
                    if (globalJiraToken || dbJiraTokens.Contains(pid))
                        projectIdSet.Add(pid);
                }
            }

            if (_options.SyncGitHub)
            {
                var ghIds = await db.ApiIntegrations.AsNoTracking()
                    .Where(a => !string.IsNullOrWhiteSpace(a.GithubToken))
                    .Select(a => a.ProjectId!)
                    .ToListAsync(stoppingToken);
                foreach (var id in ghIds)
                    projectIdSet.Add(id);
            }

            var projectIds = projectIdSet.ToList();
            if (_options.MaxProjectsPerRun > 0)
                projectIds = projectIds.Take(_options.MaxProjectsPerRun).ToList();
            if (projectIds.Count == 0)
            {
                _logger.LogInformation("IntegrationAutoSync: no configured projects.");
                return;
            }

            _logger.LogInformation("IntegrationAutoSync started for {Count} project(s).", projectIds.Count);
            foreach (var projectId in projectIds)
            {
                if (stoppingToken.IsCancellationRequested) break;

                if (_options.SyncJira)
                {
                    var (count, error) = await sync.SyncJiraIssuesAsync(projectId, stoppingToken);
                    if (!string.IsNullOrEmpty(error))
                        _logger.LogWarning("Auto Jira sync failed for {ProjectId}: {Error}", projectId, error);
                    else
                        _logger.LogInformation("Auto Jira sync {ProjectId}: {Count} issue(s).", projectId, count);
                }

                if (_options.SyncGitHub)
                {
                    var (count, error) = await sync.SyncGitHubCommitsAsync(projectId, stoppingToken);
                    if (!string.IsNullOrEmpty(error))
                        _logger.LogWarning("Auto GitHub sync failed for {ProjectId}: {Error}", projectId, error);
                    else
                        _logger.LogInformation("Auto GitHub sync {ProjectId}: {Count} commit(s).", projectId, count);
                }
            }

            _logger.LogInformation("IntegrationAutoSync completed.");
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IntegrationAutoSync crashed.");
        }
    }
}
