using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class Recipe : IBinarySerializable
    {
        public SerializableList<ItemAmount> Input { get; set; }
        public ItemAmount Output { get; set; }
        public float TimeToCraft;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Input);
            writer.Write(Output);
            writer.Write(TimeToCraft);
        }

        public void Deserialize(BinaryReader reader)
        {
            Input = reader.Deserialize<SerializableList<ItemAmount>>();
            Output = reader.Deserialize<ItemAmount>();
            TimeToCraft = reader.ReadSingle();
        }
    }
}