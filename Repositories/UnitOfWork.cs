using Microsoft.EntityFrameworkCore;
using SWD1813.Models;

namespace SWD1813.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ProjectManagementContext _context;

    public UnitOfWork(ProjectManagementContext context)
    {
        _context = context;
    }

    public System.Threading.Tasks.Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public System.Threading.Tasks.Task ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default) =>
        _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
}
