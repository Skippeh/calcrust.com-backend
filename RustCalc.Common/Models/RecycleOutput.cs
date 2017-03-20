using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class RecycleOutput : IBinarySerializable
    {
        public SerializableList<ItemAmount> Output { get; set; }

        public RecycleOutput()
        {
            Output = new SerializableList<ItemAmount>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Output);
        }

        public void Deserialize(BinaryReader reader)
        {
            Output = reader.Deserialize<SerializableList<ItemAmount>>();
        }
    }
}