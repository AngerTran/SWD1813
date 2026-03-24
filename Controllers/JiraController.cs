using Microsoft.AspNetCore.Mvc;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

public class JiraController : ProjectIntegrationControllerBase
{
    private readonly IIntegrationSyncService _integrationSync;

    public JiraController(
        IProjectService projectService,
        IGroupService groupService,
        IIntegrationSyncService integrationSync)
        : base(projectService, groupService)
    {
        _integrationSync = integrationSync;
    }

    public async Task<IActionResult> ConnectJira(string id)
    {
        var (project, err) = await TryGetAccessibleProjectAsync(id);
        if (err != null) return err;
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectJira([FromForm] string projectId, [FromForm] string? jiraProjectKey, [FromForm] string? jiraToken)
    {
        var (_, err) = await TryGetAccessibleProjectAsync(projectId);
        if (err != null) return err;

        if (!string.IsNullOrWhiteSpace(jiraProjectKey))
        {
            var key = jiraProjectKey.Trim();
            if (key.Contains('@'))
            {
                TempData["Error"] =
                    "Jira Project Key không phải email. Nhập mã project (VD: KAN) lấy từ URL .../projects/KAN/... Email cấu hình trong appsettings.json → Jira:Email.";
                return RedirectToAction(nameof(ConnectJira), "Jira", new { id = projectId });
            }

            await ProjectService.SetJiraProjectKeyAsync(projectId, key);
        }

        if (!string.IsNullOrWhiteSpace(jiraToken))
            await ProjectService.SaveApiIntegrationAsync(projectId, jiraToken.Trim(), null);

        return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id = projectId });
    }

    public async Task<IActionResult> JiraIssues(string id)
    {
        var (project, err) = await TryGetAccessibleProjectAsync(id);
        if (err != null) return err;

        var issues = await ProjectService.GetJiraIssuesByProjectAsync(id);
        ViewBag.ProjectId = project!.ProjectId;
        ViewBag.ProjectName = project.ProjectName ?? project.ProjectId;
        ViewBag.JiraProjectKey = project.JiraProjectKey ?? "-";
        return View(issues);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncJira(string id, CancellationToken cancellationToken)
    {
        var (_, err) = await TryGetAccessibleProjectAsync(id);
        if (err != null) return err;

        var (count, syncError) = await _integrationSync.SyncJiraIssuesAsync(id, cancellationToken);
        if (!string.IsNullOrEmpty(syncError))
            TempData["Error"] = syncError;
        else if (count == 0)
            TempData["Success"] = "Jira trả về 0 issue (kiểm tra project key KAN và quyền token), hoặc project chưa có issue.";
        else
            TempData["Success"] = $"Đã đồng bộ {count} issue từ Jira.";
        return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id });
    }
}
