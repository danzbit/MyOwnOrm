using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MyOwnORM.Helper;
using MyOwnORM.Reflection;

namespace MyOwnORM
{
    public class CustomDbSetService<T, TKey> where T : class
    {
        private readonly string _connectionString;
        private readonly CustomDbSetReflection<T> dbSetReflection;
        private readonly CustomDbSetReflectionHelper<T> reflectionHelper;

        public CustomDbSetService(string connectionString)
        {
            _connectionString = connectionString;
            dbSetReflection = new CustomDbSetReflection<T>();
            reflectionHelper = new CustomDbSetReflectionHelper<T>();
        }
        public async Task<List<string>> FindTablesNameWithForeignKeysForCascadeDelete(string tableName)
        {
            List<string> res = new List<string>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT " +
                    $"TP.name AS TableName " +
                    $"FROM " +
                    $"sys.foreign_keys AS FK " +
                    $"INNER JOIN " +
                    $"sys.tables AS TP ON FK.parent_object_id = TP.object_id " +
                    $"INNER JOIN " +
                    $"sys.tables AS RF ON FK.referenced_object_id = RF.object_id " +
                    $"WHERE " +
                    $"RF.name = '{tableName}';";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            object table = reader["TableName"];
                            res.Add(table.ToString());
                        }
                    }
                }
            }

            return res;
        }

        public async Task<List<string>> FindForeignKeysNameInTablesForCascadeDelete(string tableName)
        {
            List<string> res = new List<string>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT " +
                    $"COL_NAME(FK.parent_object_id, FKC.parent_column_id) AS ForeignKeyColumnName " +
                    $"FROM sys.foreign_keys AS FK " +
                    $"INNER JOIN sys.tables AS TP ON FK.parent_object_id = TP.object_id " +
                    $"INNER JOIN sys.tables AS RF ON FK.referenced_object_id = RF.object_id " +
                    $"INNER JOIN sys.foreign_key_columns AS FKC ON FK.object_id = FKC.constraint_object_id " +
                    $"WHERE RF.name = '{tableName}';";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            object table = reader["ForeignKeyColumnName"];
                            res.Add(table.ToString());
                        }
                    }
                }
            }

            return res;
        }

        public string GetKeyInLambdaExpression(Expression<Func<T, bool>> propertyLambda)
        {
            var binaryExpression = propertyLambda.Body as BinaryExpression;
            var memberExpression = binaryExpression.Left as MemberExpression;
            var propertyInfo = memberExpression.Member as PropertyInfo;

            return dbSetReflection.GetColumnName(propertyInfo);
        }
        public object GetValueInLambdaExpression(Expression<Func<T, bool>> propertyLambda)
        {
            var binaryExpression = propertyLambda.Body as BinaryExpression;
            var memberExpression = binaryExpression.Right.ToString();
            if (memberExpression.StartsWith("\"") && memberExpression.EndsWith("\""))
            {
                return memberExpression.Substring(1, memberExpression.Length - 2);
            }
            return memberExpression;
        }
        public string ExtractGuidStringFromExpression(Expression<Func<T, bool>> predicate)
        {
            if (predicate.Body is BinaryExpression binaryExpression)
            {
                if (binaryExpression.Left is MemberExpression memberExpression &&
                    memberExpression.Member is PropertyInfo propertyInfo &&
                    propertyInfo.PropertyType == typeof(Guid))
                {
                    object constantValue = null;

                    if (binaryExpression.Right is ConstantExpression constantExpression)
                    {
                        constantValue = constantExpression.Value;
                    }
                    else if (binaryExpression.Right is MemberExpression closureMemberExpression)
                    {
                        if (closureMemberExpression.Member is FieldInfo fieldInfo)
                        {
                            object closureObject = ((ConstantExpression)closureMemberExpression.Expression).Value;
                            constantValue = fieldInfo.GetValue(closureObject);
                        }
                    }

                    if (constantValue is Guid guidValue)
                    {
                        return guidValue.ToString();
                    }
                }
            }
            return string.Empty;
        }
        public string GetPropertyValue(Expression<Func<T, object>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operand)
            {
                return operand.Member.Name;
            }
            return string.Empty;
        }

        public string GenerateInsertSqlQuery(string str, string tableName)
        {
            return $"INSERT INTO {tableName} VALUES ({str})"; 
        } 
        public string[] GenerateInsertSqlQueries(string[] strs, string tableName)
        {
            List<string> res = new List<string>();
            foreach (var str in strs)
                res.Add($"INSERT INTO {tableName} VALUES ({str})");

            return res.ToArray();
        }
        public string GenerateUpdateSqlQuery(string str, string tableName, string idProperty, string idPropertyValue)
        {
            return $"UPDATE {tableName} SET {str} WHERE {idProperty}={idPropertyValue}";
        }
        public string[] GenerateUpdateSqlQueries(string[] strs, string tableName, string idProperty, string idPropertyValue)
        {
            List<string> res = new List<string>();
            foreach (var str in strs)
                res.Add($"UPDATE {tableName} SET {str} WHERE {idProperty}={idPropertyValue}");

            return res.ToArray();
        }
        public string GetForeignKeyNameForGenericType()
        {
            T type = dbSetReflection.CreateInstanceType();
            string fkAttribute = dbSetReflection.GetForeignKeyAttribute(type.GetType());
            string fk = $"{typeof(T).Name}Id";
            return string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;
        }

        public string GetForeignKeyNameForCustomClass(Type includeType)
        {
            string fkAttribute = dbSetReflection.GetForeignKeyAttribute(includeType);
            string fk = $"{typeof(T).Name}Id";
            return string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;
        }
        public string GetForeignKeyNameForIncludeExpressionsMethod(Type includeType, T entity)
        {
            string fkAttribute = dbSetReflection.GetForeignKeyAttribute(includeType);
            string fk = $"{entity}Id";
            return string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;
        }
        public string GetSelectQuery(string tableName)
        {
            return $"SELECT * FROM {tableName}";
        }
        public string GetSelectFilterQuery(string tableName, string key, string val)
        {
            return $"SELECT * FROM {tableName} WHERE {key} = {val}";
        }
        public string GetInsertIntoQuery(string tableName, string values)
        {
            return $"INSERT INTO {tableName} VALUES ({values})";
        }
        public string GetUpdateQuery(string tableName, string updateStr, string key, string val)
        {
            return $"UPDATE {tableName} SET {updateStr} WHERE {key}={val}";
        }
        public string GetDeleteQuery(string tableName, string key, string val)
        {
            return $"DELETE FROM {tableName} WHERE {key}={val}";
        }
        public async Task GetAllAsyncQuery(string tableName, List<T> entities)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = GetSelectQuery(tableName);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T entity = dbSetReflection.MapReaderToEntity(reader);
                            entities.Add(entity);
                        }
                    }
                }
            }
        }
        public async Task IncludeAsyncQuery(Expression<Func<T, object>> include, string tableName, List<T> entities)
        {
            string propVal = GetPropertyValue(include);
            object includeType = dbSetReflection.GetIncludeType(include);

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string sql = GetSelectQuery(tableName);
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
        }

        public async Task IncludeAsyncQuery(Expression<Func<T, object>>[] includes, string tableName, List<T> entities)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string sql = GetSelectQuery(tableName);
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
        }

        public async Task WhereAsyncQuery(Expression<Func<T, bool>> predicate, string tableName, List<T> entities)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = GetKeyInLambdaExpression(predicate);
                bool isGuid = dbSetReflection.IsPropertyGuid(key);
                object val = new object();
                if (isGuid)
                {
                    val = $"'{ExtractGuidStringFromExpression(predicate)}'";
                }
                else
                {
                    val = GetValueInLambdaExpression(predicate);
                }

                key = dbSetReflection.GetRealColumnName(key);

                string query = GetSelectFilterQuery(tableName, key, val.ToString());
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T entity = dbSetReflection.MapReaderToEntity(reader);
                            entities.Add(entity);
                        }
                    }
                }
            }
        }

        public async Task GetByIdAsyncQuery(string tableName, TKey id, T entity)
        {
            string idProperty = dbSetReflection.GetIdProperty(entity);

            string pkChoose = id.GetType() == typeof(Guid) ? $"'{id}'" : $"{id}";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = GetSelectFilterQuery(tableName, idProperty, pkChoose);
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
        }

        public async Task InsertAsyncQuery(object obj, string tableName)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (obj.GetType() == typeof(T))
                {
                    string values = reflectionHelper.MapEntityPropertyValuesInString((T)obj);

                    string query = GetInsertIntoQuery(tableName, values);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                else if (dbSetReflection.IsClassInheritsAnotherClass(obj))
                {
                    string[] values = reflectionHelper.MapEntityPropertyValuesInString(obj);
                    string query = GetInsertIntoQuery(tableName, values[0]);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string querySecond = GetInsertIntoQuery(dbSetReflection.GetTableName(obj), values[1]);
                    using (SqlCommand commandSecond = new SqlCommand(querySecond, connection))
                    {
                        await commandSecond.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task InsertCascadeAsyncQuery(T obj, string tableName)
        {
            string[] names = dbSetReflection.GetNamesOfCollectionOrModel();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                bool hasRowInDataBase = false;
                connection.Open();

                string values = reflectionHelper.MapEntityPropertyValuesInString(obj, names);
                string idNameEntity = dbSetReflection.GetIdProperty(obj);
                object idEntity = dbSetReflection.GetIdPropertyValue(obj);

                string checkQuery = GetSelectFilterQuery(tableName, idNameEntity, idEntity.ToString());

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
                    string query = GetInsertIntoQuery(tableName, values);

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

        public async Task UpdateAsyncQuery(T obj, string tableName)
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

                    string query = GetUpdateQuery(tableName, updateStr, idProperty, idPropertyValue.ToString());

                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }
                else if (dbSetReflection.IsClassInheritsAnotherClass(obj))
                {
                    string[] updateStrs = reflectionHelper.MapEntityPropertyValuesInUpdateStringTPT(idProperty, obj);
                    object idPropertyValue = dbSetReflection.GetIdPropertyValue(obj);

                    string query = GetUpdateQuery(tableName, updateStrs[0], idProperty, idPropertyValue.ToString());

                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();

                    string querySecond = GetUpdateQuery(dbSetReflection.GetTableName(obj), updateStrs[1], idProperty, idPropertyValue.ToString());
                    SqlCommand commandSecond = new SqlCommand(querySecond, connection);
                    commandSecond.ExecuteNonQuery();
                }
            }
        }
        public async Task UpdateCascadeAsyncQuery(T obj, string tableName)
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

                string query = GetUpdateQuery(tableName, updateStr, idProperty, idPropertyValue.ToString());

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
        public async Task DeleteAsyncQuery(Expression<Func<T, bool>> predicate, string tableName)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = GetKeyInLambdaExpression(predicate);
                bool isGuid = dbSetReflection.IsPropertyGuid(key);
                object val = new object();
                if (isGuid)
                {
                    val = $"'{ExtractGuidStringFromExpression(predicate)}'";
                }
                else
                {
                    val = GetValueInLambdaExpression(predicate);
                }

                key = dbSetReflection.GetRealColumnName(key);

                string query = GetDeleteQuery(tableName, key, val.ToString());

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task DeleteByIdAsyncQuery(TKey id, string tableName)
        {
            T entity = Activator.CreateInstance<T>();
            string idProperty = dbSetReflection.GetIdProperty(entity);
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string pkChoose = id.GetType() == typeof(Guid) ? $"'{id}'" : $"{id}";

                string query = GetDeleteQuery(tableName, idProperty, pkChoose);

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task DeleteCascadeAsyncQuery(Expression<Func<T, bool>> predicate, string tableName)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = GetKeyInLambdaExpression(predicate);
                bool isGuid = dbSetReflection.IsPropertyGuid(key);
                object val = new object();
                if (isGuid)
                {
                    val = $"'{ExtractGuidStringFromExpression(predicate)}'";
                }
                else
                {
                    val = GetValueInLambdaExpression(predicate);
                }

                key = dbSetReflection.GetRealColumnName(key);
                List<string> tables = await FindTablesNameWithForeignKeysForCascadeDelete(typeof(T).Name);
                List<string> fks = await FindForeignKeysNameInTablesForCascadeDelete(typeof(T).Name);

                for (int i = 0; i < tables.Count; i++)
                {
                    string cascadeDeleteQuery = GetDeleteQuery(tables[i], fks[i], val.ToString().ToUpper());
                    SqlCommand commandCascadeDelete = new SqlCommand(cascadeDeleteQuery, connection);
                    commandCascadeDelete.ExecuteNonQuery();
                }

                string query = GetDeleteQuery(tableName, key, val.ToString());

                SqlCommand command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<object> FromSqlRawAsyncQuery(string sql)
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
            string fkChoose = GetForeignKeyNameForGenericType();

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                Type tableName = dbSetReflection.GetTypeCollectionArguments(property);

                string query = GetSelectFilterQuery(tableName.Name, fkChoose, $"'{idValue}'");

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
            string fkChoose = GetForeignKeyNameForCustomClass(includeType);
            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                string query = GetSelectFilterQuery(includeType.Name, fkChoose, $"'{idValue}'");

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {

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

            string fkChoose = GetForeignKeyNameForIncludeExpressionsMethod(includeType, entity);

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                string query = GetSelectFilterQuery(propertyInfo.Name, fkChoose, $"'{idValue}'");

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {

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
            string fkChoose = GetForeignKeyNameForIncludeExpressionsMethod(includeType, entity);

            using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
            {
                connectionSecond.Open();

                string query = GetSelectFilterQuery(propertyInfo.Name, fkChoose, $"'{idValue}'");

                using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                {

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
