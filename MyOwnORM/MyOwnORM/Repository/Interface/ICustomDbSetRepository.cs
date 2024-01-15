using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM.Repository
{
    public interface ICustomDbSetRepository<T> where T : class
    {
        public Task<IEnumerable<T>> GetAllAsync();
        public Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>> include);
        public Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>>[] includes);
        public Task<IQueryable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
        public Task<T> GetByIdAsync(dynamic id);
        public Task InsertAsync(T obj);
        public Task InsertCascadeAsync(T obj);
        public Task UpdateAsync(T obj);
        public Task UpdateCascadeAsync(T obj);
        public Task DeleteAsync(Expression<Func<T, bool>> predicate);
        public Task DeleteCascadeAsync(Expression<Func<T, bool>> predicate);
        public Task<dynamic> FromSqlRawAsync(string sql);

    }
}
