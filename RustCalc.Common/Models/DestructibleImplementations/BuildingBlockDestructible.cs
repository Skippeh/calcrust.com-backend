using System;
using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models.DestructibleImplementations
{
    public class BuildingBlockDestructible : Destructible
    {
        public SerializableDictionary<BuildingGrade, SerializableDictionary<int, AttackInfo>> Grades { get; set; }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Grades.ToSerializableDictionary(kv => (int) kv.Key, kv => kv.Value));
        }

        public override void Deserialize(BinaryReader reader)
        {
            Grades = reader.Deserialize<SerializableDictionary<int, SerializableDictionary<int, AttackInfo>>>().ToSerializableDictionary(kv => (BuildingGrade) kv.Key, kv => kv.Value);
        }
    }
}