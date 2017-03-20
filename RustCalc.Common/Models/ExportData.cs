using System.Collections.Generic;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Models
{
    public class ExportData : Dictionary<string, IBinarySerializable>
    {
        /// <summary>The current ExportData object being exported, serialized, or deserialized. Only safe to use in IExporter.ExportData, IBinarySerializable.Serialize, and IBinarySerializable.Deserialize.</summary>
        public static ExportData Current { get; private set; }

        public SerializableList<Item> Items => Get<SerializableList<Item>>("items");
        public SerializableList<Recipe> Recipes => Get<SerializableList<Recipe>>("recipes");
        public Meta Meta => Get<Meta>("meta");
        public SerializableDictionary<string, Destructible> Destructibles => Get<SerializableDictionary<string, Destructible>>("destructibles");
        public SerializableDictionary<int, RecycleOutput> Recycler => Get<SerializableDictionary<int, RecycleOutput>>("recycler");

        internal static void SetCurrent(ExportData data)
        {
            Current = data;
        }

        private T Get<T>(string key) where T : IBinarySerializable
        {
            if (ContainsKey(key))
                return (T) this[key];

            return default(T);
        }
    }
}