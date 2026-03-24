namespace SWD1813.Models.ViewModels;

public class ChatWidgetVm
{
    public List<ChatWidgetTeamOption> Teams { get; set; } = new();
    public string CurrentUserId { get; set; } = "";
}

public class ChatWidgetTeamOption
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
}
