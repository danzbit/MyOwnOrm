using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public class CustomDbSetReflection<T> where T : class
    {
        private CustomDbSetService<T> dbSetService;
        public T MapReaderToEntity(SqlDataReader reader)
        {
            T entity = Activator.CreateInstance<T>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                object value = reader.GetValue(i);

                PropertyInfo property = typeof(T).GetProperty(columnName);
                property.SetValue(entity, value);
            }

            return entity;
        }

        public string MapEntityPropertyValuesInString(T obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                object value = prop.GetValue(obj);

                strBuilder.Append(value);

                if (i < properties.Length - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            return strBuilder.ToString();
        }

        public string MapEntityPropertyValuesInUpdateString(T obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                string columnName = prop.Name;
                object value = prop.GetValue(obj);

                strBuilder.Append($"{columnName}={value}");

                if (i < properties.Length - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            return strBuilder.ToString();
        }

        public string GetIdProperty(T obj)
        {
            Type type = obj.GetType();
            for (int i = 0; i < type.GetProperties().Length; i++)
            {
                PropertyInfo prop = type.GetProperties()[i];
                if (prop.Name == "Id")
                    return prop.Name;
                else if (Attribute.IsDefined(prop, typeof(CustomPrimaryKeyAttribute)))
                    return prop.Name;
            }
            return string.Empty;
        }

        public string GetIdPropertyValue(T obj)
        {
            Type type = typeof(T);
            for (int i = 0; i < type.GetProperties().Length; i++)
            {
                PropertyInfo prop = type.GetProperties()[i];
                if (prop.Name == "Id")
                    return prop.GetValue(obj).ToString();
                else if (Attribute.IsDefined(prop, typeof(CustomPrimaryKeyAttribute)))
                    return prop.GetValue(obj).ToString();
            }
            return string.Empty;
        }

        public string GetForeignKeyAttribute(dynamic obj)
        {
            T targetType = Activator.CreateInstance<T>();
            PropertyInfo[] properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                if (Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                {
                    var attribute = (ForeignKeyAttribute)property.GetCustomAttribute(typeof(ForeignKeyAttribute));

                    if (attribute.TargetType.Name == targetType.ToString())
                    {
                        return property.Name;
                    }
                }
            }
            return string.Empty;
        }

        public bool IsCollectionType(PropertyInfo property)
        {
            return property.PropertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public List<string> GetPropertyValues(Expression<Func<T, object>>[] expressions)
        {
            List<string> result = new List<string>();
            foreach (var expression in expressions)
            {
                if (expression.Body is MemberExpression memberExpression)
                {
                    result.Add(memberExpression.Member.Name);
                }
                else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operand)
                {
                    result.Add(operand.Member.Name);
                }
                else
                {
                    result.Add(string.Empty);
                }
            }

            return result;
        }

        public dynamic[] GetIncludeTypes(Expression<Func<T, object>>[] includes)
        {
            List<dynamic> result = new List<dynamic>();

            foreach (var include in includes)
            {
                List<string> propVal = GetPropertyValues(new Expression<Func<T, object>>[] { include });

                PropertyInfo[] includeProperties = typeof(T).GetProperties();

                foreach (PropertyInfo property in includeProperties)
                {
                    if (property.Name == propVal[0])
                    {
                        Type propertyType = property.PropertyType;

                        if (IsCollectionType(property))
                        {
                            Type elementType = propertyType.GetGenericArguments()[0];
                            Type listType = typeof(List<>).MakeGenericType(elementType);
                            dynamic listInstance = Activator.CreateInstance(listType);

                            dynamic elementInstance = Activator.CreateInstance(elementType);
                            listInstance.Add(elementInstance);

                            result.Add(listInstance);
                        }
                        else
                        {
                            object includeInstance = Activator.CreateInstance(propertyType);

                            PropertyInfo[] includeInstanceProperties = propertyType.GetProperties();
                            foreach (PropertyInfo includeProperty in includeInstanceProperties)
                            {
                                includeProperty.SetValue(includeInstance, null);
                            }

                            result.Add(includeInstance);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        public dynamic GetIncludeType(Expression<Func<T, object>> include)
        {
            string propVal = dbSetService.GetPropertyValue(include);

            PropertyInfo[] includeProperties = typeof(T).GetProperties();

            foreach (PropertyInfo property in includeProperties)
            {
                if (property.Name == propVal)
                {
                    Type propertyType = property.PropertyType;

                    if (IsCollectionType(property))
                    {
                        Type elementType = propertyType.GetGenericArguments()[0];
                        Type listType = typeof(List<>).MakeGenericType(elementType);
                        dynamic listInstance = Activator.CreateInstance(listType);

                        dynamic elementInstance = Activator.CreateInstance(elementType);
                        listInstance.Add(elementInstance);

                        return listInstance;
                    }
                    else
                    {
                        object includeInstance = Activator.CreateInstance(propertyType);

                        PropertyInfo[] includeInstanceProperties = propertyType.GetProperties();
                        foreach (PropertyInfo includeProperty in includeInstanceProperties)
                        {
                            includeProperty.SetValue(includeInstance, null);
                        }

                        return includeInstance;
                    }
                }

            }
            return null;
        }

        public string[] GetNamesOfCollectionOrModel()
        {
            List<string> res = new List<string>();
            T obj = Activator.CreateInstance<T>();

            PropertyInfo[] properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                Type propType = property.PropertyType;

                if (propType.IsGenericType)
                {
                    res.Add(property.Name);
                }
                else if (propType.IsClass && propType != typeof(string))
                {
                    res.Add(property.Name);
                }
            }

            return res.ToArray();
        }

        public dynamic GetIncludeTypeAndSetValues(T obj, string propVal)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.Name == propVal)
                {
                    Type propertyType = property.PropertyType;

                    if (IsCollectionType(property))
                    {
                        Type elementType = propertyType.GetGenericArguments()[0];
                        Type listType = typeof(List<>).MakeGenericType(elementType);
                        dynamic listInstance = Activator.CreateInstance(listType);

                        MethodInfo addMethod = listType.GetMethod("Add");

                        dynamic propertyValue = property.GetValue(obj);

                        foreach (var item in propertyValue)
                        {
                            dynamic elementInstance = Activator.CreateInstance(elementType);

                            PropertyInfo[] propertyInstanceProperties = item.GetType().GetProperties();
                            foreach (var prop in propertyInstanceProperties)
                            {
                                var value = prop.GetValue(item);
                                prop.SetValue(elementInstance, value);
                            }

                            addMethod?.Invoke(listInstance, new object[] { elementInstance });
                        }

                        return listInstance;
                    }
                    else
                    {
                        object propertyValue = property.GetValue(obj);
                        object propertyInstance = Activator.CreateInstance(property.PropertyType);

                        PropertyInfo[] propertyInstanceProperties = propertyInstance.GetType().GetProperties();
                        foreach (var prop in propertyInstanceProperties)
                        {
                            var value = prop.GetValue(propertyValue);
                            prop.SetValue(propertyInstance, value);
                        }

                        return propertyInstance;
                    }
                }

            }
            return null;
        }

        public string[] UpdateCascadeModelsOrCollection(T obj, string[] names, string idProperty, string idPropertyValue)
        {
            List<string> queries = new List<string>();

            PropertyInfo[] properties = obj.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Type properyType = property.PropertyType;
                foreach (var name in names)
                {
                    if (property.Name == name)
                    {
                        if (properyType.IsGenericType)
                        {
                            dynamic entity = GetIncludeTypeAndSetValues(obj, name);
                            string[] strs = MapListEntitiesPropertyValuesInUpdateString(entity, idProperty);
                            string[] queryIes = dbSetService.GenerateUpdateSqlQueries(strs, property.Name, idProperty, idPropertyValue);
                            queries.AddRange(queryIes);
                        }
                        else if (properyType.IsClass && properyType != typeof(string))
                        {
                            dynamic entity = GetIncludeTypeAndSetValues(obj, name);
                            string str = MapEntityPropertyValuesInUpdateString(entity, idProperty);
                            string query = dbSetService.GenerateUpdateSqlQuery(str, property.Name, idProperty, idPropertyValue);
                            queries.Add(query);
                        }
                    }
                }
            }

            return queries.ToArray();
        }

        public string MapEntityPropertyValuesInString(dynamic obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];

                object value = prop.GetValue(obj);

                if (prop.PropertyType == typeof(string))
                {
                    strBuilder.Append($"'{value}'");
                }
                else
                {
                    strBuilder.Append(value.ToString());
                }

                if (i < properties.Length - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            return strBuilder.ToString();
        }

        public string[] InsertCascadeModelsOrCollection(T obj, string[] names)
        {
            List<string> queries = new List<string>();

            PropertyInfo[] properties = obj.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Type properyType = property.PropertyType;
                foreach (var name in names)
                {
                    if (property.Name == name)
                    {
                        if (properyType.IsGenericType)
                        {
                            dynamic entity = GetIncludeTypeAndSetValues(obj, name);
                            string[] strs = MapListEntitiesPropertyValuesInString(entity, property.Name);
                            string[] querIes = dbSetService.GenerateInsertSqlQueries(strs, property.Name);
                            queries.AddRange(querIes);
                        }
                        else if (properyType.IsClass && properyType != typeof(string))
                        {
                            dynamic entity = GetIncludeTypeAndSetValues(obj, name);
                            string str = MapEntityPropertyValuesInString(entity);
                            string query = dbSetService.GenerateInsertSqlQuery(str, property.Name);
                            queries.Add(query);
                        }
                    }
                }
            }

            return queries.ToArray();
        }

        public string MapEntityPropertyValuesInString(T obj, string[] names)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                bool isCollectionOrModel = false;
                PropertyInfo prop = properties[i];

                foreach (string name in names)
                {
                    if (prop.Name == name)
                    {
                        isCollectionOrModel = true;
                        break;
                    }
                }
                if (isCollectionOrModel == true)
                    continue;

                object value = prop.GetValue(obj);

                if (prop.PropertyType == typeof(string))
                {
                    strBuilder.Append($"'{value}'");
                }
                else
                {
                    strBuilder.Append(value);
                }

                if (i < properties.Length - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                return strBuilder.ToString();
            }

            return strBuilder.ToString();
        }

        public string[] MapListEntitiesPropertyValuesInString(dynamic obj, string tableName)
        {
            List<string> strs = new List<string>();
            Type elementType = obj.GetType().GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            dynamic listInstance = Activator.CreateInstance(listType);

            MethodInfo addMethod = listType.GetMethod("Add");

            dynamic elementInstance = Activator.CreateInstance(elementType);

            foreach (var item in obj)
            {
                StringBuilder strBuilder = new StringBuilder();
                PropertyInfo[] propertyInstanceProperties = item.GetType().GetProperties();
                for (int i = 0; i < propertyInstanceProperties.Length; i++)
                {
                    try
                    {
                        var value = propertyInstanceProperties[i].GetValue(item);

                        if (propertyInstanceProperties[i].PropertyType == typeof(string))
                        {
                            strBuilder.Append($"'{value.ToString()}'");
                        }
                        else
                        {
                            strBuilder.Append(value);
                        }
                        if (i < propertyInstanceProperties.Length - 1)
                        {
                            strBuilder.Append(", ");
                        }
                        propertyInstanceProperties[i].SetValue(elementInstance, value);
                    }
                    catch (RuntimeBinderException)
                    {
                        continue;
                    }
                }

                if (strBuilder.ToString().EndsWith(' '))
                {
                    strBuilder.Length = strBuilder.Length - 2;
                }
                addMethod?.Invoke(listInstance, new object[] { elementInstance });
                strs.Add(strBuilder.ToString());
            }

            return strs.ToArray();
        }

        public string MapEntityPropertyValuesInUpdateString(T obj, string idProperty)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                string columnName = prop.Name;
                object value = prop.GetValue(obj);

                if (columnName == idProperty) continue;

                if (prop.PropertyType.IsGenericType || (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))) continue;

                if (prop.PropertyType == typeof(string))
                {
                    strBuilder.Append($"{columnName}='{value}'");
                }
                else
                {
                    strBuilder.Append($"{columnName}={value}");
                }

                if (i < properties.Length - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
            }

            return strBuilder.ToString();
        }

        public string MapEntityPropertyValuesInUpdateString(dynamic obj, string idProperty)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                string columnName = prop.Name;

                object value = prop.GetValue(obj);

                if (columnName == idProperty) continue;

                if (prop.PropertyType == typeof(string))
                {
                    strBuilder.Append($"{columnName}='{value}'");
                }
                else
                {
                    strBuilder.Append($"{columnName}={value}");
                }

                if (i < properties.Length - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
            }

            return strBuilder.ToString();
        }

        public string[] MapListEntitiesPropertyValuesInUpdateString(dynamic obj, string idProperty)
        {
            List<string> strs = new List<string>();
            Type elementType = obj.GetType().GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            dynamic listInstance = Activator.CreateInstance(listType);

            MethodInfo addMethod = listType.GetMethod("Add");

            dynamic elementInstance = Activator.CreateInstance(elementType);

            foreach (var item in obj)
            {
                StringBuilder strBuilder = new StringBuilder();
                PropertyInfo[] propertyInstanceProperties = item.GetType().GetProperties();
                for (int i = 0; i < propertyInstanceProperties.Length; i++)
                {
                    try
                    {
                        var value = propertyInstanceProperties[i].GetValue(item);

                        if (value == null) continue;
                        string columnName = propertyInstanceProperties[i].Name;

                        if (columnName == idProperty) continue;

                        if (propertyInstanceProperties[i].PropertyType == typeof(string))
                        {
                            strBuilder.Append($"{columnName}='{value}'");
                        }
                        else
                        {
                            strBuilder.Append($"{columnName}={value}");
                        }
                        if (i < propertyInstanceProperties.Length - 1)
                        {
                            strBuilder.Append(", ");
                        }
                        propertyInstanceProperties[i].SetValue(elementInstance, value);
                    }
                    catch (RuntimeBinderException ex)
                    {
                        continue;
                    }
                }

                if (strBuilder.ToString().EndsWith(' '))
                {
                    strBuilder.Length = strBuilder.Length - 2;
                }
                addMethod?.Invoke(listInstance, new object[] { elementInstance });
                strs.Add(strBuilder.ToString());
            }

            return strs.ToArray();
        }

        public Type GetTypeCollectionArguments(PropertyInfo property)
        {
            return property.PropertyType.GetGenericArguments()[0];
        }

        public IList GetIListType(PropertyInfo property)
        {
            Type elementType = GetTypeCollectionArguments(property);
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList entitiesSecond = (IList)Activator.CreateInstance(listType);

            return entitiesSecond;
        }

        public T CreateInstanceType()
        {
            return Activator.CreateInstance<T>();
        }
        public object MapToEntityIncludeMethodGenericType(SqlDataReader reader, Type elementType)
        {
            object entitySecond = Activator.CreateInstance(elementType);

            PropertyInfo[] properties = elementType.GetProperties();

            foreach (PropertyInfo propertySecond in properties)
            {
                string propertyNameSecond = propertySecond.Name;


                int ordinalSecond;

                try
                {
                    ordinalSecond = reader.GetOrdinal(propertyNameSecond);
                }
                catch (IndexOutOfRangeException e)
                {
                    continue;
                }

                object valueSecond = reader.GetValue(ordinalSecond);

                propertySecond.SetValue(entitySecond, valueSecond);
            }

            return entitySecond;
        }

        public object MapToEntityIncludeMethodCustomClass(dynamic includeType, SqlDataReader reader)
        {
            object entitySecond = Activator.CreateInstance(includeType.GetType());

            PropertyInfo[] properties = includeType.GetType().GetProperties();

            foreach (PropertyInfo propertySecond in properties)
            {
                string propertyNameSecond = propertySecond.Name;


                int ordinalSecond;

                try
                {
                    ordinalSecond = reader.GetOrdinal(propertyNameSecond);
                }
                catch (IndexOutOfRangeException e)
                {
                    continue;
                }

                object valueSecond = reader.GetValue(ordinalSecond);

                propertySecond.SetValue(entitySecond, valueSecond);
            }

            return entitySecond;
        }

        public void SetPropertiesForIncludeExpressionsMethod(SqlDataReader reader, string idValue, T entity)
        {
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
        }

        public object MapToGenericEntityForIncludeExpressionsMethod(SqlDataReader reader, Type elementType)
        {
            object entitySecond = Activator.CreateInstance(elementType);

            PropertyInfo[] properties = elementType.GetProperties();

            foreach (PropertyInfo propertySecond in properties)
            {
                string propertyNameSecond = propertySecond.Name;

                int ordinalSecond;

                try
                {
                    ordinalSecond = reader.GetOrdinal(propertyNameSecond);
                }
                catch (IndexOutOfRangeException)
                {
                    continue;
                }

                object valueSecond = reader.GetValue(ordinalSecond);
                propertySecond.SetValue(entitySecond, valueSecond);
            }

            return entitySecond;
        }

        public object MapToEntityForIncludeExpressionsMethod(Type propertyType, SqlDataReader reader)
        {
            object entitySecond = Activator.CreateInstance(propertyType);

            PropertyInfo[] properties = propertyType.GetProperties();

            foreach (PropertyInfo propertySecond in properties)
            {
                string propertyNameSecond = propertySecond.Name;

                int ordinalSecond;

                try
                {
                    ordinalSecond = reader.GetOrdinal(propertyNameSecond);
                }
                catch (IndexOutOfRangeException)
                {
                    continue;
                }

                object valueSecond = reader.GetValue(ordinalSecond);
                propertySecond.SetValue(entitySecond, valueSecond);
            }
            return entitySecond;
        }

        public PropertyInfo GetPropertyInfo (string propVal)
        {
            return typeof(T).GetProperty(propVal);
        }
    }
}
