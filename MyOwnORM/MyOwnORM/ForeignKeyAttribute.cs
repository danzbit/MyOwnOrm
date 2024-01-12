using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        public Type TargetType { get; }

        public ForeignKeyAttribute(Type targetType)
        {
            TargetType = targetType;
        }
    }
}
