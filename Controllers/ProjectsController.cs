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
    private readonly JiraIntegrationOptions _jiraOptions;

    public ProjectsController(
        IProjectService projectService,
        IGroupService groupService,
        IOptions<JiraIntegrationOptions> jiraOptions)
    {
        _projectService = projectService;
        _groupService = groupService;
        _jiraOptions = jiraOptions.Value;
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
        ViewBag.HasJiraToken = !string.IsNullOrWhiteSpace(integration?.JiraToken)
            || !string.IsNullOrWhiteSpace(_jiraOptions.ApiToken?.Trim());
        return View(project);
    }

    // --- URL cũ (trước khi tách JiraController / GitHubController): redirect 301 ---
    [HttpGet]
    public IActionResult ConnectJira(string id) =>
        RedirectToActionPermanent(nameof(JiraController.ConnectJira), "Jira", new { id });

    [HttpGet]
    public IActionResult JiraIssues(string id) =>
        RedirectToActionPermanent(nameof(JiraController.JiraIssues), "Jira", new { id });

    [HttpGet]
    public IActionResult ConnectGitHub(string id) =>
        RedirectToActionPermanent(nameof(GitHubController.ConnectGitHub), "GitHub", new { id });

    [HttpGet]
    public IActionResult GitHubCommits(string id) =>
        RedirectToActionPermanent(nameof(GitHubController.GitHubCommits), "GitHub", new { id });
}
