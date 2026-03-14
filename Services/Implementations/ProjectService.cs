using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly ProjectManagementContext _context;

    public ProjectService(ProjectManagementContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<List<Project>> GetAllAsync(string? groupId = null)
    {
        var q = _context.Projects
            .Include(p => p.Group)
            .AsQueryable();
        if (!string.IsNullOrEmpty(groupId))
            q = q.Where(p => p.GroupId == groupId);
        return await q.OrderBy(p => p.ProjectName).ToListAsync();
    }

    public async System.Threading.Tasks.Task<Project?> GetByIdAsync(string id)
    {
        return await _context.Projects
            .Include(p => p.Group)
            .ThenInclude(g => g!.Lecturer)
            .ThenInclude(l => l!.User)
            .Include(p => p.JiraIssues)
            .Include(p => p.Repositories)
            .Include(p => p.ApiIntegrations)
            .FirstOrDefaultAsync(p => p.ProjectId == id);
    }

    public async System.Threading.Tasks.Task<Project> CreateAsync(string projectName, string groupId, DateOnly? startDate, DateOnly? endDate, string? jiraProjectKey = null)
    {
        var project = new Project
        {
            ProjectId = Guid.NewGuid().ToString(),
            ProjectName = projectName,
            GroupId = groupId,
            JiraProjectKey = jiraProjectKey,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async System.Threading.Tasks.Task<bool> UpdateAsync(string id, string projectName, DateOnly? startDate, DateOnly? endDate)
    {
        var p = await _context.Projects.FindAsync(id);
        if (p == null) return false;
        p.ProjectName = projectName;
        p.StartDate = startDate;
        p.EndDate = endDate;
        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<bool> SetJiraProjectKeyAsync(string projectId, string jiraProjectKey)
    {
        var p = await _context.Projects.FindAsync(projectId);
        if (p == null) return false;
        p.JiraProjectKey = jiraProjectKey;
        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<ApiIntegration?> GetApiIntegrationAsync(string projectId)
    {
        return await _context.ApiIntegrations.FirstOrDefaultAsync(a => a.ProjectId == projectId);
    }

    public async System.Threading.Tasks.Task SaveApiIntegrationAsync(string projectId, string? jiraToken, string? githubToken)
    {
        var existing = await _context.ApiIntegrations.FirstOrDefaultAsync(a => a.ProjectId == projectId);
        if (existing != null)
        {
            if (jiraToken != null) existing.JiraToken = jiraToken;
            if (githubToken != null) existing.GithubToken = githubToken;
        }
        else
        {
            _context.ApiIntegrations.Add(new ApiIntegration
            {
                IntegrationId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                JiraToken = jiraToken,
                GithubToken = githubToken,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task<List<JiraIssue>> GetJiraIssuesByProjectAsync(string projectId)
    {
        return await _context.JiraIssues
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.IssueKey)
            .ToListAsync();
    }
}
