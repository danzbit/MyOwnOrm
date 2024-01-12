using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public class CustomDbSetExtension
    {
        private readonly string _connectionString;
        public CustomDbSetExtension(string connectionString)
        {
            _connectionString = connectionString;
        }
        public List<string> FindTablesNameWithForeignKeysForCascadeDelete(string tableName)
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
                        while (reader.Read())
                        {
                            object table = reader["TableName"];
                            res.Add(table.ToString());
                        }
                    }
                }
            }

            return res;
        }

        public List<string> FindForeignKeysNameInTablesForCascadeDelete(string tableName)
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
                        while (reader.Read())
                        {
                            object table = reader["ForeignKeyColumnName"];
                            res.Add(table.ToString());
                        }
                    }
                }
            }

            return res;
        }


    }
}
