using System.Collections.Generic;
using System.IO;
using System.Linq;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models.AttackInfoImplementations
{
    public class WeaponAttackInfo : AttackInfo
    {
        public Dictionary<Item, HitValues> Ammunitions { get; set; }

        public WeaponAttackInfo()
        {
            Ammunitions = new Dictionary<Item, HitValues>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Ammunitions.ToSerializableDictionary(false, kv => kv.Key.ItemId, kv => kv.Value));
        }

        public override void Deserialize(BinaryReader reader)
        {
            Ammunitions = reader.Deserialize<SerializableDictionary<int, HitValues>>().ToDictionary(kv => ExportData.Current.Items[kv.Key], kv => kv.Value);
        }
    }
}