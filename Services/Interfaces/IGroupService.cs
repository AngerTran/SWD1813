using SWD1813.Models; // Group, GroupMember, User, Lecturer

namespace SWD1813.Services.Interfaces;

public interface IGroupService
{
    Task<List<Group>> GetAllAsync(string? userId = null, string? userRole = null);
    /// <summary>Danh sách group ID mà user được tham gia (Admin: tất cả; Lecturer: nhóm được gán; Leader/Member: nhóm là thành viên).</summary>
    Task<List<string>> GetGroupIdsUserParticipatesInAsync(string? userId, string? userRole);
    Task<Group?> GetByIdAsync(string id);
    Task<bool> IsLeaderOfGroupAsync(string groupId, string userId);
    Task<Group> CreateAsync(string groupName, string? createdByUserId = null);
    Task<bool> UpdateAsync(string id, string groupName);
    Task<bool> DeleteAsync(string id);
    Task<bool> AssignLecturerAsync(string groupId, string lecturerId);
    Task<bool> AddMemberAsync(string groupId, string userId, string role);
    Task<bool> RemoveMemberAsync(string groupId, string userId);
    Task<List<GroupMember>> GetMembersAsync(string groupId);
    Task<List<Lecturer>> GetLecturersAsync();
    Task<List<User>> GetUsersNotInAnyGroupAsync();
}
