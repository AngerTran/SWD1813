using System;

namespace SWD1813.Models;

/// <summary>Tin nhắn chat theo dự án (lưu DB + đẩy realtime qua SignalR).</summary>
public partial class ChatMessage
{
    public string MessageId { get; set; } = null!;

    public string? ProjectId { get; set; }

    public string? UserId { get; set; }

    /// <summary>Nội dung tin nhắn (giới hạn độ dài ở service).</summary>
    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public virtual Project? Project { get; set; }

    public virtual User? User { get; set; }
}
