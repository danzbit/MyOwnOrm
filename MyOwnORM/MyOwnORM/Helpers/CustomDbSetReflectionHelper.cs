using Microsoft.CSharp.RuntimeBinder;
using MyOwnORM.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM.Helper
{
    public class CustomDbSetReflectionHelper<T, TKey> where T : class
    {
        private readonly CustomDbSetReflection<T, TKey> dbSetReflection;
        public CustomDbSetReflectionHelper()
        {
            dbSetReflection = new CustomDbSetReflection<T, TKey>();
        }
        public string MapEntityPropertyValuesInString(T obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++) 
            {
                PropertyInfo prop = properties[i];

                if (dbSetReflection.HasNotMappedAttribute(properties[i]))
                {
                    continue;
                }

                object value = prop.GetValue(obj);

                if (value == null)
                {
                    continue;
                }

                if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid))
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

            if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                return strBuilder.ToString();
            }

            return strBuilder.ToString();
        }

        public string[] MapEntityPropertyValuesInString<TModel>(TModel obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();
            StringBuilder strBuilderModel = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];

                if (dbSetReflection.HasNotMappedAttribute(properties[i]))
                {
                    continue;
                }

                object value = prop.GetValue(obj);

                if (value == null)
                {
                    continue;
                }

                if (dbSetReflection.IsPropertyOfModel(prop, obj))
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
                    {
                        strBuilderModel.Append($"'{value}'");
                    }
                    else
                    {
                        strBuilderModel.Append(value.ToString());
                    }

                    if (i < properties.Length - 1)
                    {
                        strBuilderModel.Append(", ");
                    }
                }
                else if (prop.Name.ToLower() == "id")
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
                    {
                        strBuilderModel.Insert(0, $"'{value}'" + ", ");
                        strBuilder.Insert(0, $"'{value}'" + ", ");
                    }
                    else
                    {
                        strBuilderModel.Insert(0, value.ToString() + ", ");
                        strBuilder.Insert(0, value.ToString() + ", ");
                    }
                }
                else
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
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
            }

            if (strBuilder.ToString().EndsWith(' ') && strBuilderModel.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                strBuilderModel.Length = strBuilderModel.Length - 2;
                return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
            }
            else if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
            }
            else if (strBuilderModel.ToString().EndsWith(' '))
            {
                strBuilderModel.Length = strBuilderModel.Length - 2;
                return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
            }

            return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
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

                if (value == null)
                {
                    continue;
                }
                else if (columnName.ToLower() == "id" || columnName == idProperty)
                {
                    continue;
                }

                if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
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
                return strBuilder.ToString();
            }

            return strBuilder.ToString();
        }

        public string MapEntityPropertyValuesInString(Type obj)
        {
            PropertyInfo[] properties = obj.GetProperties();
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

            if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                return strBuilder.ToString();
            }

            return strBuilder.ToString();
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
                {
                    continue;
                }

                object value = prop.GetValue(obj);

                if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
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

        public string MapEntityPropertyValuesInUpdateString(string idProperty, T obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                string columnName = prop.Name;
                columnName = dbSetReflection.GetRealColumnName(columnName);
                object value = prop.GetValue(obj);

                if (dbSetReflection.HasNotMappedAttribute(prop)) continue;

                if (value == null) continue;

                if (columnName == idProperty || columnName.ToLower() == "id") continue;

                if (prop.PropertyType.IsGenericType || prop.PropertyType.IsClass && prop.PropertyType != typeof(string)) continue;

                if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
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

        public string[] MapEntityPropertyValuesInUpdateStringTPT<TModel>(string idProperty, TModel obj)
        {
            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            StringBuilder strBuilder = new StringBuilder();
            StringBuilder strBuilderModel = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                string columnName = prop.Name;
                columnName = dbSetReflection.GetRealColumnName(columnName, obj);

                if (dbSetReflection.HasNotMappedAttribute(properties[i]))
                {
                    continue;
                }

                object value = prop.GetValue(obj);

                if (value == null)
                {
                    continue;
                }

                if (dbSetReflection.IsPropertyOfModel(prop, obj))
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
                    {
                        strBuilderModel.Append($"{columnName}='{value}'");
                    }
                    else
                    {
                        strBuilderModel.Append($"{columnName}={value}");
                    }

                    if (i < properties.Length - 1)
                    {
                        strBuilderModel.Append(", ");
                    }
                }
                else if (prop.Name.ToLower() == "id" || prop.Name == dbSetReflection.GetIdOfCustomPKAttribute(prop))
                {
                    continue;
                }
                else
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Guid) || prop.PropertyType.IsEnum)
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
            }

            if (strBuilder.ToString().EndsWith(' ') && strBuilderModel.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                strBuilderModel.Length = strBuilderModel.Length - 2;
                return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
            }
            else if (strBuilder.ToString().EndsWith(' '))
            {
                strBuilder.Length = strBuilder.Length - 2;
                return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
            }
            else if (strBuilderModel.ToString().EndsWith(' '))
            {
                strBuilderModel.Length = strBuilderModel.Length - 2;
                return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
            }

            return new string[] { strBuilder.ToString(), strBuilderModel.ToString() };
        }

        public string MapEntityPropertyValuesInUpdateString(Type obj, string idProperty)
        {
            PropertyInfo[] properties = obj.GetProperties();
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
        public string[] MapListEntitiesPropertyValuesInUpdateString(object obj, string idProperty)
        {
            List<string> strs = new List<string>();
            Type elementType = obj.GetType().GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            object listInstance = Activator.CreateInstance(listType);

            MethodInfo addMethod = listType.GetMethod("Add");

            foreach (var item in (IEnumerable<object>)obj)
            {
                object elementInstance = Activator.CreateInstance(elementType);
                StringBuilder strBuilder = new StringBuilder();
                PropertyInfo[] propertyInstanceProperties = item.GetType().GetProperties();
                for (int i = 0; i < propertyInstanceProperties.Length; i++)
                {
                    try
                    {
                        var value = propertyInstanceProperties[i].GetValue(item);

                        if (value == null) continue;
                        string columnName = propertyInstanceProperties[i].Name;

                        if (columnName.ToLower() == "id") continue;

                        columnName = dbSetReflection.GetRealColumnName(columnName, elementInstance);

                        if (dbSetReflection.HasNotMappedAttribute(propertyInstanceProperties[i])) continue;

                        if (columnName == idProperty) continue;

                        if (propertyInstanceProperties[i].PropertyType == typeof(string) || propertyInstanceProperties[i].PropertyType == typeof(Guid) || propertyInstanceProperties[i].PropertyType.IsEnum)
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

        public string[] MapListEntitiesPropertyValuesInString(object obj)
        {
            List<string> strs = new List<string>();
            Type elementType = obj.GetType().GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            object listInstance = Activator.CreateInstance(listType);

            MethodInfo addMethod = listType.GetMethod("Add");

            object elementInstance = Activator.CreateInstance(elementType); 

            foreach (var item in (IEnumerable<object>)obj)
            {
                StringBuilder strBuilder = new StringBuilder();
                PropertyInfo[] propertyInstanceProperties = item.GetType().GetProperties();
                for (int i = 0; i < propertyInstanceProperties.Length; i++)
                {
                    try
                    {
                        if (dbSetReflection.HasNotMappedAttribute(propertyInstanceProperties[i]))
                        {
                            continue;
                        }
                        var value = propertyInstanceProperties[i].GetValue(item);

                        if (propertyInstanceProperties[i].PropertyType == typeof(string) || propertyInstanceProperties[i].PropertyType == typeof(Guid) || propertyInstanceProperties[i].PropertyType.IsEnum)
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

    }
}
