using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RustCalc.Common.Serializing
{
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IBinarySerializable where TValue : class, IBinarySerializable
    {
        private static readonly Type[] allowedKeyTypes =
        {
            typeof(string),
            typeof(int),
            typeof(short),
            typeof(long)
        };

        static SerializableDictionary()
        {
            if (!allowedKeyTypes.Contains(typeof(TKey)))
            {
                throw new ArgumentException("TKey is not a valid type.");
            }
        }

        /// <summary>If set to false, the value type names will not be written and on deserialization it will be assumed that all items are of the same type as <typeparamref name="T"/>.</summary>
        public bool HasDerivativeTypes { get; set; }

        /// <summary>Called right before serializing an item.</summary>
        public Action<TValue, BinaryWriter> OnSerializeItem { get; set; }

        /// <summary>Called right before deserializing an item. If a non null value is returned that instance will be used for deserializing instead.</summary>
        public Func<Type, BinaryReader, TValue> OnDeserializeItem { get; set; }

        public SerializableDictionary(bool hasDerivativeTypes = false)
        {
            HasDerivativeTypes = hasDerivativeTypes;
        }  
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(HasDerivativeTypes);
            writer.Write(Count);
            foreach (var kv in this)
            {
                WriteKey(kv.Key, writer);

                if (HasDerivativeTypes)
                    writer.Write(typeof (TValue).FullName);

                OnSerializeItem?.Invoke(kv.Value, writer);
                writer.Write(kv.Value);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            HasDerivativeTypes = reader.ReadBoolean();
            int count = reader.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                object key = ReadKey(reader);
                Type valueType;

                if (HasDerivativeTypes)
                {
                    valueType = Type.GetType(reader.ReadString());
                    if (valueType == null) throw new ArgumentNullException(nameof(valueType));
                }
                else
                    valueType = typeof (TValue);

                TValue instance;

                instance = OnDeserializeItem?.Invoke(valueType, reader) ?? (TValue) Activator.CreateInstance(valueType, true);
                instance.Deserialize(reader);

                Add((TKey) key, instance);
            }
        }

        void WriteKey(object key, BinaryWriter writer)
        {
            if (key is string)
            {
                writer.Write((string) key);
            }
            else if (key is int)
            {
                writer.Write((int) key);
            }
            else if (key is short)
            {
                writer.Write((short) key);
            }
            else if (key is long)
            {
                writer.Write((long) key);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        object ReadKey(BinaryReader reader)
        {
            Type keyType = typeof (TKey);

            if (keyType == typeof (string))
            {
                return reader.ReadString();
            }
            if (keyType == typeof (int))
            {
                return reader.ReadInt32();
            }
            if (keyType == typeof (short))
            {
                return reader.ReadInt16();
            }
            if (keyType == typeof (long))
            {
                return reader.ReadInt64();
            }

            throw new NotImplementedException();
        }
    }
}