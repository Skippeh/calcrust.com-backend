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

        void IBinarySerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteNullable(Name);
            writer.WriteNullable(Shortname);
            writer.Write((int)Category);
            writer.Write(StackSize);
            writer.Write(ItemId);
        }

        void IBinarySerializable.Deserialize(BinaryReader reader)
        {
            Name = reader.ReadNullableString();
            Shortname = reader.ReadNullableString();
            Category = (ItemCategory) reader.ReadInt32();
            StackSize = reader.ReadInt32();
            ItemId = reader.ReadInt32();
        }
    }
}