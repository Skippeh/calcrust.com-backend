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
    }
}