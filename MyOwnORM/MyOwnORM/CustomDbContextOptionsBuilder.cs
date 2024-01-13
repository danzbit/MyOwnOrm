using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public class CustomDbContextOptionsBuilder 
    {
        public static void UseSqlServer(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connection opened!");
            }
        }
    }
}
