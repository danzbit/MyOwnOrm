using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class StringLengthAttribute : ValidationAttribute
    {
        readonly int _length;
        public StringLengthAttribute(int length)
        {
            _length = length;
        }
        public int Length { get { return _length; } }
        public override bool IsValid(object? value)
        {
            var column = value.ToString();
            bool result = true;
            if (this.Length != null)
            {
                result = MatchLength(_length, column);
            }
            return base.IsValid(value);
        }

        internal bool MatchLength(int length, string columnLength)
        {
            if (length != columnLength.Length) { return false; }
            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return String.Format(CultureInfo.CurrentCulture,
              ErrorMessageString, name, this.Length);
        }
    }
}
