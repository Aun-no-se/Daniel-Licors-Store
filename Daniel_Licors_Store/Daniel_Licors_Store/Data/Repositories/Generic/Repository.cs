using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data;

namespace Daniel_Licors_Store.Data.Repositories.Generic;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public Repository(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    private ApplicationDbContext CreateContext()
        => _factory.CreateDbContext();

    public async Task<List<T>> GetAllAsync()
    {
        await using var context = CreateContext();
        return await context.Set<T>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        await using var context = CreateContext();
        return await context.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        await using var context = CreateContext();
        return await context.Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await using var context = CreateContext();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        await using var context = CreateContext();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(T entity)
    {
        await using var context = CreateContext();
        context.Set<T>().Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await using var context = CreateContext();
        await context.SaveChangesAsync();
    }

    public IQueryable<T> Query()
    {
        var context = CreateContext();
        return context.Set<T>().AsNoTracking();
    }
}