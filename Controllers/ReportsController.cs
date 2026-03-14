using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ProjectManagementContext _context;
    private readonly IGroupService _groupService;

    public ReportsController(ProjectManagementContext context, IGroupService groupService)
    {
        _context = context;
        _groupService = groupService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    public async Task<IActionResult> Index(string? projectId)
    {
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var allowedProjectIds = await _context.Projects
            .Where(p => p.GroupId != null && groupIds.Contains(p.GroupId))
            .Select(p => p.ProjectId)
            .ToListAsync();
        var q = _context.Reports.Include(r => r.Project).Where(r => r.ProjectId != null && allowedProjectIds.Contains(r.ProjectId));
        if (!string.IsNullOrEmpty(projectId) && allowedProjectIds.Contains(projectId))
            q = q.Where(r => r.ProjectId == projectId);
        var list = await q.OrderByDescending(r => r.GeneratedAt).ToListAsync();
        var projects = await _context.Projects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).OrderBy(p => p.ProjectName).ToListAsync();
        ViewBag.Projects = projects;
        return View(list);
    }
}
