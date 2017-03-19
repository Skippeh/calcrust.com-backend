using System.IO;
using System.Linq;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models.DestructibleImplementations
{
    public class DeployableDestructible : Destructible
    {
        public SerializableDictionary<Item, AttackInfo> HitValues { get; set; }

        public DeployableDestructible()
        {
            HitValues = new SerializableDictionary<Item, AttackInfo>(true);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(HitValues.ToSerializableDictionary(kv => kv.Key.ItemId, kv => kv.Value));
        }

        public override void Deserialize(BinaryReader reader)
        {
            HitValues = reader.Deserialize<SerializableDictionary<int, AttackInfo>>().ToSerializableDictionary(kv => ExportData.Current.Items.First(item => item.ItemId == kv.Key), kv => kv.Value);
        }
    }
}