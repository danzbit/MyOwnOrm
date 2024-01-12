using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class CascadeDeleteAttribute : Attribute
    {
        public Type TargetType { get; }

        public CascadeDeleteAttribute(Type targetType)
        {
            TargetType = targetType;
        }
    }
}
