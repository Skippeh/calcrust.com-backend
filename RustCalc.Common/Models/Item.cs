using System;
using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public sealed class Item : IBinarySerializable
    {
        public string Name { get; set; }
        public string Shortname { get; set; }
        public ItemCategory Category { get; set; }
        public int StackSize { get; set; }
        public int ItemId { get; set; }
        public SerializableDictionary<string, ItemData> Data { get; set; }

        public Item()
        {
            Data = new SerializableDictionary<string, ItemData>();
        }

        void IBinarySerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteNullable(Name);
            writer.WriteNullable(Shortname);
            writer.Write((int)Category);
            writer.Write(StackSize);
            writer.Write(ItemId);

            Data.OnSerializeItem = (itemData, writer2) => writer2.Write(itemData.GetType().FullName);
            writer.Write(Data);
        }

        void IBinarySerializable.Deserialize(BinaryReader reader)
        {
            Name = reader.ReadNullableString();
            Shortname = reader.ReadNullableString();
            Category = (ItemCategory) reader.ReadInt32();
            StackSize = reader.ReadInt32();
            ItemId = reader.ReadInt32();

            Data.OnDeserializeItem = (type, reader2) => (ItemData)Activator.CreateInstance(Type.GetType(reader2.ReadString()), true);
            Data = reader.Deserialize<SerializableDictionary<string, ItemData>>();
        }
    }
}