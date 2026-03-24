using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SWD1813.Configuration;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

public class GitHubController : ProjectIntegrationControllerBase
{
    private readonly IIntegrationSyncService _integrationSync;
    private readonly GitHubIntegrationOptions _githubOptions;

    public GitHubController(
        IProjectService projectService,
        IGroupService groupService,
        IIntegrationSyncService integrationSync,
        IOptions<GitHubIntegrationOptions> githubOptions)
        : base(projectService, groupService)
    {
        _integrationSync = integrationSync;
        _githubOptions = githubOptions.Value;
    }

    public async Task<IActionResult> ConnectGitHub(string id)
    {
        var (project, err) = await TryGetAccessibleProjectAsync(id);
        if (err != null) return err;
        ViewBag.DefaultRepoUrl = _githubOptions.RepoUrl;
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectGitHub([FromForm] string projectId, [FromForm] string? githubToken, [FromForm] string? repoUrl)
    {
        var (_, err) = await TryGetAccessibleProjectAsync(projectId);
        if (err != null) return err;

        var tokenTrim = githubToken?.Trim();
        var repoTrim = repoUrl?.Trim();

        var savedToken = false;
        var savedRepo = false;
        if (!string.IsNullOrWhiteSpace(tokenTrim))
        {
            await ProjectService.SaveApiIntegrationAsync(projectId, null, tokenTrim);
            savedToken = true;
        }

        if (!string.IsNullOrWhiteSpace(repoTrim))
        {
            await ProjectService.UpsertGitHubRepositoryAsync(projectId, repoTrim);
            savedRepo = true;
        }

        if (savedToken && savedRepo)
            TempData["Success"] = "Đã lưu GitHub token và URL repository.";
        else if (savedToken)
            TempData["Success"] = "Đã lưu GitHub token.";
        else if (savedRepo)
            TempData["Success"] = "Đã lưu URL repository (repo public có thể đồng bộ không cần PAT).";

        if (!savedToken && !savedRepo)
        {
            TempData["Error"] =
                "Chưa lưu gì: nhập URL repo và/hoặc PAT. Nếu dán PAT, dùng ô text và Ctrl+V (xem gợi ý trên form).";
        }

        return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id = projectId });
    }

    public async Task<IActionResult> GitHubCommits(string id)
    {
        var (project, err) = await TryGetAccessibleProjectAsync(id);
        if (err != null) return err;

        var vm = await ProjectService.GetGitHubCommitsPageAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncGitHub(string id, CancellationToken cancellationToken)
    {
        var (_, err) = await TryGetAccessibleProjectAsync(id);
        if (err != null) return err;

        var (count, syncError) = await _integrationSync.SyncGitHubCommitsAsync(id, cancellationToken);
        if (!string.IsNullOrEmpty(syncError))
            TempData["Error"] = syncError;
        else if (count == 0)
            TempData["Success"] = "GitHub trả về 0 commit (repo trống hoặc sai owner/repo), hoặc đã đồng bộ hết.";
        else
            TempData["Success"] = $"Đã đồng bộ {count} commit từ GitHub.";
        return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id });
    }
}
