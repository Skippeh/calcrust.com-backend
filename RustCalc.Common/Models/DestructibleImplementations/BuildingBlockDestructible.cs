using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models.DestructibleImplementations
{
    public class BuildingBlockDestructible : Destructible
    {
        public Dictionary<BuildingGrade, Dictionary<Item, AttackInfo>> Grades { get; set; }
        public Dictionary<BuildingGrade, float> GradesHealth { get; set; } 

        public BuildingBlockDestructible()
        {
            Grades = new Dictionary<BuildingGrade, Dictionary<Item, AttackInfo>>();
            GradesHealth = new Dictionary<BuildingGrade, float>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Grades.ToSerializableDictionary(false, kv => (int) kv.Key, kv => kv.Value.ToSerializableDictionary(true, kv2 => kv2.Key.ItemId, kv2 => kv2.Value)));

            writer.Write(GradesHealth.Count);
            foreach (var kv in GradesHealth)
            {
                writer.Write((int) kv.Key);
                writer.Write(kv.Value);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Grades = reader.Deserialize<SerializableDictionary<int, SerializableDictionary<int, AttackInfo>>>()
                .ToDictionary(kv => (BuildingGrade) kv.Key, kv => kv.Value.ToDictionary(kv2 => ExportData.Current.Items[kv2.Key], kv2 => kv2.Value));

            GradesHealth = new Dictionary<BuildingGrade, float>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                GradesHealth.Add((BuildingGrade) reader.ReadInt32(), reader.ReadSingle());
            }
        }
    }
}