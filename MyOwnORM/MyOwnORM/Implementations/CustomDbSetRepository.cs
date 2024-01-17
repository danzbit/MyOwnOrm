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
    public class CustomDbSetRepository<T> : ICustomDbSetRepository<T> where T : class
    {
        private string _connectionString;
        private string tableName;
        private string query;
        private readonly CustomDbSetService<T> dbSetExtension;
        private readonly CustomDbSetReflection<T> dbSetReflection;
        private readonly CustomDbSetReflectionHelper<T> reflectionHelper;
        public CustomDbSetRepository(string connectionString)
        {
            _connectionString = connectionString;
            tableName = typeof(T).Name;
            dbSetExtension = new CustomDbSetService<T>(_connectionString);
            dbSetReflection = new CustomDbSetReflection<T>();
            reflectionHelper = new CustomDbSetReflectionHelper<T>();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            List<T> entities = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                query = $"SELECT * FROM {tableName}";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            T entity = dbSetReflection.MapReaderToEntity(reader);
                            entities.Add(entity);
                        }
                    }
                }
            }

            return entities.AsQueryable();
        }

        public async Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>> include)
        {
            List<T> entities = new List<T>();

            string propVal = dbSetExtension.GetPropertyValue(include);
            object includeType = dbSetReflection.GetIncludeType(include);

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string tableName = typeof(T).Name;
                string sql = $"SELECT * FROM {tableName}";
                SqlCommand command = new SqlCommand(sql, connection);
                string idValue = string.Empty;

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        T entity = dbSetReflection.CreateInstanceType();

                        foreach (var property in typeof(T).GetProperties())
                        {
                            string propertyName = property.Name;

                            int ordinal;
                            try
                            {
                                ordinal = reader.GetOrdinal(propertyName);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                FindSetMethod(property, idValue, entity, propVal, includeType.GetType());
                                continue;
                            }

                            object value = reader.GetValue(ordinal);

                            if (propertyName == "Id")
                                idValue = value.ToString();

                            property.SetValue(entity, value);
                        }
                        entities.Add(entity);
                    }
                }
            }
            return entities;
        }

        public async Task<IEnumerable<T>> IncludeAsync(Expression<Func<T, object>>[] includes)
        {
            List<T> entities = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string tableName = typeof(T).Name;
                string sql = $"SELECT * FROM {tableName}";
                SqlCommand command = new SqlCommand(sql, connection);
                string idValue = string.Empty;

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        T entity = dbSetReflection.CreateInstanceType();

                        dbSetReflection.SetPropertiesForIncludeExpressionsMethod(reader, idValue, entity);

                        foreach (var include in includes)
                        {
                            List<string> propVal = dbSetReflection.GetPropertyValues(new Expression<Func<T, object>>[] { include });
                            object[] includeType = dbSetReflection.GetIncludeTypes(new Expression<Func<T, object>>[] { include });

                            for (int i = 0; i < propVal.Count; i++)
                            {
                                PropertyInfo propertyInfo = dbSetReflection.GetPropertyInfo(propVal[i]);
                                Type propertyType = propertyInfo.PropertyType;

                                if (dbSetReflection.IsCollectionType(propertyInfo))
                                {
                                    SetGenericEntityIncludeExpressionsMethod(propertyInfo, includeType[i].GetType(), entity, idValue, reader);
                                }
                                else
                                {
                                    SetEntityIncludeExpressionsMethod(includeType[i].GetType(), entity, propertyInfo, idValue, reader, propertyType);
                                }
                            }
                        }

                        entities.Add(entity);
                    }
                }
            }
            return entities;
        }

        public async Task<IQueryable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            List<T> entities = new List<T>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string key = dbSetExtension.GetKeyInLambdaExpression(predicate);
                object val = dbSetExtension.GetValueInLambdaExpression(predicate);

                query = $"SELECT * FROM {tableName} WHERE {key} = {val}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            T entity = dbSetReflection.MapReaderToEntity(reader);
                            entities.Add(entity);
                        }
                    }
                }
            }

            return entities.AsQueryable();
        }

        public async Task<T> GetByIdAsync(object id)
        {
            T entity = Activator.CreateInstance<T>();
            string idProperty = dbSetReflection.GetIdProperty(entity);

            if (string.IsNullOrEmpty(idProperty))
                throw new NullReferenceException(nameof(idProperty));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                query = $"SELECT * FROM {tableName} WHERE {idProperty} = {id}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            T type = dbSetReflection.MapReaderToEntity(reader);
                            entity = type;
                        }
                    }
                }
            }
            return entity;
        }

        public async Task InsertAsync(T obj)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string values = reflectionHelper.MapEntityPropertyValuesInString(obj);

                query = $"INSERT INTO {tableName} VALUE ({values})";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task InsertCascadeAsync(T obj)
        {
            string[] names = dbSetReflection.GetNamesOfCollectionOrModel();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                bool hasRowInDataBase = false;
                await connection.OpenAsync();

                string values = reflectionHelper.MapEntityPropertyValuesInString(obj, names);
                string idNameEntity = dbSetReflection.GetIdProperty(obj);
                string idEntity = dbSetReflection.GetIdPropertyValue(obj);

                string checkQuery = $"SELECT * FROM {typeof(T).Name} WHERE {idNameEntity} = {idEntity}";

                using (SqlCommand commandCheck = new SqlCommand(checkQuery, connection))
                {
                    using (SqlDataReader reader = await commandCheck.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            object id = reader[$"{idNameEntity}"];
                            if (id == null)
                            {
                                hasRowInDataBase = true;
                                break;
                            }
                        }
                    }
                }

                if (!hasRowInDataBase)
                {
                    string query = $"INSERT INTO {typeof(T).Name} VALUES ({values})";

                    SqlCommand command = new SqlCommand(query, connection);
                    await command.ExecuteNonQueryAsync();
                }

                string[] queries = dbSetReflection.InsertCascadeModelsOrCollection(obj, names);

                for (int i = 0; i < queries.Length; i++)
                {
                    SqlCommand commandCascade = new SqlCommand(queries[i], connection);
                    await commandCascade.ExecuteNonQueryAsync();
                }

            }
        }
        public async Task UpdateAsync(T obj)
        {
            string idProperty = dbSetReflection.GetIdProperty(obj);

            if (string.IsNullOrEmpty(idProperty))
                throw new NullReferenceException(nameof(idProperty));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string updateStr = reflectionHelper.MapEntityPropertyValuesInUpdateString(obj);
                string idPropertyValue = dbSetReflection.GetIdPropertyValue(obj);

                query = $"UPDATE {tableName} SET {updateStr} WHERE {idProperty}={idPropertyValue}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task UpdateCascadeAsync(T obj)
        {
            string idProperty = dbSetReflection.GetIdProperty(obj);
            string[] names = dbSetReflection.GetNamesOfCollectionOrModel();

            if (string.IsNullOrEmpty(idProperty))
                throw new NullReferenceException(nameof(idProperty));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string updateStr = reflectionHelper.MapEntityPropertyValuesInUpdateString(obj, idProperty);
                string idPropertyValue = dbSetReflection.GetIdPropertyValue(obj);

                string query = $"UPDATE {typeof(T).Name} SET {updateStr} WHERE {idProperty}={idPropertyValue}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();

                string[] queries = dbSetReflection.UpdateCascadeModelsOrCollection(obj, names, idProperty, idPropertyValue);

                for (int i = 0; i < queries.Length; i++)
                {
                    SqlCommand commandCascade = new SqlCommand(queries[i], connection);
                    await commandCascade.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string key = dbSetExtension.GetKeyInLambdaExpression(predicate);
                object val = dbSetExtension.GetValueInLambdaExpression(predicate);

                query = $"DELETE FROM {tableName} WHERE {key}={val}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task DeleteByIdAsync(object id)
        {
            T entity = Activator.CreateInstance<T>();
            string idProperty = dbSetReflection.GetIdProperty(entity);
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                query = $"DELETE FROM {tableName} WHERE {idProperty}={id}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task DeleteCascadeAsync(Expression<Func<T, bool>> predicate)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string key = dbSetExtension.GetKeyInLambdaExpression(predicate);
                object val = dbSetExtension.GetValueInLambdaExpression(predicate);
                List<string> tables = await dbSetExtension.FindTablesNameWithForeignKeysForCascadeDelete(typeof(T).Name);
                List<string> fks = await dbSetExtension.FindForeignKeysNameInTablesForCascadeDelete(typeof(T).Name);

                for (int i = 0; i < tables.Count; i++)
                {
                    string cascadeDeleteQuery = $"DELETE FROM {tables[i]} WHERE {fks[i]} = {val}";
                    SqlCommand commandCascadeDelete = new SqlCommand(cascadeDeleteQuery, connection);
                    await commandCascadeDelete.ExecuteNonQueryAsync();
                }

                string query = $"DELETE FROM {typeof(T).Name} WHERE {key}={val}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task<dynamic> FromSqlRawAsync(string sql)
        {
            string keyWord = sql.Split()[0] + sql.Split()[1];
            List<T> entities = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (keyWord.ToLower() == "select*")
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                T entity = dbSetReflection.MapReaderToEntity(reader);
                                entities.Add(entity);
                            }
                        }

                        return entities;
                    }
                    else if (keyWord.ToLower() == "update" || keyWord.ToLower() == "insert" || keyWord.ToLower() == "delete")
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        await command.ExecuteScalarAsync();
                    }
                }
            }
            return null;
        }
        private void FindSetMethod(PropertyInfo property, string idValue, T entity, string propVal, Type includeType)
        {
            if (property.PropertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) && property.Name == propVal)
            {
                SetGenericEntity(property, idValue, entity);
            }
            else if (includeType.GetType().IsAssignableFrom(property.PropertyType) && propVal == includeType.GetType().Name && property.Name == includeType.GetType().Name)
            {
                SetClassEntity(includeType, idValue, property, entity);
            }
        }
        private void SetGenericEntity(PropertyInfo property, string idValue, T entity)
        {
            Type elementType = dbSetReflection.GetTypeCollectionArguments(property);
            IList entitiesSecond = dbSetReflection.GetIListType(property);
            string fkChoose = dbSetExtension.GetForeignKeyNameForGenericType();

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                query = $"SELECT * FROM {property.Name} WHERE {fkChoose} = @Id";

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {
                    commandSecond.Parameters.AddWithValue("@Id", idValue);

                    using (SqlDataReader readerSecond = commandSecond.ExecuteReader())
                    {
                        while (readerSecond.Read())
                        {
                            object entitySecond = dbSetReflection.MapToEntityIncludeMethodGenericType(readerSecond, elementType);
                            entitiesSecond.Add(entitySecond);
                        }
                        property.SetValue(entity, entitiesSecond);
                    }
                }
            }
        }
        private void SetClassEntity(Type includeType, string idValue, PropertyInfo property, T entity)
        {
            string fkChoose = dbSetExtension.GetForeignKeyNameForCustomClass(includeType);
            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                string query = $"SELECT * FROM {includeType.Name} WHERE {fkChoose} = @Id";

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {
                    commandSecond.Parameters.AddWithValue("@Id", idValue);

                    using (SqlDataReader readerSecond = commandSecond.ExecuteReader())
                    {
                        while (readerSecond.Read())
                        {
                            object entitySecond = dbSetReflection.MapToEntityIncludeMethodCustomClass(includeType, readerSecond);
                            property.SetValue(entity, entitySecond);
                        }
                    }
                }
            }
        }

        private void SetGenericEntityIncludeExpressionsMethod(PropertyInfo propertyInfo, Type includeType, T entity, string idValue, SqlDataReader reader)
        {
            Type elementType = dbSetReflection.GetTypeCollectionArguments(propertyInfo);
            IList entitiesSecond = dbSetReflection.GetIListType(propertyInfo);

            string fkChoose = dbSetExtension.GetForeignKeyNameForIncludeExpressionsMethod(includeType, entity);

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                string query = $"SELECT * FROM {propertyInfo.Name} WHERE {fkChoose} = @Id";

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {
                    commandSecond.Parameters.AddWithValue("@Id", idValue);

                    using (SqlDataReader readerSecond = commandSecond.ExecuteReader())
                    {
                        while (readerSecond.Read())
                        {
                            object entitySecond = dbSetReflection.MapToGenericEntityForIncludeExpressionsMethod(reader, elementType);

                            entitiesSecond.Add(entitySecond);
                        }
                    }
                }
            }

            propertyInfo.SetValue(entity, entitiesSecond);
        }

        private void SetEntityIncludeExpressionsMethod(Type includeType, T entity, PropertyInfo propertyInfo, string idValue, SqlDataReader reader, Type propertyType)
        {
            string fkChoose = dbSetExtension.GetForeignKeyNameForIncludeExpressionsMethod(includeType, entity);

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                string query = $"SELECT * FROM {propertyInfo.Name} WHERE {fkChoose} = @Id";

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {
                    commandSecond.Parameters.AddWithValue("@Id", idValue);

                    using (SqlDataReader readerSecond = commandSecond.ExecuteReader())
                    {
                        while (readerSecond.Read())
                        {
                            object entitySecond = dbSetReflection.MapToEntityForIncludeExpressionsMethod(propertyType, reader);

                            propertyInfo.SetValue(entity, entitySecond);
                        }
                    }
                }
            }
        }
    }
}
