using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class Item : IBinarySerializer
    {
        public string Name { get; set; }
        public string Shortname { get; set; }
        
        public void Serialize(BinaryWriter writer)
        {
            writer.WriteNullable(Name);
            writer.WriteNullable(Shortname);
        }
        
        public void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadNullableString();
            Shortname = reader.ReadNullableString();
        }
    }
}