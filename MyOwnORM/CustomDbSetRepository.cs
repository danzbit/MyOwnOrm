using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public class CustomDbSetRepository<T> : CustomDbSetReflection<T> where T : class
    {
        private string _connectionString;
        private string tableName;
        private string query;
        private CustomDbSetExtension dbSetExtension;
        public CustomDbSetRepository(string connectionString) 
        {
            _connectionString = connectionString;
            tableName = typeof(T).Name;
            dbSetExtension = new CustomDbSetExtension(_connectionString);
        }
        public IEnumerable<T> GetAll()
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
                        while (reader.Read())
                        {
                            T entity = MapReaderToEntity(reader);
                            entities.Add(entity);
                        }
                    }
                }
            }

            return entities.AsQueryable();
        }

        public IEnumerable<T> Include(Expression<Func<T, object>> include)
        {
            List<T> entities = new List<T>();

            string propVal = GetPropertyValue(include);
            dynamic includeType = GetIncludeType(include);

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
                        T entity = Activator.CreateInstance<T>();

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
                                if (property.PropertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) && property.Name == propVal)
                                {
                                    Type elementType = property.PropertyType.GetGenericArguments()[0];
                                    Type listType = typeof(List<>).MakeGenericType(elementType);
                                    IList entitiesSecond = (IList)Activator.CreateInstance(listType);
                                    T type = Activator.CreateInstance<T>();
                                    string fkAttribute = GetForeignKeyAttribute(type.GetType());
                                    string fk = $"{typeof(T).Name}Id";
                                    string fkChoose = string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;
                                    using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
                                    {
                                        connectionSecond.Open();

                                        string query = $"SELECT * FROM {property.Name} WHERE {fkChoose} = @Id";

                                        using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                                        {
                                            commandSecond.Parameters.AddWithValue("@Id", idValue);

                                            using (SqlDataReader readerSecond = commandSecond.ExecuteReader())
                                            {
                                                while (readerSecond.Read())
                                                {
                                                    object entitySecond = Activator.CreateInstance(elementType);

                                                    PropertyInfo[] properties = elementType.GetProperties();

                                                    foreach (PropertyInfo propertySecond in properties)
                                                    {
                                                        string propertyNameSecond = propertySecond.Name;


                                                        int ordinalSecond;

                                                        try
                                                        {
                                                            ordinalSecond = readerSecond.GetOrdinal(propertyNameSecond);
                                                        }
                                                        catch (IndexOutOfRangeException e)
                                                        {
                                                            continue;
                                                        }

                                                        object valueSecond = readerSecond.GetValue(ordinalSecond);

                                                        propertySecond.SetValue(entitySecond, valueSecond);
                                                    }
                                                    entitiesSecond.Add(entitySecond);
                                                }
                                                property.SetValue(entity, entitiesSecond);
                                            }
                                        }
                                    }
                                }
                                else if (includeType.GetType().IsAssignableFrom(property.PropertyType) && propVal == includeType.GetType().Name && property.Name == includeType.GetType().Name)
                                {
                                    string fkAttribute = GetForeignKeyAttribute(includeType);
                                    string fk = $"{typeof(T).Name}Id";
                                    string fkChoose = string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;
                                    using (SqlConnection connectionSecond = new SqlConnection(_connectionString))
                                    {
                                        connectionSecond.Open();

                                        string query = $"SELECT * FROM {includeType.GetType().Name} WHERE {fkChoose} = @Id";

                                        using (SqlCommand commandSecond = new SqlCommand(query, connectionSecond))
                                        {
                                            commandSecond.Parameters.AddWithValue("@Id", idValue);

                                            using (SqlDataReader readerSecond = commandSecond.ExecuteReader())
                                            {
                                                while (readerSecond.Read())
                                                {
                                                    dynamic entitySecond = Activator.CreateInstance(includeType.GetType());

                                                    PropertyInfo[] properties = includeType.GetType().GetProperties();

                                                    foreach (PropertyInfo propertySecond in properties)
                                                    {
                                                        string propertyNameSecond = propertySecond.Name;


                                                        int ordinalSecond;

                                                        try
                                                        {
                                                            ordinalSecond = readerSecond.GetOrdinal(propertyNameSecond);
                                                        }
                                                        catch (IndexOutOfRangeException e)
                                                        {
                                                            continue;
                                                        }

                                                        object valueSecond = readerSecond.GetValue(ordinalSecond);

                                                        propertySecond.SetValue(entitySecond, valueSecond);
                                                    }
                                                    property.SetValue(entity, entitySecond);
                                                }
                                            }
                                        }
                                    }
                                }
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



        public IEnumerable<T> Include(Expression<Func<T, object>>[] includes)
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
                        T entity = Activator.CreateInstance<T>();

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
                                continue;
                            }

                            object value = reader.GetValue(ordinal);

                            if (propertyName == "Id")
                                idValue = value.ToString();

                            property.SetValue(entity, value);
                        }

                        foreach (var include in includes)
                        {
                            List<string> propVal = GetPropertyValues(new Expression<Func<T, object>>[] { include });
                            dynamic[] includeType = GetIncludeTypes(new Expression<Func<T, object>>[] { include });

                            for (int i = 0; i < propVal.Count; i++)
                            {
                                PropertyInfo propertyInfo = typeof(T).GetProperty(propVal[i]);
                                Type propertyType = propertyInfo.PropertyType;

                                if (IsCollectionType(propertyInfo))
                                {
                                    Type elementType = propertyType.GetGenericArguments()[0];
                                    Type listType = typeof(List<>).MakeGenericType(elementType);
                                    IList entitiesSecond = (IList)Activator.CreateInstance(listType);

                                    string fkAttribute = GetForeignKeyAttribute(includeType[i]);
                                    string fk = $"{entity}Id";
                                    string fkChoose = string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;

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
                                                    object entitySecond = Activator.CreateInstance(elementType);

                                                    PropertyInfo[] properties = elementType.GetProperties();

                                                    foreach (PropertyInfo propertySecond in properties)
                                                    {
                                                        string propertyNameSecond = propertySecond.Name;

                                                        int ordinalSecond;

                                                        try
                                                        {
                                                            ordinalSecond = readerSecond.GetOrdinal(propertyNameSecond);
                                                        }
                                                        catch (IndexOutOfRangeException)
                                                        {
                                                            continue;
                                                        }

                                                        object valueSecond = readerSecond.GetValue(ordinalSecond);
                                                        propertySecond.SetValue(entitySecond, valueSecond);
                                                    }

                                                    entitiesSecond.Add(entitySecond);
                                                }
                                            }
                                        }
                                    }

                                    propertyInfo.SetValue(entity, entitiesSecond);
                                }
                                else
                                {
                                    string fkAttribute = GetForeignKeyAttribute(includeType[i]);
                                    string fk = $"{entity}Id";
                                    string fkChoose = string.IsNullOrEmpty(fkAttribute) ? fk : fkAttribute;

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
                                                    dynamic entitySecond = Activator.CreateInstance(propertyType);

                                                    PropertyInfo[] properties = propertyType.GetProperties();

                                                    foreach (PropertyInfo propertySecond in properties)
                                                    {
                                                        string propertyNameSecond = propertySecond.Name;

                                                        int ordinalSecond;

                                                        try
                                                        {
                                                            ordinalSecond = readerSecond.GetOrdinal(propertyNameSecond);
                                                        }
                                                        catch (IndexOutOfRangeException)
                                                        {
                                                            continue;
                                                        }

                                                        object valueSecond = readerSecond.GetValue(ordinalSecond);
                                                        propertySecond.SetValue(entitySecond, valueSecond);
                                                    }

                                                    propertyInfo.SetValue(entity, entitySecond);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        entities.Add(entity);
                    }
                }
            }

            return entities;
        }

        public IQueryable<T> Where(Expression<Func<T, bool>> predicate)
        {
            List<T> entities = new List<T>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = GetKeyInLambdaExpression(predicate);
                dynamic val = GetValueInLambdaExpression(predicate);

                query = $"SELECT * FROM {tableName} WHERE {key} = {val}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T entity = MapReaderToEntity(reader);
                            entities.Add(entity);
                        }
                    }
                }
            }

            return entities.AsQueryable();
        }

        public T GetById(dynamic id)
        {
            T entity = Activator.CreateInstance<T>();
            string idProperty = GetIdProperty(entity);

            if (string.IsNullOrEmpty(idProperty)) 
                throw new NullReferenceException(nameof(idProperty));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                query = $"SELECT * FROM {tableName} WHERE {idProperty} = {id}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            T type = MapReaderToEntity(reader);
                            entity = type;
                        }
                    }
                }
            }
            return entity;
        }

        public void Insert(T obj)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string values = MapEntityPropertyValuesInString(obj);

                query = $"INSERT INTO {tableName} VALUE ({values})";

                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
        public void InsertCascade(T obj)
        {
            string[] names = GetNamesOfCollectionOrModel();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                bool hasRowInDataBase = false;
                connection.Open();

                string values = MapEntityPropertyValuesInString(obj, names);
                string idNameEntity = GetIdProperty(obj);
                string idEntity = GetIdPropertyValue(obj);

                string checkQuery = $"SELECT * FROM {typeof(T).Name} WHERE {idNameEntity} = {idEntity}";

                using (SqlCommand commandCheck = new SqlCommand(checkQuery, connection))
                {
                    using (SqlDataReader reader = commandCheck.ExecuteReader())
                    {
                        while (reader.Read())
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
                    command.ExecuteNonQuery();
                }

                string[] queries = InsertCascadeModelsOrCollection(obj, names);

                for (int i = 0; i < queries.Length; i++)
                {
                    SqlCommand commandCascade = new SqlCommand(queries[i], connection);
                    commandCascade.ExecuteNonQuery();
                }

            }
        }
        public void Update(T obj)
        {
            string idProperty = GetIdProperty(obj);

            if (string.IsNullOrEmpty(idProperty))
                throw new NullReferenceException(nameof(idProperty));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string updateStr = MapEntityPropertyValuesInUpdateString(obj);
                string idPropertyValue = GetIdPropertyValue(obj);

                query = $"UPDATE {tableName} SET {updateStr} WHERE {idProperty}={idPropertyValue}";

                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
        public void UpdateCascade(T obj)
        {
            string idProperty = GetIdProperty(obj);
            string[] names = GetNamesOfCollectionOrModel();

            if (string.IsNullOrEmpty(idProperty))
                throw new NullReferenceException(nameof(idProperty));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string updateStr = MapEntityPropertyValuesInUpdateString(obj, idProperty);
                string idPropertyValue = GetIdPropertyValue(obj);

                string query = $"UPDATE {typeof(T).Name} SET {updateStr} WHERE {idProperty}={idPropertyValue}";

                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();

                string[] queries = UpdateCascadeModelsOrCollection(obj, names, idProperty, idPropertyValue);

                for (int i = 0; i < queries.Length; i++)
                {
                    SqlCommand commandCascade = new SqlCommand(queries[i], connection);
                    commandCascade.ExecuteNonQuery();
                }
            }
        }
        public void Delete(Expression<Func<T, bool>> predicate)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = GetKeyInLambdaExpression(predicate);
                dynamic val = GetValueInLambdaExpression(predicate);

                query = $"DELETE FROM {tableName} WHERE {key}={val}";

                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
        public void DeleteCascade(Expression<Func<T, bool>> predicate)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string key = GetKeyInLambdaExpression(predicate);
                dynamic val = GetValueInLambdaExpression(predicate);
                List<string> tables = dbSetExtension.FindTablesNameWithForeignKeysForCascadeDelete(typeof(T).Name);
                List<string> fks = dbSetExtension.FindForeignKeysNameInTablesForCascadeDelete(typeof(T).Name);

                for (int i = 0; i < tables.Count; i++)
                {
                    string cascadeDeleteQuery = $"DELETE FROM {tables[i]} WHERE {fks[i]} = {val}";
                    SqlCommand commandCascadeDelete = new SqlCommand(cascadeDeleteQuery, connection);
                    commandCascadeDelete.ExecuteNonQuery();
                }

                string query = $"DELETE FROM {typeof(T).Name} WHERE {key}={val}";

                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
        public dynamic FromSqlRaw(string sql)
        {
            string keyWord = query.Split()[0] + query.Split()[1];
            List<T> entities = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (keyWord.ToLower() == "select*")
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                T entity = MapReaderToEntity(reader);
                                entities.Add(entity);
                            }
                        }

                        return entities;
                    }
                    else if (keyWord.ToLower() == "update" || keyWord.ToLower() == "insert" || keyWord.ToLower() == "delete")
                    {
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        object scalar = command.ExecuteScalar();
                        return scalar;
                    }
                }
            }
            return null;
        }
    }
}
