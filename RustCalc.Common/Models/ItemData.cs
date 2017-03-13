using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public abstract class ItemData : IBinarySerializable
    {
        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);
    }
}