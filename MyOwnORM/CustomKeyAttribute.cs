using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomKeyAttribute : Attribute
    {
        public string Id { get; }
        public CustomKeyAttribute() { }
        public CustomKeyAttribute(string id) => Id = id;
    }
}
