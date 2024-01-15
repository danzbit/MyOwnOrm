using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM.Helper
{
    public class CustomDbSetReflectionHelper<T> where T : class
    {
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

                if (prop.PropertyType.IsGenericType || prop.PropertyType.IsClass && prop.PropertyType != typeof(string)) continue;

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

    }
}
