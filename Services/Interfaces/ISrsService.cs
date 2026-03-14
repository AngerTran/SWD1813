namespace SWD1813.Services.Interfaces;

/// <summary>Sinh nội dung tài liệu SRS từ dữ liệu dự án (nhóm, giảng viên, Jira issues, tasks).</summary>
public interface ISrsService
{
    /// <summary>Sinh nội dung SRS (text/markdown) cho một dự án. Trả về null nếu project không tồn tại hoặc user không có quyền.</summary>
    System.Threading.Tasks.Task<string?> GenerateSrsContentAsync(string projectId, IReadOnlyList<string>? allowedGroupIds = null);
}
