using System.Collections.Generic;
using System.IO;
using System.Linq;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models.DestructibleImplementations
{
    public class DeployableDestructible : Destructible
    {
        public Dictionary<Item, AttackInfo> Values { get; set; }
        public float Health { get; set; }

        public DeployableDestructible()
        {
            Values = new Dictionary<Item, AttackInfo>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Values.ToSerializableDictionary(true, kv => kv.Key.ItemId, kv => kv.Value));
            writer.Write(Health);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Values = reader.Deserialize<SerializableDictionary<int, AttackInfo>>().ToDictionary(kv => ExportData.Current.Items.First(item => item.ItemId == kv.Key), kv => kv.Value);
            Health = reader.ReadSingle();
        }
    }
}