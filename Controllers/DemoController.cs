using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Models.ViewModels;

namespace SWD1813.Controllers;

/// <summary>Trang demo cố định (thuyết trình / xem trước UI).</summary>
[Authorize]
public class DemoController : Controller
{
    /// <summary>Luôn hiển thị commit + bảng % + biểu đồ thanh (không cần DB / Connect GitHub).</summary>
    [HttpGet]
    public IActionResult GitHubCommitsMau()
    {
        var vm = GitHubCommitsDemoData.Create();
        return View(vm);
    }
}
