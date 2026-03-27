using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Repositories;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize]
public class SrsController : Controller
{
    private readonly IRepository<Project> _projects;
    private readonly IRepository<JiraIssue> _jiraIssues;
    private readonly IGroupService _groupService;
    private readonly ISrsService _srsService;
    private readonly IReportService _reportService;

    public SrsController(
        IRepository<Project> projects,
        IRepository<JiraIssue> jiraIssues,
        IGroupService groupService,
        ISrsService srsService,
        IReportService reportService)
    {
        _projects = projects;
        _jiraIssues = jiraIssues;
        _groupService = groupService;
        _srsService = srsService;
        _reportService = reportService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    public async Task<IActionResult> Index(string? projectId)
    {
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var allProjects = await _projects.Query().Include(p => p.JiraIssues).OrderBy(p => p.ProjectName).ToListAsync();
        var projects = allProjects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).ToList();
        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = projectId;
        if (!string.IsNullOrEmpty(projectId) && projects.Any(p => p.ProjectId == projectId))
        {
            var issues = await _jiraIssues.Query()
                .Where(j => j.ProjectId == projectId && (j.IssueType == "Story" || j.IssueType == "Epic" || j.IssueType == "Task"))
                .OrderBy(j => j.IssueKey)
                .ToListAsync();
            ViewBag.Issues = issues;
        }
        else
        {
            ViewBag.Issues = new List<JiraIssue>();
        }
        return View();
    }

    /// <summary>Tạo tài liệu SRS từ dữ liệu dự án (Jira issues, tasks, nhóm, giảng viên).</summary>
    public async Task<IActionResult> Generate(string? projectId)
    {
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var allProjects = await _projects.Query().Include(p => p.Group).OrderBy(p => p.ProjectName).ToListAsync();
        var projects = allProjects.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId)).ToList();
        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = projectId;
        if (string.IsNullOrEmpty(projectId))
            return View("Generate");
        var content = await _srsService.GenerateSrsContentAsync(projectId, groupIds);
        if (content == null)
            return Forbid();
        ViewBag.SrsContent = content;
        ViewBag.ProjectId = projectId;
        ViewBag.ProjectName = projects.FirstOrDefault(p => p.ProjectId == projectId)?.ProjectName ?? projectId;
        return View("ShowSrs");
    }

    /// <summary>Ghi nhận báo cáo SRS vào danh sách Reports (không cần tải file).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveToReportList(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrEmpty(CurrentUserId))
            return BadRequest();
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var content = await _srsService.GenerateSrsContentAsync(projectId, groupIds);
        if (content == null)
            return Forbid();
        var viewUrl = Url.Action(nameof(Generate), "Srs", new { projectId })
                      ?? $"/Srs/Generate?projectId={Uri.EscapeDataString(projectId)}";
        await _reportService.RecordAsync(projectId, ReportTypes.SrsPreview, viewUrl, CurrentUserId);
        TempData["ReportSaved"] = true;
        return RedirectToAction(nameof(Generate), new { projectId });
    }

    /// <summary>Tải file SRS (.md) đã sinh từ dự án.</summary>
    public async Task<IActionResult> Download(string projectId)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
            return Challenge();
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var content = await _srsService.GenerateSrsContentAsync(projectId, groupIds);
        if (content == null)
            return Forbid();
        var downloadUrl = Url.Action(nameof(Download), "Srs", new { projectId })
                          ?? $"/Srs/Download?projectId={Uri.EscapeDataString(projectId)}";
        await _reportService.RecordAsync(projectId, ReportTypes.SrsMarkdown, downloadUrl, CurrentUserId);
        var project = await _projects.FindAsync([projectId]);
        var fileName = $"SRS_{project?.ProjectName?.Replace(" ", "_") ?? projectId}_{DateTime.Now:yyyyMMdd_HHmm}.md";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return File(bytes, "text/markdown", fileName);
    }
}
