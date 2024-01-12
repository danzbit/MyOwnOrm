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
    public class CustomDbSet<T> : CustomDbSetRepository<T> where T : class
    {
        public CustomDbSet(string connectionString) : base(connectionString) 
        {
        }
    }
}
