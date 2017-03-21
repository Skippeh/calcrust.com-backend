using System.IO;
using System.Linq;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class ItemAmount : IBinarySerializable
    {
        public Item Item { get; set; }
        public float Amount { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Item.ItemId);
            writer.Write(Amount);
        }

        public void Deserialize(BinaryReader reader)
        {
            int itemId = reader.ReadInt32();

            Item = ExportData.Current.Items[itemId];
            Amount = reader.ReadSingle();
        }
    }
}