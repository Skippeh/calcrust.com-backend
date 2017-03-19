using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public abstract class AttackInfo : IBinarySerializable
    {
        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);
    }

    public abstract class SingleAttackInfo : AttackInfo
    {
        public HitValues Values { get; set; }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Values);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Values = reader.Deserialize<HitValues>();
        }
    }
}