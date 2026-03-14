using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

/// <summary>View Task: Lecturer, Leader, Member. Create/Assign Task: chỉ Leader. Update Task: Leader + Member (chỉ assignee). Admin không có quyền Tasks (theo ma trận).</summary>
[Authorize(Roles = "Lecturer,LECTURER,Leader,LEADER,Member,MEMBER")]
public class TasksController : Controller
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IGroupService _groupService;

    public TasksController(ITaskService taskService, IProjectService projectService, IGroupService groupService)
    {
        _taskService = taskService;
        _projectService = projectService;
        _groupService = groupService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);
    /// <summary>Create Task / Assign Task: chỉ Leader (Admin không có theo ma trận).</summary>
    private bool CanCreateOrAssignTask => User.IsInRole("Leader") || User.IsInRole("LEADER");

    public async Task<IActionResult> Index(string? projectId, string? groupId)
    {
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var groups = await _groupService.GetAllAsync(CurrentUserId, CurrentUserRole);
        var allProjects = await _projectService.GetAllAsync();
        var projects = allProjects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).ToList();
        List<SWD1813.Models.Task> list;
        if (!string.IsNullOrEmpty(projectId))
        {
            var proj = await _projectService.GetByIdAsync(projectId);
            if (proj == null || proj.GroupId == null || !groupIds.Contains(proj.GroupId))
                list = new List<SWD1813.Models.Task>();
            else
                list = await _taskService.GetByProjectAsync(projectId);
        }
        else if (!string.IsNullOrEmpty(groupId))
        {
            if (!groupIds.Contains(groupId))
                list = new List<SWD1813.Models.Task>();
            else
                list = await _taskService.GetByGroupAsync(groupId);
        }
        else
        {
            var projectIds = projects.Select(p => p.ProjectId).ToList();
            list = await _taskService.GetByProjectIdsAsync(projectIds);
        }
        ViewBag.Projects = projects;
        ViewBag.Groups = groups;
        ViewBag.CanCreateTask = CanCreateOrAssignTask;
        ViewBag.CurrentUserId = CurrentUserId;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> GetIssues(string projectId)
    {
        if (string.IsNullOrEmpty(projectId)) return Json(Array.Empty<object>());
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null || project.GroupId == null || !groupIds.Contains(project.GroupId))
            return Json(Array.Empty<object>());
        var issues = await _projectService.GetJiraIssuesByProjectAsync(projectId);
        return Json(issues.Select(i => new { i.IssueId, i.IssueKey, i.Summary }));
    }

    public async Task<IActionResult> Create()
    {
        if (!CanCreateOrAssignTask) return Forbid();
        var groups = await _groupService.GetAllAsync(CurrentUserId, CurrentUserRole);
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var allProjects = await _projectService.GetAllAsync();
        var projects = allProjects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).ToList();
        var groupsData = new List<object>();
        foreach (var g in groups)
        {
            var members = await _groupService.GetMembersAsync(g.GroupId);
            groupsData.Add(new { g.GroupId, g.GroupName, Members = members.Select(m => new { m.UserId, FullName = m.User?.FullName ?? m.UserId }).ToList() });
        }
        ViewBag.Groups = groups;
        var jsonOpt = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        ViewBag.GroupsDataJson = System.Text.Json.JsonSerializer.Serialize(groupsData, jsonOpt);
        ViewBag.ProjectsDataJson = System.Text.Json.JsonSerializer.Serialize(projects.Select(p => new { p.ProjectId, p.ProjectName, p.GroupId }).ToList(), jsonOpt);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string issueId, string assignedTo, DateOnly? deadline)
    {
        if (!CanCreateOrAssignTask) return Forbid();
        if (string.IsNullOrEmpty(issueId) || string.IsNullOrEmpty(assignedTo))
        {
            TempData["Error"] = "Chọn issue và thành viên.";
            return RedirectToAction(nameof(Create));
        }
        var task = await _taskService.CreateTaskAsync(issueId, assignedTo, deadline);
        if (task == null) { TempData["Error"] = "Issue không tồn tại."; return RedirectToAction(nameof(Create)); }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Assign(string id)
    {
        if (!CanCreateOrAssignTask) return Forbid();
        var task = await _taskService.GetByIdAsync(id);
        if (task == null) return NotFound();
        var project = task.Issue?.ProjectId != null ? await _projectService.GetByIdAsync(task.Issue.ProjectId) : null;
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        if (project?.GroupId == null || !groupIds.Contains(project.GroupId))
            return Forbid();
        var groupId = project.GroupId;
        var members = await _groupService.GetMembersAsync(groupId);
        ViewBag.Task = task;
        ViewBag.Members = members;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(string taskId, string userId, DateOnly? deadline)
    {
        if (!CanCreateOrAssignTask) return Forbid();
        var task = await _taskService.GetByIdAsync(taskId);
        if (task == null) return NotFound();
        var project = task.Issue?.ProjectId != null ? await _projectService.GetByIdAsync(task.Issue.ProjectId) : null;
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        if (project?.GroupId == null || !groupIds.Contains(project.GroupId))
            return Forbid();
        var ok = await _taskService.AssignTaskAsync(taskId, userId, deadline);
        if (!ok) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string taskId, string status)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task == null) return NotFound();
        var project = task.Issue?.ProjectId != null ? await _projectService.GetByIdAsync(task.Issue.ProjectId) : null;
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        if (project?.GroupId == null || !groupIds.Contains(project.GroupId))
            return Forbid();
        var ok = await _taskService.UpdateStatusAsync(taskId, status, CurrentUserId);
        if (!ok) { TempData["Error"] = "Chỉ thành viên được giao task mới có thể đổi trạng thái."; return RedirectToAction(nameof(Index)); }
        return RedirectToAction(nameof(Index));
    }
}
