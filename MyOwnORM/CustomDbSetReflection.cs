using Microsoft.CSharp.RuntimeBinder;
using System;
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
        private protected static T MapReaderToEntity(SqlDataReader reader)
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

        private protected static string MapEntityPropertyValuesInString(T obj)
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

        private protected static string MapEntityPropertyValuesInUpdateString(T obj)
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

        private protected static string GetIdProperty(T obj)
        {
            Type type = obj.GetType();
                for (int i = 0; i < type.GetProperties().Length; i++)
                {
                    PropertyInfo prop = type.GetProperties()[i];
                    if (prop.Name == "Id")
                        return prop.Name;
                    else if (Attribute.IsDefined(prop, typeof(CustomKeyAttribute)))
                        return prop.Name;
                }
                return string.Empty;
            }

        private protected static string GetIdPropertyValue(T obj)
        {
            Type type = typeof(T);
            for (int i = 0; i < type.GetProperties().Length; i++)
            {
                PropertyInfo prop = type.GetProperties()[i];
                if (prop.Name == "Id")
                    return prop.GetValue(obj).ToString();
                else if (Attribute.IsDefined(prop, typeof(CustomKeyAttribute)))
                    return prop.GetValue(obj).ToString();
            }
            return string.Empty;
        }

        private protected static string GetKeyInLambdaExpression(Expression<Func<T, bool>> propertyLambda)
        {
            var binaryExpression = propertyLambda.Body as BinaryExpression;
            var memberExpression = binaryExpression.Left as MemberExpression;
            var propertyInfo = memberExpression.Member as PropertyInfo;
            return propertyInfo.Name;
        }
        private protected static dynamic GetValueInLambdaExpression(Expression<Func<T, bool>> propertyLambda)
        {
            var binaryExpression = propertyLambda.Body as BinaryExpression;
            var memberExpression = binaryExpression.Right.ToString();
            if (memberExpression.StartsWith("\"") && memberExpression.EndsWith("\""))
            {
                return memberExpression.Substring(1, memberExpression.Length - 2);
            }
            return memberExpression;
        }

        private protected static string GetForeignKeyAttribute(dynamic obj)
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

        private protected static bool IsCollectionType(PropertyInfo property)
        {
            return property.PropertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private protected static List<string> GetPropertyValues(Expression<Func<T, object>>[] expressions)
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

        private protected static dynamic[] GetIncludeTypes(Expression<Func<T, object>>[] includes)
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

        private protected static string GetPropertyValue(Expression<Func<T, object>> expression)
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

        private protected static dynamic GetIncludeType(Expression<Func<T, object>> include)
        {
            string propVal = GetPropertyValue(include);

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

        private protected static string[] GetNamesOfCollectionOrModel()
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

        private protected static dynamic GetIncludeTypeAndSetValues(T obj, string propVal)
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

        private protected static string[] UpdateCascadeModelsOrCollection(T obj, string[] names, string idProperty, string idPropertyValue)
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
                            string[] queryIes = MapListEntitiesPropertyValuesInUpdateString(entity, property.Name, idProperty, idPropertyValue);
                            queries.AddRange(queryIes);
                        }
                        else if (properyType.IsClass && properyType != typeof(string))
                        {
                            dynamic entity = GetIncludeTypeAndSetValues(obj, name);
                            string query = MapEntityPropertyValuesInUpdateString(entity, property.Name, idProperty, idPropertyValue);
                            queries.Add(query);
                        }
                    }
                }
            }

            return queries.ToArray();
        }

        private protected static string MapEntityPropertyValuesInString(dynamic obj, string tableName)
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

            return $"INSERT INTO {tableName} VALUES ({strBuilder.ToString()})";
        }

        private protected static string[] InsertCascadeModelsOrCollection(T obj, string[] names)
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
                            string[] queryIes = MapListEntitiesPropertyValuesInString(entity, property.Name);
                            queries.AddRange(queryIes);
                        }
                        else if (properyType.IsClass && properyType != typeof(string))
                        {
                            dynamic entity = GetIncludeTypeAndSetValues(obj, name);
                            string query = MapEntityPropertyValuesInString(entity, property.Name);
                            queries.Add(query);
                        }
                    }
                }
            }

            return queries.ToArray();
        }

        private protected static string MapEntityPropertyValuesInString(T obj, string[] names)
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

        private protected static string[] MapListEntitiesPropertyValuesInString(dynamic obj, string tableName)
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

            List<string> res = new List<string>();
            foreach (var str in strs)
                res.Add($"INSERT INTO {tableName} VALUES ({str})");

            return res.ToArray();
        }

        private protected static string MapEntityPropertyValuesInUpdateString(T obj, string idProperty)
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

        private protected static string MapEntityPropertyValuesInUpdateString(dynamic obj, string tableName, string idProperty, string idPropertyValue)
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

            return $"UPDATE {tableName} SET {strBuilder} WHERE {idProperty}={idPropertyValue}";
        }

        private protected static string[] MapListEntitiesPropertyValuesInUpdateString(dynamic obj, string tableName, string idProperty, string idPropertyValue)
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

            List<string> res = new List<string>();
            foreach (var str in strs)
                res.Add($"UPDATE {tableName} SET {str} WHERE {idProperty}={idPropertyValue}");

            return res.ToArray();
        }
    }
}
