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
        var emailNorm = (email ?? "").Trim();
        if (string.IsNullOrEmpty(emailNorm)) return null;
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == emailNorm);
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

    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
        { "Admin", "ADMIN", "Lecturer", "LECTURER", "Leader", "LEADER", "Member", "MEMBER" };

    public async System.Threading.Tasks.Task<(User? User, string? ErrorMessage)> RegisterAsync(RegisterViewModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Role))
            return (null, "Vai trò (role) là bắt buộc.");

        var roleNormalized = model.Role.Trim();
        if (!AllowedRoles.Contains(roleNormalized))
            return (null, "Vai trò không hợp lệ. Chọn: Admin, Lecturer, Leader hoặc Member.");

        var email = model.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return (null, "Email là bắt buộc.");

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return (null, "Email này đã được sử dụng. Vui lòng dùng email khác hoặc đăng nhập.");

        var fullName = model.FullName?.Trim() ?? "";
        if (fullName.Length > 255) fullName = fullName[..255];

        var roleForDb = roleNormalized.ToUpperInvariant();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password!, workFactor: 10);
        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = roleForDb,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return (user, null);
    }
}
