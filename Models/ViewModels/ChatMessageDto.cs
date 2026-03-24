namespace SWD1813.Models.ViewModels;

/// <summary>Dữ liệu gửi qua SignalR + load lịch sử.</summary>
public class ChatMessageDto
{
    public string MessageId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime SentAt { get; set; }
}
