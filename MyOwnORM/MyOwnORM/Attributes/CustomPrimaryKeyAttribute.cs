using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomPrimaryKeyAttribute : Attribute
    {
        public string Id { get; }
        public CustomPrimaryKeyAttribute() { }
        public CustomPrimaryKeyAttribute(string id) => Id = id;
    }
}
