using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Repositories;
using SWD1813.Services.Interfaces;
using TaskEntity = SWD1813.Models.Task;

namespace SWD1813.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IRepository<JiraIssue> _jiraIssues;
    private readonly IRepository<Models.Repository> _repositories;
    private readonly IRepository<Commit> _commits;
    private readonly IRepository<GroupMember> _groupMembers;
    private readonly IRepository<Project> _projects;
    private readonly IRepository<ContributorStat> _contributorStats;
    private readonly IRepository<TaskEntity> _tasks;

    public DashboardService(
        IRepository<JiraIssue> jiraIssues,
        IRepository<Models.Repository> repositories,
        IRepository<Commit> commits,
        IRepository<GroupMember> groupMembers,
        IRepository<Project> projects,
        IRepository<ContributorStat> contributorStats,
        IRepository<TaskEntity> tasks)
    {
        _jiraIssues = jiraIssues;
        _repositories = repositories;
        _commits = commits;
        _groupMembers = groupMembers;
        _projects = projects;
        _contributorStats = contributorStats;
        _tasks = tasks;
    }

    public async Task<DashboardTaskCompletionVm> GetTaskCompletionAsync(string projectId, string? sprintId = null)
    {
        var issues = _jiraIssues.Query().Where(j => j.ProjectId == projectId);
        if (!string.IsNullOrEmpty(sprintId))
        {
            // Filter by sprint dates if we had sprint info on issues; for now use all
        }
        var total = await issues.CountAsync();
        var done = await issues.CountAsync(j => j.Status == "Done");
        return new DashboardTaskCompletionVm { Total = total, Done = done };
    }

    public async Task<DashboardCommitStatsVm> GetCommitStatsAsync(string projectId)
    {
        var repoIds = await _repositories.Query().Where(r => r.ProjectId == projectId).Select(r => r.RepoId).ToListAsync();
        var commits = await _commits.Query().Where(c => c.RepoId != null && repoIds.Contains(c.RepoId)).ToListAsync();
        return new DashboardCommitStatsVm
        {
            TotalCommits = commits.Count,
            TotalAdditions = commits.Sum(c => c.Additions ?? 0),
            TotalDeletions = commits.Sum(c => c.Deletions ?? 0)
        };
    }

    public async Task<List<DashboardContributionVm>> GetMemberContributionAsync(string groupId)
    {
        var members = await _groupMembers.Query().Where(m => m.GroupId == groupId).Include(m => m.User).ToListAsync();
        var projectIds = await _projects.Query().Where(p => p.GroupId == groupId).Select(p => p.ProjectId).ToListAsync();
        var repoIds = await _repositories.Query().Where(r => r.ProjectId != null && projectIds.Contains(r.ProjectId)).Select(r => r.RepoId).ToListAsync();
        var stats = await _contributorStats.Query().Where(s => s.RepoId != null && repoIds.Contains(s.RepoId)).ToListAsync();
        var tasksDone = await _tasks.Query()
            .Where(t => t.AssignedTo != null && t.Status == "Done" && t.Issue != null && t.Issue.ProjectId != null && projectIds.Contains(t.Issue.ProjectId))
            .GroupBy(t => t.AssignedTo)
            .ToDictionaryAsync(g => g.Key!, g => g.Count());

        var result = new List<DashboardContributionVm>();
        foreach (var m in members.Where(m => m.User != null))
        {
            var uid = m.User!.UserId;
            var totalCommits = stats.Where(s => s.UserId == uid).Sum(s => s.TotalCommits ?? 0);
            var tasksDoneCount = tasksDone.GetValueOrDefault(uid, 0);
            result.Add(new DashboardContributionVm
            {
                UserId = uid,
                FullName = m.User.FullName ?? m.User.Email,
                TotalCommits = totalCommits,
                TasksDone = tasksDoneCount,
                LowContribution = totalCommits == 0
            });
        }
        return result.OrderByDescending(x => x.TotalCommits + x.TasksDone).ToList();
    }
}
