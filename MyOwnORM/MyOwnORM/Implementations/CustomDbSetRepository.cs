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
        private string query;
        private readonly CustomDbSetService<T> dbSetExtension;
        private readonly CustomDbSetReflection<T> dbSetReflection;
        private readonly CustomDbSetReflectionHelper<T> reflectionHelper;
        public CustomDbSetRepository(string connectionString)
        {
            _connectionString = connectionString;
            dbSetExtension = new CustomDbSetService<T>(_connectionString);
            dbSetReflection = new CustomDbSetReflection<T>(_connectionString);
            reflectionHelper = new CustomDbSetReflectionHelper<T>();
            tableName = dbSetReflection.GetTableName();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            List<T> entities = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                query = $"SELECT * FROM {tableName}";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
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
                connection.Open();

                string tableName = typeof(T).Name;
                string sql = $"SELECT * FROM {tableName}";
                SqlCommand command = new SqlCommand(sql, connection);
                string idValue = string.Empty;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
                            {
                                idValue = value.ToString();
                            }

                            if (property.PropertyType == typeof(Guid))
                            {
                                property.SetValue(entity, new Guid(idValue));
                            }
                            else
                            {
                                property.SetValue(entity, value);
                            }
                            
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
                connection.Open();

                string tableName = typeof(T).Name;
                string sql = $"SELECT * FROM {tableName}";
                SqlCommand command = new SqlCommand(sql, connection);
                string idValue = string.Empty;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
                connection.Open();

                string key = dbSetExtension.GetKeyInLambdaExpression(predicate);
                bool isGuid = dbSetReflection.IsPropertyGuid(key);
                object val = new object();
                if (isGuid)
                {
                    val =  $"'{dbSetExtension.ExtractGuidStringFromExpression(predicate)}'";
                }
                else
                {
                    val = dbSetExtension.GetValueInLambdaExpression(predicate);
                }

                key = dbSetReflection.GetRealColumnName(key);

                query = $"SELECT * FROM {tableName} WHERE {key} = {val}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
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

        public async Task<T> GetByIdAsync(TKey id)
        {
            T entity = Activator.CreateInstance<T>();
            string idProperty = dbSetReflection.GetIdProperty(entity);

            if (string.IsNullOrEmpty(idProperty))
                throw new NullReferenceException(nameof(idProperty));

            string pkChoose = id.GetType() == typeof(Guid) ? $"'{id}'" : $"{id}";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                query = $"SELECT * FROM {tableName} WHERE {idProperty} = {pkChoose}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
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

        public async Task InsertAsync(object obj)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (obj.GetType() == typeof(T))
                {
                    string values = reflectionHelper.MapEntityPropertyValuesInString((T)obj);

                    query = $"INSERT INTO {tableName} VALUES ({values})";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                } 
                else if (dbSetReflection.IsClassInheritsAnotherClass(obj))
                {
                    string[] values = reflectionHelper.MapEntityPropertyValuesInString(obj);
                    query = $"INSERT INTO {tableName} VALUES ({values[0]})";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string querySecond = $"INSERT INTO {dbSetReflection.GetTableName(obj)} VALUES ({values[1]})";
                    using (SqlCommand commandSecond = new SqlCommand(querySecond, connection))
                    {
                        await commandSecond.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        public async Task InsertCascadeAsync(T obj)
        {
            string[] names = dbSetReflection.GetNamesOfCollectionOrModel();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                bool hasRowInDataBase = false;
                connection.Open();

                string values = reflectionHelper.MapEntityPropertyValuesInString(obj, names);
                string idNameEntity = dbSetReflection.GetIdProperty(obj);
                object idEntity = dbSetReflection.GetIdPropertyValue(obj);

                string checkQuery = $"SELECT * FROM {tableName} WHERE {idNameEntity} = {idEntity}";

                using (SqlCommand commandCheck = new SqlCommand(checkQuery, connection))
                {
                    using (SqlDataReader reader = commandCheck.ExecuteReader())
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
                    string query = $"INSERT INTO {tableName} VALUES ({values})";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }

                string[] queries = dbSetReflection.InsertCascadeModelsOrCollection(obj, names);

                for (int i = 0; i < queries.Length; i++)
                {
                    SqlCommand commandCascade = new SqlCommand(queries[i], connection);
                    commandCascade.ExecuteNonQuery();
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
                connection.Open();

                if (obj.GetType() == typeof(T))
                {
                    string updateStr = reflectionHelper.MapEntityPropertyValuesInUpdateString(idProperty, obj);
                    object idPropertyValue = dbSetReflection.GetIdPropertyValue(obj);

                    query = $"UPDATE {tableName} SET {updateStr} WHERE {idProperty}={idPropertyValue}";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }
                else if (dbSetReflection.IsClassInheritsAnotherClass(obj))
                {
                    string[] updateStrs = reflectionHelper.MapEntityPropertyValuesInUpdateStringTPT(idProperty, obj);
                    object idPropertyValue = dbSetReflection.GetIdPropertyValue(obj);

                    query = $"UPDATE {tableName} SET {updateStrs[0]} WHERE {idProperty}={idPropertyValue}";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();

                    string querySecond = $"UPDATE {dbSetReflection.GetTableName(obj)} SET {updateStrs[1]} WHERE {idProperty}={idPropertyValue}";
                    SqlCommand commandSecond = new SqlCommand(querySecond, connection);
                    commandSecond.ExecuteNonQuery();
                }
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
                connection.Open();

                string updateStr = reflectionHelper.MapEntityPropertyValuesInUpdateString(idProperty, obj);
                object idPropertyValue = dbSetReflection.GetIdPropertyValue(obj);

                string query = $"UPDATE {tableName} SET {updateStr} WHERE {idProperty}={idPropertyValue}";

                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();

                string[] queries = dbSetReflection.UpdateCascadeModelsOrCollection(obj, names, idProperty, idPropertyValue.ToString());

                for (int i = 0; i < queries.Length; i++)
                {
                    SqlCommand commandCascade = new SqlCommand(queries[i], connection);
                    commandCascade.ExecuteNonQuery();
                }
            }
        }
        public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = dbSetExtension.GetKeyInLambdaExpression(predicate);
                bool isGuid = dbSetReflection.IsPropertyGuid(key);
                object val = new object();
                if (isGuid)
                {
                    val = $"'{dbSetExtension.ExtractGuidStringFromExpression(predicate)}'";
                }
                else
                {
                    val = dbSetExtension.GetValueInLambdaExpression(predicate);
                }

                key = dbSetReflection.GetRealColumnName(key);

                query = $"DELETE FROM {tableName} WHERE {key}={val}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task DeleteByIdAsync(TKey id)
        {
            T entity = Activator.CreateInstance<T>();
            string idProperty = dbSetReflection.GetIdProperty(entity);
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string pkChoose = id.GetType() == typeof(Guid) ? $"'{id}'" : $"{id}";

                query = $"DELETE FROM {tableName} WHERE {idProperty}={pkChoose}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task DeleteCascadeAsync(Expression<Func<T, bool>> predicate)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = dbSetExtension.GetKeyInLambdaExpression(predicate);
                bool isGuid = dbSetReflection.IsPropertyGuid(key);
                object val = new object();
                if (isGuid)
                {
                    val = $"'{dbSetExtension.ExtractGuidStringFromExpression(predicate)}'";
                }
                else
                {
                    val = dbSetExtension.GetValueInLambdaExpression(predicate);
                }

                key = dbSetReflection.GetRealColumnName(key);
                List<string> tables = await dbSetExtension.FindTablesNameWithForeignKeysForCascadeDelete(typeof(T).Name);
                List<string> fks = await dbSetExtension.FindForeignKeysNameInTablesForCascadeDelete(typeof(T).Name);

                for (int i = 0; i < tables.Count; i++)
                {
                    string cascadeDeleteQuery = $"DELETE FROM {tables[i]} WHERE {fks[i]} = {val.ToString().ToUpper()}";
                    SqlCommand commandCascadeDelete = new SqlCommand(cascadeDeleteQuery, connection);
                    commandCascadeDelete.ExecuteNonQuery();
                }

                string query = $"DELETE FROM {tableName} WHERE {key}={val}";

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task<object> FromSqlRawAsync(string sql)
        {
            string keyWord = sql.Split()[0] + sql.Split()[1];
            List<T> entities = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (keyWord.ToLower() == "select*")
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
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
            //string fkChoose = dbSetExtension.GetForeignKeyNameForGenericType();
            string fkChoose = "PostionId";

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                Type tableName = dbSetReflection.GetTypeCollectionArguments(property);

                query = $"SELECT * FROM {tableName.Name} WHERE {fkChoose} = '{idValue}'";

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                { 
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
