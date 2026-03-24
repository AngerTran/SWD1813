using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Models.ViewModels;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class ChatService : IChatService
{
    public const int MaxContentLength = 2000;

    private readonly ProjectManagementContext _context;
    private readonly IGroupService _groupService;

    public ChatService(ProjectManagementContext context, IGroupService groupService)
    {
        _context = context;
        _groupService = groupService;
    }

    public async Task<bool> UserCanAccessTeamAsync(string teamId, string? userId, string? role,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return false;
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(userId, role);
        return groupIds.Contains(teamId);
    }

    public async Task<string?> ResolveTeamIdByProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return null;
        return await _context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.GroupId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ChatMessageDto>> GetRecentTeamMessagesAsync(string teamId, int take = 200,
        CancellationToken cancellationToken = default)
    {
        var anchorProjectId = await ResolveTeamAnchorProjectIdAsync(teamId, cancellationToken);
        if (anchorProjectId == null) return new List<ChatMessageDto>();
        return await GetRecentMessagesAsync(anchorProjectId, take, cancellationToken);
    }

    public async Task<ChatMessageDto?> AddTeamMessageAsync(string teamId, string userId, string? role, string? content,
        CancellationToken cancellationToken = default)
    {
        if (!await UserCanAccessTeamAsync(teamId, userId, role, cancellationToken))
            return null;

        var anchorProjectId = await ResolveTeamAnchorProjectIdAsync(teamId, cancellationToken);
        if (anchorProjectId == null)
            return null;

        return await AddMessageAsync(anchorProjectId, userId, role, content, cancellationToken);
    }

    public async Task<List<ChatMessageDto>> GetRecentPublicMessagesAsync(int take = 200,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.ChatMessages.AsNoTracking()
            .Where(m => m.ProjectId == null)
            .Include(m => m.User)
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        list.Reverse();
        return list.Select(ToDto).ToList();
    }

    public async Task<ChatMessageDto?> AddPublicMessageAsync(string userId, string? content,
        CancellationToken cancellationToken = default)
    {
        var text = (content ?? "").Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > MaxContentLength)
            text = text[..MaxContentLength];

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return null;

        var msg = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            ProjectId = null,
            UserId = userId,
            Content = text,
            SentAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync(cancellationToken);

        return new ChatMessageDto
        {
            MessageId = msg.MessageId,
            UserId = userId,
            DisplayName = user.FullName?.Trim() ?? user.Email ?? userId,
            Content = msg.Content,
            SentAt = msg.SentAt
        };
    }

    public async Task<bool> UserCanAccessProjectAsync(string projectId, string? userId, string? role,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);
        if (project?.GroupId == null) return false;
        var groupIds = await _groupService.GetGroupIdsUserParticipatesInAsync(userId, role);
        return groupIds.Contains(project.GroupId);
    }

    public async Task<List<ChatMessageDto>> GetRecentMessagesAsync(string projectId, int take = 200,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.ChatMessages.AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .Include(m => m.User)
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        list.Reverse();
        return list.Select(ToDto).ToList();
    }

    public async Task<ChatMessageDto?> AddMessageAsync(string projectId, string userId, string? role, string? content,
        CancellationToken cancellationToken = default)
    {
        if (!await UserCanAccessProjectAsync(projectId, userId, role, cancellationToken))
            return null;
        var text = (content ?? "").Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > MaxContentLength)
            text = text[..MaxContentLength];

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return null;

        var msg = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            UserId = userId,
            Content = text,
            SentAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync(cancellationToken);

        return new ChatMessageDto
        {
            MessageId = msg.MessageId,
            UserId = userId,
            DisplayName = user.FullName?.Trim() ?? user.Email ?? userId,
            Content = msg.Content,
            SentAt = msg.SentAt
        };
    }

    private static ChatMessageDto ToDto(ChatMessage m)
    {
        var name = m.User?.FullName?.Trim();
        if (string.IsNullOrEmpty(name)) name = m.User?.Email ?? m.UserId ?? "?";
        return new ChatMessageDto
        {
            MessageId = m.MessageId,
            UserId = m.UserId ?? "",
            DisplayName = name,
            Content = m.Content,
            SentAt = m.SentAt
        };
    }

    /// <summary>
    /// Mỗi team chat private gắn với 1 anchor project (deterministic) để tái sử dụng schema chat_messages hiện tại.
    /// </summary>
    private async Task<string?> ResolveTeamAnchorProjectIdAsync(string teamId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return null;
        return await _context.Projects.AsNoTracking()
            .Where(p => p.GroupId == teamId)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.ProjectId)
            .Select(p => p.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
