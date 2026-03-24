using System.Text;
using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class ReportContentService : IReportContentService
{
    private readonly ProjectManagementContext _context;
    private readonly IDashboardService _dashboard;

    public ReportContentService(ProjectManagementContext context, IDashboardService dashboard)
    {
        _context = context;
        _dashboard = dashboard;
    }

    public async Task<string?> GenerateProjectSummaryMarkdownAsync(string projectId,
        CancellationToken cancellationToken = default)
    {
        var p = await _context.Projects
            .AsNoTracking()
            .Include(x => x.Group)
            .ThenInclude(g => g!.Lecturer)
            .ThenInclude(l => l!.User)
            .Include(x => x.Repositories)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
        if (p == null) return null;

        var taskVm = await _dashboard.GetTaskCompletionAsync(projectId);
        var commitVm = await _dashboard.GetCommitStatsAsync(projectId);
        var jiraTotal = await _context.JiraIssues.AsNoTracking().CountAsync(j => j.ProjectId == projectId, cancellationToken);
        var repoCount = p.Repositories?.Count ?? 0;

        var sb = new StringBuilder();
        sb.AppendLine($"# Báo cáo tóm tắt dự án");
        sb.AppendLine();
        sb.AppendLine($"| Trường | Giá trị |");
        sb.AppendLine("|--------|---------|");
        sb.AppendLine($"| Tên dự án | {EscapeMd(p.ProjectName)} |");
        sb.AppendLine($"| Nhóm | {EscapeMd(p.Group?.GroupName ?? "—")} |");
        sb.AppendLine(
            $"| Giảng viên | {EscapeMd(p.Group?.Lecturer?.User?.FullName ?? p.Group?.Lecturer?.User?.Email ?? "—")} |");
        sb.AppendLine($"| Jira key | {EscapeMd(p.JiraProjectKey ?? "—")} |");
        sb.AppendLine(
            $"| Thời gian | {(p.StartDate?.ToString("yyyy-MM-dd") ?? "—")} → {(p.EndDate?.ToString("yyyy-MM-dd") ?? "—")} |");
        sb.AppendLine($"| Số repository | {repoCount} |");
        sb.AppendLine($"| Issue Jira (trong DB) | {jiraTotal} |");
        sb.AppendLine(
            $"| Task Jira (Done / tổng) | {taskVm.Done} / {taskVm.Total} ({taskVm.Percentage:F1}%) |");
        sb.AppendLine(
            $"| Commit (tổng / + / −) | {commitVm.TotalCommits} / {commitVm.TotalAdditions} / {commitVm.TotalDeletions} |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Sinh tự động từ SWD1813 — {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");
        return sb.ToString();
    }

    private static string EscapeMd(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "—";
        return s.Replace("|", "\\|").Replace("\n", " ");
    }
}
