using System.IO;

namespace RustCalc.Common.Serializing
{
    public interface IBinarySerializer
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}