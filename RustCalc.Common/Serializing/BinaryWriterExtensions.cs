using System;
using System.IO;
using System.Reflection;

namespace RustCalc.Common.Serializing
{
    public static class BinaryWriterExtensions
    {
        public static void WriteNullable(this BinaryWriter writer, object value)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            if (value == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                
                var methodInfo = typeof(BinaryWriter).GetMethod("Write", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] {value.GetType()}, null);

                if (methodInfo == null)
                {
                    throw new ArgumentException("BinaryWriter.Write does not have an overload for value type.");
                }

                methodInfo.Invoke(writer, new[] {value});
            }
        }

        public static string ReadNullableString(this BinaryReader reader)
        {
            if (reader.ReadBoolean())
                return reader.ReadString();

            return null;
        }

        public static void Serialize(this BinaryWriter writer, IBinarySerializable serializable)
        {
            serializable.Serialize(writer);
        }

        public static T Deserialize<T>(this BinaryReader reader) where T : IBinarySerializable
        {
            return (T) Deserialize(reader, typeof (T));
        }

        public static IBinarySerializable Deserialize(this BinaryReader reader, Type type)
        {
            var instance = (IBinarySerializable)Activator.CreateInstance(type, true);
            instance.Deserialize(reader);
            return instance;
        }
    }
}