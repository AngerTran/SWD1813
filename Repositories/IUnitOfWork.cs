namespace SWD1813.Repositories;

/// <summary>Gom SaveChanges và thao tác SQL cấp DB (schema tối thiểu) — DbContext chỉ dùng trong lớp infrastructure.</summary>
public interface IUnitOfWork
{
    System.Threading.Tasks.Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default);
}
