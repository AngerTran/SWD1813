using SWD1813.Models;
using SWD1813.Models.ViewModels;

namespace SWD1813.Services.Interfaces;

public interface IChatService
{
    /// <summary>Kiểm tra user có thuộc team (group) để vào private chat team.</summary>
    Task<bool> UserCanAccessTeamAsync(string teamId, string? userId, string? role, CancellationToken cancellationToken = default);

    /// <summary>Map project -> team (group) để tương thích flow cũ theo project.</summary>
    Task<string?> ResolveTeamIdByProjectAsync(string projectId, CancellationToken cancellationToken = default);

    Task<List<ChatMessageDto>> GetRecentTeamMessagesAsync(string teamId, int take = 200, CancellationToken cancellationToken = default);

    /// <summary>Lưu private message theo team.</summary>
    Task<ChatMessageDto?> AddTeamMessageAsync(string teamId, string userId, string? role, string? content, CancellationToken cancellationToken = default);

    Task<List<ChatMessageDto>> GetRecentPublicMessagesAsync(int take = 200, CancellationToken cancellationToken = default);

    Task<ChatMessageDto?> AddPublicMessageAsync(string userId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Legacy: kiểm tra user (theo nhóm dự án) được xem/chat project.</summary>
    Task<bool> UserCanAccessProjectAsync(string projectId, string? userId, string? role, CancellationToken cancellationToken = default);

    Task<List<ChatMessageDto>> GetRecentMessagesAsync(string projectId, int take = 200, CancellationToken cancellationToken = default);

    /// <summary>Lưu tin và trả DTO để broadcast (null nếu không có quyền / nội dung rỗng).</summary>
    Task<ChatMessageDto?> AddMessageAsync(string projectId, string userId, string? role, string? content, CancellationToken cancellationToken = default);
}
