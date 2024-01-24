using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        public string Name { get; }
        public TableAttribute() { }
        public TableAttribute(string name) => Name = name;
    }
}
