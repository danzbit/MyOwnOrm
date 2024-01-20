using Microsoft.CSharp.RuntimeBinder;
using MyOwnORM.Attributes;
using MyOwnORM.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM.Reflection
{
    public class CustomDbSetReflection<T> where T : class
    {
        private readonly CustomDbSetReflectionHelper<T> reflectionHelper;
        private readonly CustomDbSetService<T> dbSetService;

        public CustomDbSetReflection()
        {
            
        }
        public CustomDbSetReflection(string connectionString)
        {
            dbSetService = new CustomDbSetService<T>(connectionString);
            reflectionHelper = new CustomDbSetReflectionHelper<T>();
        }

        public T MapReaderToEntity(SqlDataReader reader)
        {
            T entity = Activator.CreateInstance<T>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                object value = reader.GetValue(i);

                PropertyInfo property = typeof(T).GetProperty(columnName);
                if (property == null)
                {
                    foreach (PropertyInfo prop in typeof(T).GetProperties())
                    {
                        if (GetIdOfCustomPKAttribute(prop) == columnName)
                        {
                            if (prop.PropertyType == typeof(Guid))
                            {
                                prop.SetValue(entity, new Guid(value.ToString()));
                                
                            }
                           else
                            {
                                prop.SetValue(entity, value);
                            }
                            break;
                        }
                        else if (GetNameOfColumnAttribute(prop) == columnName)
                        {
                            if (int.TryParse(value.ToString(), out int n))
                            {
                                prop.SetValue(entity, n);
                            }
                            else
                            {
                                prop.SetValue(entity, value);
                            }
                            break;
                        }
                    }
                }
                else
                {
                    property.SetValue(entity, value);
                }
            }

            return entity;
        }

        public string GetIdProperty(T obj)
        {
            Type type = obj.GetType();
            for (int i = 0; i < type.GetProperties().Length; i++)
            {
                PropertyInfo prop = type.GetProperties()[i];
                if (Attribute.IsDefined(prop, typeof(CustomPrimaryKeyAttribute)))
                {
                    var attr = prop.GetCustomAttribute<CustomPrimaryKeyAttribute>(true);
                    return attr.Id ?? prop.Name;
                }
                else if (prop.Name.ToLower() == "id")
                {
                    return prop.Name;
                }
            }
            return string.Empty;
        }

        public object GetIdPropertyValue(T obj)
        {
            object id = null;
            Type type = typeof(T);
            for (int i = 0; i < type.GetProperties().Length; i++)
            {
                var prop = type.GetProperties()[i];
                if (prop.Name == "Id")
                    id = prop.GetValue(obj);
                else if (Attribute.IsDefined(prop, typeof(CustomPrimaryKeyAttribute)))
                    id = prop.GetValue(obj);
            }

            if (id != null && (id.GetType() == typeof(string) || id.GetType() == typeof(Guid))) 
            {
                return $"'{id}'";
            }

            return id;
        }

        public string GetForeignKeyAttribute(Type obj)
        {
            T targetType = Activator.CreateInstance<T>();
            PropertyInfo[] properties = obj.GetProperties();

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

        public object[] GetIncludeTypes(Expression<Func<T, object>>[] includes)
        {
            List<object> result = new List<object>();

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
                            object listInstance = Activator.CreateInstance(listType);

                            MethodInfo addMethod = listType.GetMethod("Add"); 

                            object elementInstance = Activator.CreateInstance(elementType);
                            addMethod?.Invoke(listInstance, new[] { elementInstance });

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

        public object GetIncludeType(Expression<Func<T, object>> include)
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
                        object listInstance = Activator.CreateInstance(listType);

                        MethodInfo addMethod = listType.GetMethod("Add");

                        object elementInstance = Activator.CreateInstance(elementType);
                        addMethod?.Invoke(listInstance, new[] { elementInstance });

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

        public object GetIncludeTypeAndSetValues(T obj, string propVal)
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
                        object listInstance = Activator.CreateInstance(listType);

                        MethodInfo addMethod = listType.GetMethod("Add");

                        object propertyValue = property.GetValue(obj);

                        foreach (var item in (IEnumerable<object>)propertyValue)
                        {
                            object elementInstance = Activator.CreateInstance(elementType);

                            PropertyInfo[] propertyInstanceProperties = item.GetType().GetProperties();
                            foreach (var prop in propertyInstanceProperties)
                            {
                                if (HasNotMappedAttribute(prop))
                                {
                                    continue;
                                }
                                else
                                {
                                    var value = prop.GetValue(item);
                                    prop.SetValue(elementInstance, value);
                                }
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
                            if (HasNotMappedAttribute(prop))
                            {
                                prop.SetValue(propertyInstance, null);
                            }
                            else
                            {
                                var value = prop.GetValue(propertyValue);
                                prop.SetValue(propertyInstance, value);
                            }
                        }

                        return propertyInstance;
                    }
                }

            }
            return null;
        }

        public object GetGenericType(T obj, string propVal)
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
                        return Activator.CreateInstance(elementType);
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
                            object entity = GetIncludeTypeAndSetValues(obj, name);
                            string[] strs = reflectionHelper.MapListEntitiesPropertyValuesInUpdateString(entity, idProperty);
                            object type = GetGenericType(obj, name);
                            string tableName = GetTableName(obj);
                            string fk = GetRealFKName(type, tableName);
                            string[] queryIes = dbSetService.GenerateUpdateSqlQueries(strs, type.GetType().Name, fk, idPropertyValue);
                            queries.AddRange(queryIes);
                        }
                        else if (properyType.IsClass && properyType != typeof(string))
                        {
                            object entity = GetIncludeTypeAndSetValues(obj, name);
                            string str = reflectionHelper.MapEntityPropertyValuesInUpdateString(entity.GetType(), idProperty);
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

            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Type properyType = property.PropertyType;
                foreach (var name in names)
                {
                    if (property.Name == name)
                    {
                        if (properyType.IsGenericType)
                        {
                            object entity = GetIncludeTypeAndSetValues(obj, name);
                            string[] strs = reflectionHelper.MapListEntitiesPropertyValuesInString(entity);
                            string[] querIes = dbSetService.GenerateInsertSqlQueries(strs, property.Name);
                            queries.AddRange(querIes);
                        }
                        else if (properyType.IsClass && properyType != typeof(string))
                        {
                            object entity = GetIncludeTypeAndSetValues(obj, name);
                            string str = reflectionHelper.MapEntityPropertyValuesInString(entity.GetType());
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

        public object MapToEntityIncludeMethodCustomClass(Type includeType, SqlDataReader reader)
        {
            object entitySecond = Activator.CreateInstance(includeType);

            PropertyInfo[] properties = includeType.GetProperties();

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

                if (property.PropertyType == typeof(Guid))
                {
                    property.SetValue(entity, new Guid(idValue));
                }
                else
                {
                    property.SetValue(entity, value);
                }
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

        public string GetTableName()
        {
            var tableAttribute = typeof(T).GetCustomAttributes(
                typeof(TableAttribute), true
            ).FirstOrDefault() as TableAttribute;
            if (tableAttribute != null)
            {
                return tableAttribute.Name;
            }
                
            return typeof(T).Name;
        }

        public string GetTableName(object obj)
        {
            var tableAttribute = obj.GetType().GetCustomAttributes(
                typeof(TableAttribute), true
            ).FirstOrDefault() as TableAttribute;
            if (tableAttribute != null)
            {
                return tableAttribute.Name;
            }

            return obj.GetType().Name;
        }

        public string GetTableName<TModel>(TModel obj)
        {
            var tableAttribute = obj.GetType().GetCustomAttributes(
                typeof(TableAttribute), true
            ).FirstOrDefault() as TableAttribute;
            if (tableAttribute != null)
            {
                return tableAttribute.Name;
            }

            return typeof(TModel).Name;
        }

        public string GetColumnName(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                ColumnAttribute columnAttribute = attr as ColumnAttribute;
                if (columnAttribute != null)
                {
                    return columnAttribute.Column;
                }
            }

            return property.Name;
        }

        public bool HasNotMappedAttribute(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                NotMappedAttribute columnAttribute = attr as NotMappedAttribute;
                if (columnAttribute != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasCustomPKAttribute(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                CustomPrimaryKeyAttribute columnAttribute = attr as CustomPrimaryKeyAttribute;
                if (columnAttribute != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasColumnAttribute(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                ColumnAttribute columnAttribute = attr as ColumnAttribute;
                if (columnAttribute != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPropertyOfModel(PropertyInfo propertyInfo, object obj)
        {
            return obj.GetType().IsAssignableFrom(propertyInfo.DeclaringType);
        }

        public bool IsClassInheritsAnotherClass(object obj)
        {
            return typeof(T).IsAssignableFrom(obj.GetType());
        }

        public string GetIdOfCustomPKAttribute(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                CustomPrimaryKeyAttribute columnAttribute = attr as CustomPrimaryKeyAttribute;
                if (columnAttribute != null)
                {
                    return columnAttribute.Id ?? property.Name;
                }
            }

            return string.Empty;
        }

        public string GetNameOfColumnAttribute(PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                ColumnAttribute columnAttribute = attr as ColumnAttribute;
                if (columnAttribute != null)
                {
                    return columnAttribute.Column;
                }
            }

            return string.Empty;
        }

        public bool IsPropertyGuid(string property)
        {
            PropertyInfo prop = typeof(T).GetProperty(property);

            if (prop.PropertyType == typeof(Guid))
            {
                return true;
            }

            return false;
        }

        public string GetRealColumnName(string key)
        {
            PropertyInfo propertyInfo = typeof(T).GetProperty(key);
            
            if (HasColumnAttribute(propertyInfo))
            {
                return GetNameOfColumnAttribute(propertyInfo);
            }
            else if (HasCustomPKAttribute(propertyInfo))
            {
                return GetIdOfCustomPKAttribute(propertyInfo);
            }

            return key;
        }

        public string GetRealColumnName(string key, object obj)
        {
            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                if (prop.Name == key)
                {
                    if (HasColumnAttribute(prop))
                    {
                        return GetNameOfColumnAttribute(prop);
                    }
                    else if (HasCustomPKAttribute(prop))
                    {
                        return GetIdOfCustomPKAttribute(prop);
                    }
                    break;
                }
            }

            return key;
        }

        public string GetRealColumnName<TModel>(string key, TModel obj)
        {
            foreach(PropertyInfo prop in obj.GetType().GetProperties())
            {
                if (prop.Name == key)
                {
                    if (HasColumnAttribute(prop))
                    {
                        return GetNameOfColumnAttribute(prop);
                    }
                    else if (HasCustomPKAttribute(prop))
                    {
                        return GetIdOfCustomPKAttribute(prop);
                    }
                    break;
                }
            }

            return key;
        }

        public string GetRealFKName(object entity, string tableName) 
        {
            Type type = entity.GetType();
            PropertyInfo[] properties = type.GetProperties();
            foreach (var property in properties)
            {
                object[] attrs = property.GetCustomAttributes(true);
                foreach (var attr in attrs)
                {
                    ForeignKeyAttribute columnAttribute = attr as ForeignKeyAttribute;
                    if (columnAttribute != null && tableName == columnAttribute.TargetType.Name)
                    {
                        return property.Name;
                    }
                }
            }
            return string.Empty;
        }
    }
}
