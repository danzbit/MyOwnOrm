using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MyOwnORM.Reflection;

namespace MyOwnORM
{
    public class CustomDbSetService<T> where T : class
    {
        private readonly string _connectionString;
        private readonly CustomDbSetReflection<T> dbSetReflection;
        
        public CustomDbSetService(string connectionString)
        {
            _connectionString = connectionString;
            dbSetReflection = new CustomDbSetReflection<T>();
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
    }
}
