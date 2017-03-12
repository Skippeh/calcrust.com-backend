using System;
using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public sealed class Meta : IBinarySerializable
    {
        public DateTime Time { get; set; }

        void IBinarySerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Time.Ticks);
        }

        void IBinarySerializable.Deserialize(BinaryReader reader)
        {
            Time = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
        }
    }
}