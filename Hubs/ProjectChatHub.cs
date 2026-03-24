using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SWD1813.Services.Interfaces;

namespace SWD1813.Hubs;

/// <summary>Chat realtime theo từng dự án (nhóm SignalR = project-{projectId}).</summary>
[Authorize]
public class ProjectChatHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProjectChatHub> _logger;

    public ProjectChatHub(IServiceScopeFactory scopeFactory, ILogger<ProjectChatHub> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private string? UserId => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? Role => Context.User?.FindFirstValue(ClaimTypes.Role);

    /// <summary>Client gọi sau khi connect để nhận tin từ room dự án.</summary>
    public async Task JoinProjectChat(string? projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            await Clients.Caller.SendAsync("ChatError", "Thiếu mã dự án.");
            return;
        }

        var uid = UserId;
        if (string.IsNullOrEmpty(uid))
        {
            await Clients.Caller.SendAsync("ChatError", "Chưa đăng nhập.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
        var teamId = await chat.ResolveTeamIdByProjectAsync(projectId);
        if (string.IsNullOrWhiteSpace(teamId) || !await chat.UserCanAccessTeamAsync(teamId, uid, Role))
        {
            await Clients.Caller.SendAsync("ChatError", "Bạn không có quyền tham gia chat dự án này.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TeamGroupName(teamId));
        await Clients.Caller.SendAsync("Joined", projectId);
    }

    /// <summary>Rời phòng khi đổi dự án trong widget (tránh nhận tin nhầm project).</summary>
    public async Task LeaveProjectChat(string? projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
        var teamId = await chat.ResolveTeamIdByProjectAsync(projectId);
        if (string.IsNullOrWhiteSpace(teamId))
            return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamGroupName(teamId));
    }

    public async Task SendChat(string? projectId, string? message)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid))
        {
            await Clients.Caller.SendAsync("ChatError", "Chưa đăng nhập.");
            return;
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            await Clients.Caller.SendAsync("ChatError", "Thiếu mã dự án.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
        var teamId = await chat.ResolveTeamIdByProjectAsync(projectId);
        if (string.IsNullOrWhiteSpace(teamId))
        {
            await Clients.Caller.SendAsync("ChatError", "Không xác định được Team của dự án.");
            return;
        }

        var dto = await chat.AddTeamMessageAsync(teamId, uid, Role, message);
        if (dto == null)
        {
            await Clients.Caller.SendAsync("ChatError", "Không gửi được tin (kiểm tra quyền hoặc nội dung).");
            return;
        }

        await Clients.Group(TeamGroupName(teamId)).SendAsync("ReceiveChat", dto);
    }

    public async Task JoinTeamChat(string? teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            await Clients.Caller.SendAsync("ChatError", "Thiếu TeamId.");
            return;
        }

        var uid = UserId;
        if (string.IsNullOrEmpty(uid))
        {
            await Clients.Caller.SendAsync("ChatError", "Chưa đăng nhập.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
        if (!await chat.UserCanAccessTeamAsync(teamId, uid, Role))
        {
            await Clients.Caller.SendAsync("ChatError", "Bạn không có quyền vào chat nhóm này.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TeamGroupName(teamId));
        await Clients.Caller.SendAsync("JoinedTeam", teamId);
    }

    public Task LeaveTeamChat(string? teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return Task.CompletedTask;
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamGroupName(teamId));
    }

    public async Task SendTeamChat(string? teamId, string? message)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid))
        {
            await Clients.Caller.SendAsync("ChatError", "Chưa đăng nhập.");
            return;
        }
        if (string.IsNullOrWhiteSpace(teamId))
        {
            await Clients.Caller.SendAsync("ChatError", "Thiếu TeamId.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
        var dto = await chat.AddTeamMessageAsync(teamId, uid, Role, message);
        if (dto == null)
        {
            await Clients.Caller.SendAsync("ChatError", "Không gửi được tin (kiểm tra quyền hoặc nội dung).");
            return;
        }

        await Clients.Group(TeamGroupName(teamId)).SendAsync("ReceiveChat", dto);
    }

    public Task JoinPublicChat()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, PublicGroupName);
    }

    public Task LeavePublicChat()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, PublicGroupName);
    }

    public async Task SendPublicChat(string? message)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid))
        {
            await Clients.Caller.SendAsync("ChatError", "Chưa đăng nhập.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
        var dto = await chat.AddPublicMessageAsync(uid, message);
        if (dto == null)
        {
            await Clients.Caller.SendAsync("ChatError", "Không gửi được tin (nội dung rỗng / quá dài).");
            return;
        }

        await Clients.Group(PublicGroupName).SendAsync("ReceivePublicChat", dto);
    }

    public static string GroupName(string projectId) => "project-" + projectId;
    public static string TeamGroupName(string teamId) => "team-" + teamId;
    public const string PublicGroupName = "public-community";

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            _logger.LogDebug(exception, "SignalR chat disconnect");
        return base.OnDisconnectedAsync(exception);
    }
}
