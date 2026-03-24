using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Models;
using SWD1813.Models.ViewModels;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

/// <summary>Reports: Controller mỏng — nghiệp vụ trong <see cref="IReportService"/> / <see cref="IReportContentService"/>.</summary>
[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IReportContentService _reportContent;

    public ReportsController(IReportService reportService, IReportContentService reportContent)
    {
        _reportService = reportService;
        _reportContent = reportContent;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    public async Task<IActionResult> Index(string? projectId)
    {
        var vm = await _reportService.GetIndexAsync(CurrentUserId, CurrentUserRole, projectId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProjectSummary(string projectId)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
            return Challenge();
        if (string.IsNullOrWhiteSpace(projectId))
        {
            TempData["Error"] = "Chọn dự án.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _reportService.UserCanAccessProjectAsync(CurrentUserId, CurrentUserRole, projectId))
            return Forbid();

        var rid = Guid.NewGuid().ToString();
        var fileUrl = Url.Action(nameof(Download), "Reports", new { reportId = rid })
                      ?? $"/Reports/Download?reportId={Uri.EscapeDataString(rid)}";
        await _reportService.RecordAsync(projectId, ReportTypes.ProjectSummaryMarkdown, fileUrl, CurrentUserId, rid);
        TempData["Success"] = "Đã tạo báo cáo tóm tắt dự án. Dùng link Download trong bảng để tải file .md.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Download(string reportId)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
            return Challenge();
        if (string.IsNullOrWhiteSpace(reportId))
            return NotFound();

        var report = await _reportService.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (!await _reportService.UserCanAccessProjectAsync(CurrentUserId, CurrentUserRole, report.ProjectId))
            return Forbid();

        if (report.ReportType == ReportTypes.ProjectSummaryMarkdown)
        {
            var md = await _reportContent.GenerateProjectSummaryMarkdownAsync(report.ProjectId!);
            if (md == null)
                return NotFound();
            var name = await _reportService.GetProjectDisplayNameAsync(report.ProjectId!);
            var safe = SanitizeFileSegment(name ?? report.ProjectId!);
            var fileName = $"BaoCaoTomTat_{safe}_{DateTime.UtcNow:yyyyMMdd_HHmm}.md";
            return File(Encoding.UTF8.GetBytes(md), "text/markdown", fileName);
        }

        if (!string.IsNullOrEmpty(report.FileUrl))
        {
            if (report.FileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                report.FileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return Redirect(report.FileUrl);
            return LocalRedirect(report.FileUrl);
        }

        return NotFound();
    }

    private static string SanitizeFileSegment(string s)
    {
        var chars = Path.GetInvalidFileNameChars();
        var t = string.Join("_", s.Split(chars, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrEmpty(t) ? "project" : t[..Math.Min(t.Length, 80)];
    }
}
