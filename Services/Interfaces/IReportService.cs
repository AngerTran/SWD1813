using SWD1813.Models;
using SWD1813.Models.ViewModels;

namespace SWD1813.Services.Interfaces;

/// <summary>Báo cáo: ghi nhận + truy vấn (tách khỏi Controller theo MVC).</summary>
public interface IReportService
{
    /// <param name="reportId">Nếu có (GUID), dùng làm khóa chính — tiện tạo URL Download trước khi lưu.</param>
    Task<Report> RecordAsync(string projectId, string reportType, string? fileUrl, string userId,
        string? reportId = null, CancellationToken cancellationToken = default);

    /// <summary>Danh sách báo cáo + dropdown dự án (theo quyền nhóm).</summary>
    Task<ReportsIndexVm> GetIndexAsync(string? userId, string? role, string? filterProjectId,
        CancellationToken cancellationToken = default);

    Task<bool> UserCanAccessProjectAsync(string? userId, string? role, string? projectId,
        CancellationToken cancellationToken = default);

    Task<Report?> GetReportByIdAsync(string reportId, CancellationToken cancellationToken = default);

    Task<string?> GetProjectDisplayNameAsync(string projectId, CancellationToken cancellationToken = default);
}
