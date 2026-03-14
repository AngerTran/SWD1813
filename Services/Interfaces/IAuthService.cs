using SWD1813.Models;

namespace SWD1813.Services.Interfaces;

public interface IAuthService
{
    System.Threading.Tasks.Task<User?> ValidateUserAsync(string email, string password);
    System.Threading.Tasks.Task EnsureSeedAdminAsync();
}
