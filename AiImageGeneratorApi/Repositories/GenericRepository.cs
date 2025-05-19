using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AiImageGeneratorApi.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public DbSet<T> DbSet => _dbSet;
        public async Task<T> GetByIdAsync(object id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool ignoreQueryFilters = false)
        {
            IQueryable<T> query = _dbSet;
            if (ignoreQueryFilters)
                query = query.IgnoreQueryFilters();

            return await query.FirstOrDefaultAsync(predicate);
        }
        public async Task<IEnumerable<TResult>> GetAllSelectAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector)
        {
            return await _dbSet.Where(predicate).Select(selector).ToListAsync();
        }
        public async Task<IEnumerable<T>> Where(Expression<Func<T, bool>> predicate)
        => await _dbSet.Where(predicate).ToListAsync();

        public async Task<List<TResult>> ExecuteStoredProcedureAsync<TResult>(string sql, params object[] parameters) where TResult : class
        {
            return await _context.Set<TResult>().FromSqlRaw(sql, parameters).ToListAsync();
        }
    }
}
