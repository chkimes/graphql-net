using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.GraphQL
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string val)
        {
            if (string.IsNullOrEmpty(val))
                return "";
            return char.ToLower(val[0]) + val.Substring(1);
        }
    }
}
