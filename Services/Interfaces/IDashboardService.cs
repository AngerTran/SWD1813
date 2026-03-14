namespace SWD1813.Services.Interfaces;

public class DashboardTaskCompletionVm
{
    public int Total { get; set; }
    public int Done { get; set; }
    public double Percentage => Total > 0 ? (double)Done / Total * 100 : 0;
}

public class DashboardCommitStatsVm
{
    public int TotalCommits { get; set; }
    public int TotalAdditions { get; set; }
    public int TotalDeletions { get; set; }
}

public class DashboardContributionVm
{
    public string UserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public int TotalCommits { get; set; }
    public int TasksDone { get; set; }
    public bool LowContribution { get; set; }
}

public interface IDashboardService
{
    Task<DashboardTaskCompletionVm> GetTaskCompletionAsync(string projectId, string? sprintId = null);
    Task<DashboardCommitStatsVm> GetCommitStatsAsync(string projectId);
    Task<List<DashboardContributionVm>> GetMemberContributionAsync(string groupId);
}
