using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IGroupService _groupService;
    private readonly IProjectService _projectService;

    public DashboardController(IDashboardService dashboardService, IGroupService groupService, IProjectService projectService)
    {
        _dashboardService = dashboardService;
        _groupService = groupService;
        _projectService = projectService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    private async Task<List<string>> GetUserGroupIdsAsync()
    {
        return await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
    }

    public async Task<IActionResult> Index(string? groupId, string? projectId)
    {
        var groupIds = await GetUserGroupIdsAsync();
        var groups = await _groupService.GetAllAsync(CurrentUserId, CurrentUserRole);
        var allProjects = await _projectService.GetAllAsync(groupId);
        var projects = allProjects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).ToList();
        ViewBag.Groups = groups;
        ViewBag.Projects = projects;
        ViewBag.SelectedGroupId = groupId;
        ViewBag.SelectedProjectId = projectId;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TaskCompletion(string projectId, string? sprintId)
    {
        var groupIds = await GetUserGroupIdsAsync();
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null || project.GroupId == null || !groupIds.Contains(project.GroupId))
            return Json(new object());
        var vm = await _dashboardService.GetTaskCompletionAsync(projectId, sprintId);
        return Json(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CommitStats(string projectId)
    {
        var groupIds = await GetUserGroupIdsAsync();
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null || project.GroupId == null || !groupIds.Contains(project.GroupId))
            return Json(new object());
        var vm = await _dashboardService.GetCommitStatsAsync(projectId);
        return Json(vm);
    }

    [HttpGet]
    public async Task<IActionResult> MemberContribution(string groupId)
    {
        var groupIds = await GetUserGroupIdsAsync();
        if (!groupIds.Contains(groupId))
            return Json(Array.Empty<object>());
        var list = await _dashboardService.GetMemberContributionAsync(groupId);
        return Json(list);
    }
}
