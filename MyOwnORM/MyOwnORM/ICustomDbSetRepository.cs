using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public interface ICustomDbSetRepository<T> where T : class
    {
        public IEnumerable<T> GetAll();
        public IEnumerable<T> Include(Expression<Func<T, object>> include);
        public IEnumerable<T> Include(Expression<Func<T, object>>[] includes);
        public IQueryable<T> Where(Expression<Func<T, bool>> predicate);
        public T GetById(dynamic id);
        public void Insert(T obj);
        public void InsertCascade(T obj);
        public void Update(T obj);
        public void UpdateCascade(T obj);
        public void Delete(Expression<Func<T, bool>> predicate);
        public void DeleteCascade(Expression<Func<T, bool>> predicate);
        public dynamic FromSqlRaw(string sql);
    }
}
