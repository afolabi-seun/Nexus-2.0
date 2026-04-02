using System.Linq.Expressions;

namespace UtilityService.Domain.Interfaces.Repositories.Generics;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    IQueryable<T> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    IQueryable<T> FindWithoutFiltersAsync(Expression<Func<T, bool>> predicate);
}
