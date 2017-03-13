using System;
using System.Collections.Generic;
using System.IO;

namespace RustCalc.Common.Serializing
{
    public class SerializableList<T> : List<T>, IBinarySerializable where T : IBinarySerializable
    {
        /// <summary>If set to false, the item type names will not be written and on deserialization it will be assumed that all items are of the same type as <typeparamref name="T"/>.</summary>
        public bool HasDerivativeTypes { get; set; }

        public SerializableList(bool hasDerivativeTypes = false)
        {
            HasDerivativeTypes = hasDerivativeTypes;
        }

        // For deserialization
        private SerializableList()
        {
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(HasDerivativeTypes);
            writer.Write(Count);
            foreach (T val in this)
            {
                if (HasDerivativeTypes)
                    writer.Write(val.GetType().FullName);

                writer.Write(val);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            HasDerivativeTypes = reader.ReadBoolean();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                Type type;

                if (HasDerivativeTypes)
                {
                    string typeName = reader.ReadString();
                    type = Type.GetType(typeName);
                    if (type == null) throw new ArgumentNullException(nameof(type));
                }
                else
                {
                    type = typeof (T);
                }

                var instance = (T)reader.Deserialize(typeof (T));
                Add(instance);
            }
        }
    }
}