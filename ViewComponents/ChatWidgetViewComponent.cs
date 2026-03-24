using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Models.ViewModels;
using SWD1813.Services.Interfaces;

namespace SWD1813.ViewComponents;

public class ChatWidgetViewComponent : ViewComponent
{
    private readonly IGroupService _groupService;

    public ChatWidgetViewComponent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Content(string.Empty);

        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = UserClaimsPrincipal.FindFirstValue(ClaimTypes.Role);
        var groups = await _groupService.GetAllAsync(userId, role);
        var teams = groups
            .OrderBy(g => g.GroupName)
            .Select(g => new ChatWidgetTeamOption
            {
                TeamId = g.GroupId,
                TeamName = g.GroupName ?? g.GroupId
            })
            .ToList();

        var vm = new ChatWidgetVm
        {
            Teams = teams,
            CurrentUserId = userId ?? ""
        };
        return View(vm);
    }
}
