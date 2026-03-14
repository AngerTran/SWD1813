using Microsoft.EntityFrameworkCore;
using SWD1813.Models;
using SWD1813.Services.Interfaces;

namespace SWD1813.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ProjectManagementContext _context;

    public AuthService(ProjectManagementContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash)) return null;

        // BCrypt hash bắt đầu bằng $2a$ hoặc $2b$; nếu không thì coi là mật khẩu lưu dạng thường (legacy)
        bool valid = false;
        if (user.PasswordHash.StartsWith("$2a$", StringComparison.Ordinal) || user.PasswordHash.StartsWith("$2b$", StringComparison.Ordinal))
            valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        else
            valid = password == user.PasswordHash;

        return valid ? user : null;
    }

    public async System.Threading.Tasks.Task EnsureSeedAdminAsync()
    {
        // Seed 4 tài khoản mặc định (4 role) nếu chưa có user nào
        if (await _context.Users.AnyAsync()) return;

        var defaultPassword = "hash123"; // Lưu dạng thường để đăng nhập nhanh (AuthService hỗ trợ so sánh trực tiếp)
        var now = DateTime.UtcNow;

        var users = new[]
        {
            new User { UserId = Guid.NewGuid().ToString(), Email = "admin@system.com",     PasswordHash = defaultPassword, FullName = "System Admin",   Role = "ADMIN",    CreatedAt = now },
            new User { UserId = Guid.NewGuid().ToString(), Email = "lecturer@university.edu", PasswordHash = defaultPassword, FullName = "Dr Nguyen Van A", Role = "LECTURER", CreatedAt = now },
            new User { UserId = Guid.NewGuid().ToString(), Email = "leader@student.edu",   PasswordHash = defaultPassword, FullName = "Tran Leader",    Role = "LEADER",   CreatedAt = now },
            new User { UserId = Guid.NewGuid().ToString(), Email = "member@student.edu",   PasswordHash = defaultPassword, FullName = "Nguyen Member",  Role = "MEMBER",   CreatedAt = now }
        };

        foreach (var u in users)
            _context.Users.Add(u);
        await _context.SaveChangesAsync();
    }
}
