using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IGroupService _groupService;

    public ProjectsController(IProjectService projectService, IGroupService groupService)
    {
        _projectService = projectService;
        _groupService = groupService;
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
        return View(project);
    }

    public async Task<IActionResult> ConnectJira(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        ViewBag.Integration = await _projectService.GetApiIntegrationAsync(id);
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectJira(string projectId, string? jiraProjectKey, string? jiraToken)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        if (!string.IsNullOrWhiteSpace(jiraProjectKey))
            await _projectService.SetJiraProjectKeyAsync(projectId, jiraProjectKey.Trim());
        if (!string.IsNullOrWhiteSpace(jiraToken))
            await _projectService.SaveApiIntegrationAsync(projectId, jiraToken.Trim(), null);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    public async Task<IActionResult> ConnectGitHub(string id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectGitHub(string projectId, string? githubToken)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return NotFound();
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId)) return Forbid();
        if (!string.IsNullOrWhiteSpace(githubToken))
            await _projectService.SaveApiIntegrationAsync(projectId, null, githubToken.Trim());
        return RedirectToAction(nameof(Details), new { id = projectId });
    }
}
