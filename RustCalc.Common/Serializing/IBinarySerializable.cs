using System.IO;

namespace RustCalc.Common.Serializing
{
    public interface IBinarySerializable
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}