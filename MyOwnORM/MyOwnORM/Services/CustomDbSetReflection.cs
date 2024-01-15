using Microsoft.CSharp.RuntimeBinder;
using MyOwnORM.Attributes;
using MyOwnORM.Helper;
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

namespace MyOwnORM.Reflection
{
    public class CustomDbSetReflection<T> : CustomDbSetReflectionHelper<T> where T : class
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

        public PropertyInfo GetPropertyInfo(string propVal)
        {
            return typeof(T).GetProperty(propVal);
        }
    }
}
