using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DanhGiaAPI.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public Repository(AppDbContext context)
        {
            Context = context;
            DbSet = context.Set<TEntity>();
        }

        public async Task<TEntity?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

        public async Task<List<TEntity>> GetAllAsync() => await DbSet.ToListAsync();

        public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
            await DbSet.Where(predicate).ToListAsync();

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) =>
            await DbSet.FirstOrDefaultAsync(predicate);

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) =>
            await DbSet.AnyAsync(predicate);

        public IQueryable<TEntity> Query() => DbSet.AsQueryable();

        public async Task AddAsync(TEntity entity) => await DbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<TEntity> entities) => await DbSet.AddRangeAsync(entities);

        public void Update(TEntity entity) => DbSet.Update(entity);

        public void Remove(TEntity entity) => DbSet.Remove(entity);

        public void RemoveRange(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);
    }
}
