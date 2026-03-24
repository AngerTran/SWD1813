namespace SWD1813.Services.Interfaces;

/// <summary>Sinh nội dung báo cáo (Markdown, v.v.) — có thể mở rộng thêm loại.</summary>
public interface IReportContentService
{
    /// <summary>Báo cáo tóm tắt: thông tin dự án, Jira, commit, task (theo Dashboard).</summary>
    Task<string?> GenerateProjectSummaryMarkdownAsync(string projectId, CancellationToken cancellationToken = default);
}
