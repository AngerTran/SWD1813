using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SWD1813.Configuration;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IGroupService _groupService;
    private readonly IIntegrationSyncService _integrationSync;
    private readonly GitHubIntegrationOptions _githubIntegrationOptions;

    public ProjectsController(
        IProjectService projectService,
        IGroupService groupService,
        IIntegrationSyncService integrationSync,
        IOptions<GitHubIntegrationOptions> githubIntegrationOptions)
    {
        _projectService = projectService;
        _groupService = groupService;
        _integrationSync = integrationSync;
        _githubIntegrationOptions = githubIntegrationOptions.Value;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    private async Task<List<string>> GetUserGroupIdsAsync()
    {
        return await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
    }

    public async Task<IActionResult> Index(string? groupId)
    {
        var groupIds = await GetUserGroupIdsAsync();
        var groups = await _groupService.GetAllAsync(CurrentUserId, CurrentUserRole);
        var allProjects = await _projectService.GetAllAsync(groupId);
        var list = allProjects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).ToList();
        ViewBag.Groups = groups;
        ViewBag.SelectedGroupId = groupId;
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Groups = await _groupService.GetAllAsync(CurrentUserId, CurrentUserRole);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string projectName, string groupId, DateOnly? startDate, DateOnly? endDate)
    {
        var groupIds = await GetUserGroupIdsAsync();
        if (!groupIds.Contains(groupId))
            return Forbid();
        if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(groupId))
        {
            ViewBag.Groups = await _groupService.GetAllAsync(CurrentUserId, CurrentUserRole);
            ModelState.AddModelError("", "Project name and group are required.");
            return View();
        }
        await _projectService.CreateAsync(projectName.Trim(), groupId, startDate, endDate);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string projectName, DateOnly? startDate, DateOnly? endDate)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        var ok = await _projectService.UpdateAsync(id, projectName ?? "", startDate, endDate);
        if (!ok) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        var integration = await _projectService.GetApiIntegrationAsync(id);
        ViewBag.HasGithubToken = !string.IsNullOrWhiteSpace(integration?.GithubToken);
        ViewBag.HasJiraToken = !string.IsNullOrWhiteSpace(integration?.JiraToken);
        return View(project);
    }

    public async Task<IActionResult> ConnectJira(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectJira([FromForm] string projectId, [FromForm] string? jiraProjectKey, [FromForm] string? jiraToken)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        if (!string.IsNullOrWhiteSpace(jiraProjectKey))
        {
            var key = jiraProjectKey.Trim();
            if (key.Contains('@'))
            {
                TempData["Error"] =
                    "Jira Project Key không phải email. Nhập mã project (VD: KAN) lấy từ URL .../projects/KAN/... Email cấu hình trong appsettings.json → Jira:Email.";
                return RedirectToAction(nameof(ConnectJira), new { id = projectId });
            }

            await _projectService.SetJiraProjectKeyAsync(projectId, key);
        }

        if (!string.IsNullOrWhiteSpace(jiraToken))
            await _projectService.SaveApiIntegrationAsync(projectId, jiraToken.Trim(), null);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    /// <summary>Trang danh sách issue Jira đã lưu trong DB (sau Connect + Đồng bộ).</summary>
    public async Task<IActionResult> JiraIssues(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();

        var issues = await _projectService.GetJiraIssuesByProjectAsync(id);
        ViewBag.ProjectId = project.ProjectId;
        ViewBag.ProjectName = project.ProjectName ?? project.ProjectId;
        ViewBag.JiraProjectKey = project.JiraProjectKey ?? "-";
        return View(issues);
    }

    /// <summary>Trang commit GitHub đã lưu + % đóng góp theo tác giả.</summary>
    public async Task<IActionResult> GitHubCommits(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();

        var vm = await _projectService.GetGitHubCommitsPageAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> ConnectGitHub(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        ViewBag.DefaultRepoUrl = _githubIntegrationOptions.RepoUrl;
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectGitHub([FromForm] string projectId, [FromForm] string? githubToken, [FromForm] string? repoUrl)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();

        if (string.IsNullOrWhiteSpace(githubToken))
        {
            TempData["Error"] =
                "Server không nhận được GitHub token (ô trống). Hãy dán (Ctrl+V) token vào ô PAT, hoặc click vào ô rồi gõ một ký tự xóa đi rồi dán lại — nhiều trình duyệt không gửi ô password khi chỉ tự điền.";
        }
        else
        {
            await _projectService.SaveApiIntegrationAsync(projectId, null, githubToken.Trim());
            TempData["Success"] = "Đã lưu GitHub token.";
        }

        if (!string.IsNullOrWhiteSpace(repoUrl))
            await _projectService.UpsertGitHubRepositoryAsync(projectId, repoUrl.Trim());

        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncJira(string id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        var (count, error) = await _integrationSync.SyncJiraIssuesAsync(id, cancellationToken);
        if (!string.IsNullOrEmpty(error))
            TempData["Error"] = error;
        else if (count == 0)
            TempData["Success"] = "Jira trả về 0 issue (kiểm tra project key KAN và quyền token), hoặc project chưa có issue.";
        else
            TempData["Success"] = $"Đã đồng bộ {count} issue từ Jira.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncGitHub(string id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        var (count, error) = await _integrationSync.SyncGitHubCommitsAsync(id, cancellationToken);
        if (!string.IsNullOrEmpty(error))
            TempData["Error"] = error;
        else if (count == 0)
            TempData["Success"] = "GitHub trả về 0 commit (repo trống hoặc sai owner/repo), hoặc đã đồng bộ hết.";
        else
            TempData["Success"] = $"Đã đồng bộ {count} commit từ GitHub.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
