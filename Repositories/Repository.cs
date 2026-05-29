    using LuanVanTotNghiep.Models.Entities;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.EntityFrameworkCore;

    namespace LuanVanTotNghiep.Repositories;
    public abstract class Repository<T> where T : class
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<T> _dbSet;
        public Repository(AppDbContext appContexters)
        {
            Context= appContexters;
            _dbSet=appContexters.Set<T>();
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
                var user = await _dbSet.FindAsync(id);
                if(user!=null)
                {
                _dbSet.Remove(user);
                    await Context.SaveChangesAsync();
                }
            
            }
    }
