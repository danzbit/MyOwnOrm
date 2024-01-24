using MyOwnORM.Helper;
using MyOwnORM.Interface;
using MyOwnORM.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM.Implementations
{
    public class CustomDbSetRepository<T, TKey> : ICustomDbSetRepository<T, TKey> where T : class
    {
        private string _connectionString;
        private string tableName;
        private readonly CustomDbSetService<T, TKey> dbSetExtension;
        private readonly CustomDbSetReflection<T, TKey> dbSetReflection;
        public CustomDbSetRepository(string connectionString)
        {
            _connectionString = connectionString;
            dbSetExtension = new CustomDbSetService<T, TKey>(_connectionString);
            dbSetReflection = new CustomDbSetReflection<T, TKey>(_connectionString);
            tableName = dbSetReflection.GetTableName();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await dbSetExtension.GetAllAsyncQuery(tableName);
        }

        public async Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>> include)
        {
            if (include is null)
                throw new ArgumentNullException();

            return await dbSetExtension.IncludeAsyncQuery(include, tableName); 
        }

        public async Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>>[] includes)
        {
            if (includes is null)
                throw new ArgumentNullException();

            return await dbSetExtension.IncludeAsyncQuery(includes, tableName);
        }

        public async Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException();

            return await dbSetExtension.WhereAsyncQuery(predicate, tableName);
        }

        public async Task<T> GetByIdAsync(TKey id)
        {
            if (id is null)
                throw new ArgumentNullException();

            return await dbSetExtension.GetByIdAsyncQuery(tableName, id);
        }
        public async Task InsertAsync(object obj)
        {
            if (obj is null)
                throw new ArgumentNullException();

            await dbSetExtension.InsertAsyncQuery(obj, tableName);
        }
        public async Task InsertCascadeAsync(T obj)
        {
            if (obj is null) 
                throw new ArgumentNullException();

            await dbSetExtension.InsertCascadeAsyncQuery(obj, tableName);
        }
        public async Task UpdateAsync(T obj)
        {
            if (obj is null)
                throw new ArgumentNullException();

            await dbSetExtension.UpdateAsyncQuery(obj, tableName);
        }
        public async Task UpdateCascadeAsync(T obj)
        {
            if (obj is null)
                throw new ArgumentNullException();

            await dbSetExtension.UpdateCascadeAsyncQuery(obj, tableName);
        }
        public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException();

            await dbSetExtension.DeleteAsyncQuery(predicate, tableName);
        }
        public async Task DeleteByIdAsync(TKey id)
        {
            if (id is null)
                throw new ArgumentNullException();

            await dbSetExtension.DeleteByIdAsyncQuery(id, tableName);
        }
        public async Task DeleteCascadeAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException();

            await dbSetExtension.DeleteCascadeAsyncQuery(predicate, tableName);
        }
        public async Task<object> FromSqlRawAsync(string sqlQuery)
        {
            if (sqlQuery is null)
                throw new ArgumentNullException();

            return await dbSetExtension.FromSqlRawAsyncQuery(sqlQuery);
        }
    }
}
