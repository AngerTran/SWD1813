using SWD1813.Models;

namespace SWD1813.Models.ViewModels;

/// <summary>Dữ liệu cho trang Reports/Index (MVC: View nhận ViewModel, không lệch ViewBag).</summary>
public class ReportsIndexVm
{
    public IReadOnlyList<Report> Reports { get; init; } = Array.Empty<Report>();
    public IReadOnlyList<Project> Projects { get; init; } = Array.Empty<Project>();
    public string? SelectedProjectId { get; init; }
}
