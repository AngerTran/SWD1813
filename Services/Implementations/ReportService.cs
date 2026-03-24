using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Models.ViewModels;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ProjectManagementContext _context;
    private readonly IGroupService _groupService;

    public ReportService(ProjectManagementContext context, IGroupService groupService)
    {
        _context = context;
        _groupService = groupService;
    }

    public async Task<Report> RecordAsync(string projectId, string reportType, string? fileUrl, string userId,
        string? reportId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Projects.AnyAsync(p => p.ProjectId == projectId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Project không tồn tại.");

        var report = new Report
        {
            ReportId = string.IsNullOrWhiteSpace(reportId) ? Guid.NewGuid().ToString() : reportId,
            ProjectId = projectId,
            ReportType = reportType,
            GeneratedBy = userId,
            FileUrl = fileUrl,
            GeneratedAt = DateTime.UtcNow
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<ReportsIndexVm> GetIndexAsync(string? userId, string? role, string? filterProjectId,
        CancellationToken cancellationToken = default)
    {
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(userId, role);
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.GroupId != null && groupIds.Contains(p.GroupId))
            .OrderBy(p => p.ProjectName)
            .ToListAsync(cancellationToken);

        var allowedProjectIds = projects.Select(p => p.ProjectId).ToList();
        var q = _context.Reports
            .Include(r => r.Project)
            .Where(r => r.ProjectId != null && allowedProjectIds.Contains(r.ProjectId));

        if (!string.IsNullOrEmpty(filterProjectId) && allowedProjectIds.Contains(filterProjectId))
            q = q.Where(r => r.ProjectId == filterProjectId);

        var list = await q.OrderByDescending(r => r.GeneratedAt).ToListAsync(cancellationToken);

        return new ReportsIndexVm
        {
            Reports = list,
            Projects = projects,
            SelectedProjectId = filterProjectId
        };
    }

    public async Task<bool> UserCanAccessProjectAsync(string? userId, string? role, string? projectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(userId)) return false;
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(userId, role);
        return await _context.Projects.AnyAsync(p =>
            p.ProjectId == projectId && p.GroupId != null && groupIds.Contains(p.GroupId), cancellationToken);
    }

    public async Task<Report?> GetReportByIdAsync(string reportId, CancellationToken cancellationToken = default)
    {
        return await _context.Reports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken);
    }

    public async Task<string?> GetProjectDisplayNameAsync(string projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.ProjectName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
