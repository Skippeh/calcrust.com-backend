using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebAPI.Models
{
    public enum MetaType
    {
        None,
        Consumable,
        Oven,
        Wearable,
        Bed,
        Cookable,
        Weapon,
        Burnable
    }

    public class ItemMeta
    {
        public ItemMeta(MetaType type, IEnumerable<string> descriptions = null)
        {
            Type = type;

            if (descriptions != null)
            {
                Descriptions = descriptions.ToList();
            }
        }

        public List<string> Descriptions { get; set; } = new List<string>();

        [JsonIgnore]
        public MetaType Type { get; set; }

        [JsonProperty("type")]
        private string strType => Type.ToCamelCaseString();
    }
}