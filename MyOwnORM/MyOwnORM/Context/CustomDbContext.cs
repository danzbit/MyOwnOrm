using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public class CustomDbContext : IDisposable
    {
        private readonly string _connectionString = string.Empty;
        private bool disposed = false;
        public CustomDbContext()
        {

        }

        public CustomDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

 
        public void Dispose()
        {
            
            Dispose(true); 

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {

            }
            disposed = true;
        }
        ~CustomDbContext()
        {
            Dispose(false);
        }
    }
}
