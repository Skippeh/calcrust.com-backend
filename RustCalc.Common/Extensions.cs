using System.Collections.Generic;
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
    }
}