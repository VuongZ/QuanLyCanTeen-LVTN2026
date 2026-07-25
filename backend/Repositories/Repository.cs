using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public abstract class Repository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> _dbSet;

    protected Repository(AppDbContext context)
    {
        Context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        return await _dbSet.ToListAsync();
    }

    public abstract Task<T?> GetbyId(int id);

    public async Task Add(T entity)
    {
        _dbSet.Add(entity);
        await Context.SaveChangesAsync();
    }

    public async Task Update(T entity)
    {
        _dbSet.Update(entity);
        await Context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
        {
            return;
        }

        _dbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }
}