using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IGroupService _groupService;
    private readonly IChatService _chatService;

    public ChatController(IProjectService projectService, IGroupService groupService, IChatService chatService)
    {
        _projectService = projectService;
        _groupService = groupService;
        _chatService = chatService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    /// <summary>Danh sách dự án để vào phòng chat (SignalR).</summary>
    public async Task<IActionResult> Index()
    {
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        var all = await _projectService.GetAllAsync();
        var list = all.Where(p => p.GroupId != null && groupIds.Contains(p.GroupId))
            .OrderBy(p => p.ProjectName)
            .ToList();
        return View(list);
    }

    /// <summary>Chat realtime theo dự án (SignalR).</summary>
    public async Task<IActionResult> Project(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(CurrentUserId, CurrentUserRole);
        if (project.GroupId == null || !groupIds.Contains(project.GroupId))
            return Forbid();

        var messages = await _chatService.GetRecentMessagesAsync(id);
        ViewBag.ProjectId = id;
        ViewBag.ProjectName = project.ProjectName ?? id;
        ViewBag.CurrentUserId = CurrentUserId ?? "";
        ViewBag.InitialMessagesJson = JsonSerializer.Serialize(messages, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return View();
    }

    /// <summary>JSON lịch sử chat cho widget (gọi AJAX).</summary>
    [HttpGet]
    public async Task<IActionResult> MessagesJson(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return BadRequest();
        if (!await _chatService.UserCanAccessProjectAsync(projectId, CurrentUserId, CurrentUserRole))
            return Forbid();

        var messages = await _chatService.GetRecentMessagesAsync(projectId, 80);
        return new JsonResult(messages, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    [HttpGet]
    public async Task<IActionResult> TeamMessagesJson(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return BadRequest();
        if (!await _chatService.UserCanAccessTeamAsync(teamId, CurrentUserId, CurrentUserRole))
            return Forbid();

        var messages = await _chatService.GetRecentTeamMessagesAsync(teamId, 80);// 80 tin nhan gan nhat 
        return new JsonResult(messages, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    [HttpGet]
    public async Task<IActionResult> PublicMessagesJson()
    {
        var messages = await _chatService.GetRecentPublicMessagesAsync(80);
        return new JsonResult(messages, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}
