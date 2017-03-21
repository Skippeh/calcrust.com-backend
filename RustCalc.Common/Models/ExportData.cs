using System.Collections.Generic;
using System.IO;
using System.Linq;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class ExportData : IBinarySerializable
    {
        /// <summary>The current ExportData object being exported, serialized, or deserialized. Only safe to use in IExporter.ExportData, IBinarySerializable.Serialize, and IBinarySerializable.Deserialize.</summary>
        public static ExportData Current { get; private set; }

        public Dictionary<int, Item> Items { get; set; }
        public Dictionary<Item, Recipe> Recipes { get; set; }
        public Meta Meta { get; set; }
        public Dictionary<string, Destructible> Destructibles { get; set; }
        public Dictionary<Item, RecycleOutput> Recycler { get; set; }

        internal static void SetCurrent(ExportData data)
        {
            Current = data;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Items.ToSerializableList(false, kv => kv.Value));
            writer.Write(Recipes.ToSerializableDictionary(false, kv => kv.Key.ItemId, kv => kv.Value));
            writer.Write(Meta);
            writer.Write(Destructibles.ToSerializableDictionary(true));
            writer.Write(Recycler.ToSerializableDictionary(false, kv => kv.Key.ItemId, kv => kv.Value));
        }

        public void Deserialize(BinaryReader reader)
        {
            Items = reader.Deserialize<SerializableList<Item>>().ToDictionary(item => item.ItemId, item => item);
            Recipes = reader.Deserialize<SerializableDictionary<int, Recipe>>().ToDictionary(kv => Items[kv.Key], kv => kv.Value);
            Meta = reader.Deserialize<Meta>();
            Destructibles = reader.Deserialize<SerializableDictionary<string, Destructible>>().ToDictionary();
            Recycler = reader.Deserialize<SerializableDictionary<int, RecycleOutput>>().ToDictionary(kv => Items[kv.Key], kv => kv.Value);
        }
    }
}