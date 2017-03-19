using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public abstract class Destructible : IBinarySerializable
    {
        public bool HasWeakspot { get; set; }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(HasWeakspot);
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            HasWeakspot = reader.ReadBoolean();
        }
    }
}