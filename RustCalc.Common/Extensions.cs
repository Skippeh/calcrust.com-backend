using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RustCalc.Common.Serializing;

namespace RustCalc.Common
{
    public static class Extensions
    {
        public static SerializableList<T> ToSerializableList<T>(this IEnumerable<T> enumerable, bool hasDerivativeTypes = false) where T : IBinarySerializable
        {
            var list = new SerializableList<T>(hasDerivativeTypes);
            list.AddRange(enumerable);
            return list;
        }
        
        // Wrapping in an extension method incase we need to make changes in the future.
        /// <summary>Returns the fully qualified name for the specified type without assembly information.</summary>
        public static string GetTypeName(this Type type)
        {
            return type.ToString();
        }
    }
}