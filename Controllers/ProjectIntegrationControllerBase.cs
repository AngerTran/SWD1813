using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

/// <summary>Chung cho Jira/GitHub: kiểm tra user thuộc nhóm của dự án.</summary>
[Authorize]
public abstract class ProjectIntegrationControllerBase : Controller
{
    protected readonly IProjectService ProjectService;
    protected readonly IGroupService GroupService;

    protected ProjectIntegrationControllerBase(IProjectService projectService, IGroupService groupService)
    {
        ProjectService = projectService;
        GroupService = groupService;
    }

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    protected string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    protected async Task<List<string>> GetUserGroupIdsAsync()
    {
        return await GroupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
    }

    /// <summary>Trả về dự án nếu user có quyền (cùng nhóm); ngược lại NotFound/Forbid.</summary>
    protected async Task<(Project? Project, IActionResult? Error)> TryGetAccessibleProjectAsync(string projectId)
    {
        var project = await ProjectService.GetByIdAsync(projectId);
        if (project == null)
            return (null, NotFound());
        var groupIds = await GetUserGroupIdsAsync();
        if (project.GroupId == null || !groupIds.Contains(project.GroupId))
            return (null, Forbid());
        return (project, null);
    }
}
