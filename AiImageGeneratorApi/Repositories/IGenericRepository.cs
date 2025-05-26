using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AiImageGeneratorApi.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        DbSet<T> DbSet { get; }
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(object id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> AsQueryable();
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool ignoreQueryFilters = false);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        Task<IEnumerable<T>> Where(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<TResult>> GetAllSelectAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector);
        Task<List<TResult>> ExecuteStoredProcedureAsync<TResult>(string sql, params object[] parameters) where TResult : class;

    }
}
