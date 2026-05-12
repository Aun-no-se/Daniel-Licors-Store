using System.Linq.Expressions;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();

    Task<T?> GetByIdAsync(object id);

    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

    Task AddAsync(T entity);

    Task UpdateAsync(T entity);

    Task RemoveAsync(T entity);

    Task SaveChangesAsync();

    IQueryable<T> Query();
}