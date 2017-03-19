using System;
using System.Collections.Generic;
using RustCalc.Common.Serializing;

namespace RustCalc.Common
{
    public static class Extensions
    {
        public static SerializableList<T> ToSerializableList<T>(this IEnumerable<T> enumerable, bool hasDerivativeTypes = false) where T : class, IBinarySerializable
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

        public static SerializableDictionary<TKey, TValue> ToSerializableDictionary<TDictKey, TDictValue, TKey, TValue>(this IDictionary<TDictKey, TDictValue> dictionary,
                                                                                                                        Func<KeyValuePair<TDictKey, TDictValue>, TKey> keySelector,
                                                                                                                        Func<KeyValuePair<TDictKey, TDictValue>, TValue> valueSelector) where TValue : class, IBinarySerializable
        {
            var result = new SerializableDictionary<TKey, TValue>();

            foreach (KeyValuePair<TDictKey, TDictValue> kv in dictionary)
                result.Add(keySelector(kv), valueSelector(kv));

            return result;
        }
    }
}