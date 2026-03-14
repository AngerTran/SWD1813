using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class SrsService : ISrsService
{
    private readonly ProjectManagementContext _context;

    public SrsService(ProjectManagementContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<string?> GenerateSrsContentAsync(string projectId, IReadOnlyList<string>? allowedGroupIds = null)
    {
        var project = await _context.Projects
            .Include(p => p.Group)
            .ThenInclude(g => g!.Lecturer)
            .ThenInclude(l => l!.User)
            .Include(p => p.JiraIssues)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
        if (project == null) return null;
        if (allowedGroupIds != null && (project.GroupId == null || !allowedGroupIds.Contains(project.GroupId)))
            return null;

        var issues = await _context.JiraIssues
            .Where(j => j.ProjectId == projectId && (j.IssueType == "Story" || j.IssueType == "Epic" || j.IssueType == "Task"))
            .OrderBy(j => j.IssueKey)
            .ToListAsync();
        var taskDict = await _context.Tasks
            .Where(t => t.IssueId != null && issues.Select(i => i.IssueId).Contains(t.IssueId))
            .Include(t => t.AssignedToNavigation)
            .ToDictionaryAsync(t => t.IssueId!, t => t);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# ĐẶC TẢ YÊU CẦU PHẦN MỀM (SRS)");
        sb.AppendLine($"# Dự án: {project.ProjectName ?? project.ProjectId}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 1. Thông tin dự án");
        sb.AppendLine();
        sb.AppendLine($"| Mục | Nội dung |");
        sb.AppendLine($"|-----|----------|");
        sb.AppendLine($"| **Tên dự án** | {project.ProjectName ?? "-"} |");
        sb.AppendLine($"| **Nhóm** | {project.Group?.GroupName ?? "-"} |");
        sb.AppendLine($"| **Giảng viên phụ trách** | {project.Group?.Lecturer?.User?.FullName ?? "Chưa gán"} |");
        sb.AppendLine($"| **Jira Project Key** | {project.JiraProjectKey ?? "-"} |");
        sb.AppendLine($"| **Thời gian** | {(project.StartDate?.ToString("yyyy-MM-dd") ?? "-")} → {(project.EndDate?.ToString("yyyy-MM-dd") ?? "-")} |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 2. Danh sách yêu cầu (Requirements từ Jira)");
        sb.AppendLine();
        sb.AppendLine("| STT | Key | Mô tả | Loại | Ưu tiên | Trạng thái | Người phụ trách |");
        sb.AppendLine("|-----|-----|-------|------|---------|------------|------------------|");
        var stt = 1;
        foreach (var i in issues)
        {
            var task = taskDict.GetValueOrDefault(i.IssueId!);
            var assignee = task?.AssignedToNavigation?.FullName ?? i.Assignee ?? "-";
            var summary = (i.Summary ?? "-").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
            if (summary.Length > 60) summary = summary.Substring(0, 57) + "...";
            sb.AppendLine($"| {stt} | {i.IssueKey ?? "-"} | {summary} | {i.IssueType ?? "-"} | {i.Priority ?? "-"} | {i.Status ?? "-"} | {assignee} |");
            stt++;
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 3. Chi tiết yêu cầu");
        sb.AppendLine();
        stt = 1;
        foreach (var i in issues)
        {
            var task = taskDict.GetValueOrDefault(i.IssueId!);
            sb.AppendLine($"### {stt}. [{i.IssueKey}] {i.Summary ?? "(Không có tiêu đề)"}");
            sb.AppendLine();
            sb.AppendLine($"- **Loại:** {i.IssueType ?? "-"}");
            sb.AppendLine($"- **Ưu tiên:** {i.Priority ?? "-"}");
            sb.AppendLine($"- **Trạng thái:** {i.Status ?? "-"}");
            sb.AppendLine($"- **Người phụ trách:** {task?.AssignedToNavigation?.FullName ?? i.Assignee ?? "-"}");
            if (task?.Deadline != null)
                sb.AppendLine($"- **Deadline:** {task.Deadline:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(i.Description))
            {
                sb.AppendLine("- **Mô tả:**");
                sb.AppendLine("```");
                sb.AppendLine(i.Description.Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
            stt++;
        }
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Tài liệu được sinh từ SWP Tracker – {DateTime.Now:yyyy-MM-dd HH:mm}*");
        return sb.ToString();
    }
}
