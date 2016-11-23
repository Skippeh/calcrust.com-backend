using System;

namespace WebAPI
{
    internal static class Utility
    {
        public static string ToCamelCaseString(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            string str = obj.ToString();

            if (str.Length <= 1)
                return str.ToLower();

            return str.Substring(0, 1).ToLower() + str.Substring(1);
        }
    }
}