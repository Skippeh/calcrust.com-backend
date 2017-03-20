using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static SerializableList<T> ToSerializableList<TValueType, T>(this IEnumerable<TValueType> enumerable, bool hasDerivativeTypes, Func<TValueType, T> valueSelector) where T : class, IBinarySerializable
        {
            var list = new SerializableList<T>(hasDerivativeTypes);
            list.AddRange(enumerable.Select(valueSelector));
            return list;
        } 
        
        // Wrapping in an extension method incase we need to make changes in the future.
        /// <summary>Returns the fully qualified name for the specified type without assembly information.</summary>
        public static string GetTypeName(this Type type)
        {
            return type.ToString();
        }

        public static SerializableDictionary<TKey, TValue> ToSerializableDictionary<TDictKey, TDictValue, TKey, TValue>(this IDictionary<TDictKey, TDictValue> dictionary, bool hasDerivativeTypes,
                                                                                                                        Func<KeyValuePair<TDictKey, TDictValue>, TKey> keySelector,
                                                                                                                        Func<KeyValuePair<TDictKey, TDictValue>, TValue> valueSelector) where TValue : class, IBinarySerializable
        {
            var result = new SerializableDictionary<TKey, TValue>(hasDerivativeTypes);

            foreach (KeyValuePair<TDictKey, TDictValue> kv in dictionary)
                result.Add(keySelector(kv), valueSelector(kv));

            return result;
        }

        public static T GetField<T, TClassType>(this object obj, string memberName)
        {
            var fieldInfo = GetFieldInfo<TClassType>(obj, memberName);
            return (T)fieldInfo.GetValue(obj);
        }

        private static FieldInfo GetFieldInfo<TClassType>(object obj, string memberName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var targetType = typeof (TClassType);
            var type = obj.GetType();

            while (type != targetType)
            {
                if (!type.IsSubclassOf(targetType) || type.BaseType == null)
                    throw new ArgumentException(obj.GetType().FullName + " does not equal or inherit type " + targetType.FullName + ".");

                type = type.BaseType;
            }

            var fieldInfo = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return fieldInfo;
        }

        public static void SetField<TClassType>(this object obj, string memberName, object value)
        {
            var fieldInfo = GetFieldInfo<TClassType>(obj, memberName);
            fieldInfo.SetValue(obj, value);
        }
    }
}