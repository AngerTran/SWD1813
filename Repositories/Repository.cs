using Microsoft.EntityFrameworkCore;
using SWD1813.Models;

namespace SWD1813.Repositories;

public sealed class Repository<T> : IRepository<T> where T : class
{
    private readonly ProjectManagementContext _context;

    public Repository(ProjectManagementContext context)
    {
        _context = context;
    }

    public IQueryable<T> Query() => _context.Set<T>();

    public IQueryable<T> QueryAsNoTracking() => _context.Set<T>().AsNoTracking();

    public async System.Threading.Tasks.Task<T?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync(keyValues, cancellationToken);
    }

    public void Add(T entity) => _context.Set<T>().Add(entity);

    public void AddRange(IEnumerable<T> entities) => _context.Set<T>().AddRange(entities);

    public void Remove(T entity) => _context.Set<T>().Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => _context.Set<T>().RemoveRange(entities);
}
