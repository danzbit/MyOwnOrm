using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM.Interface
{
    public interface ICustomDbSetRepository<T, TKey> where T : class
    {
        public Task<IEnumerable<T>> GetAllAsync();
        public Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>> include);
        public Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>>[] includes);
        public Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
        public Task<T> GetByIdAsync(TKey id);
        public Task InsertAsync(object obj);
        public Task InsertCascadeAsync(T obj);
        public Task UpdateAsync(T obj);
        public Task UpdateCascadeAsync(T obj);
        public Task DeleteAsync(Expression<Func<T, bool>> predicate);
        public Task DeleteByIdAsync(TKey id);
        public Task DeleteCascadeAsync(Expression<Func<T, bool>> predicate);
        public Task<object> FromSqlRawAsync(string sql);

    }
}
