using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class HitValues : IBinarySerializable
    {
        public class Values : IBinarySerializable
        {
            public float DPS;
            public float TotalHits;
            public float TotalItems;

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(DPS);
                writer.Write(TotalHits);
                writer.Write(TotalItems);
            }

            public void Deserialize(BinaryReader reader)
            {
                DPS = reader.ReadSingle();
                TotalHits = reader.ReadSingle();
                TotalItems = reader.ReadSingle();
            }
        }

        public Values Strong { get; set; } = new Values();
        public Values Weak { get; set; } = new Values();

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Strong);
            writer.Write(Weak);
        }

        public void Deserialize(BinaryReader reader)
        {
            Strong = reader.Deserialize<Values>();
            Weak = reader.Deserialize<Values>();
        }
    }
}