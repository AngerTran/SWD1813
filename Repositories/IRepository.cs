namespace SWD1813.Repositories;

/// <summary>Lớp truy cập dữ liệu generic; không dùng DbContext trực tiếp trong service.</summary>
public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    IQueryable<T> QueryAsNoTracking();
    System.Threading.Tasks.Task<T?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}
