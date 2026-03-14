using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD1813.Services.Interfaces;

namespace SWD1813.Controllers;

[Authorize(Roles = "Admin,ADMIN,Leader,LEADER")]
public class GroupsController : Controller
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdmin => User.IsInRole("Admin") || User.IsInRole("ADMIN");
    private bool IsLeader => User.IsInRole("Leader") || User.IsInRole("LEADER");

    /// <summary>Returns null if allowed, Forbid() if not.</summary>
    private async Task<IActionResult?> EnsureCanManageGroup(string groupId)
    {
        if (IsAdmin) return null;
        if (string.IsNullOrEmpty(CurrentUserId)) return Forbid();
        var can = await _groupService.IsLeaderOfGroupAsync(groupId, CurrentUserId);
        return can ? null : Forbid();
    }

    public async Task<IActionResult> Index()
    {
        var list = await _groupService.GetAllAsync(CurrentUserId, User.IsInRole("ADMIN") ? "ADMIN" : (User.IsInRole("Leader") || User.IsInRole("LEADER") ? "Leader" : null));
        return View(list);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            ModelState.AddModelError("groupName", "Group name is required.");
            return View();
        }
        var createdBy = IsLeader ? CurrentUserId : null;
        await _groupService.CreateAsync(groupName.Trim(), createdBy);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var r = await EnsureCanManageGroup(id);
        if (r != null) return r;
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        ViewBag.Group = group;
        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string groupName)
    {
        var r = await EnsureCanManageGroup(id);
        if (r != null) return r;
        if (string.IsNullOrWhiteSpace(groupName))
        {
            ModelState.AddModelError("groupName", "Group name is required.");
            return View();
        }
        var ok = await _groupService.UpdateAsync(id, groupName.Trim());
        if (!ok) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        if (!IsAdmin) return Forbid(); // Chỉ Admin được xóa nhóm (theo ma trận quyền)
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        return View(group);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        if (!IsAdmin) return Forbid(); // Chỉ Admin được xóa nhóm (theo ma trận quyền)
        var ok = await _groupService.DeleteAsync(id);
        if (!ok) { TempData["Error"] = "Cannot delete group (may have projects or data)."; return RedirectToAction(nameof(Index)); }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AssignLecturer(string id)
    {
        if (!IsAdmin) return Forbid();
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        var lecturers = await _groupService.GetLecturersAsync();
        ViewBag.Group = group;
        ViewBag.Lecturers = lecturers;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignLecturer(string id, string lecturerId)
    {
        if (!IsAdmin) return Forbid();
        var ok = await _groupService.AssignLecturerAsync(id, lecturerId);
        if (!ok) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AddMember(string id)
    {
        var r = await EnsureCanManageGroup(id);
        if (r != null) return r;
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        var users = await _groupService.GetUsersNotInAnyGroupAsync();
        ViewBag.Group = group;
        ViewBag.Users = users;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(string id, string userId, string role)
    {
        var r = await EnsureCanManageGroup(id);
        if (r != null) return r;
        if (string.IsNullOrEmpty(userId) || (role != "Leader" && role != "Member"))
        {
            TempData["Error"] = "Invalid user or role.";
            return RedirectToAction(nameof(AddMember), new { id });
        }
        var ok = await _groupService.AddMemberAsync(id, userId, role);
        if (!ok) { TempData["Error"] = "Cannot add member (user may already be in another group or group needs a Leader first)."; return RedirectToAction(nameof(AddMember), new { id }); }
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(string id)
    {
        var r = await EnsureCanManageGroup(id);
        if (r != null) return r;
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();
        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(string groupId, string userId)
    {
        var r = await EnsureCanManageGroup(groupId);
        if (r != null) return r;
        var ok = await _groupService.RemoveMemberAsync(groupId, userId);
        if (!ok) TempData["Error"] = "Could not remove member.";
        return RedirectToAction(nameof(Details), new { id = groupId });
    }
}
