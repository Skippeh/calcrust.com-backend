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
        public ItemMeta(MetaType type)
        {
            Type = type;
        }
        
        [JsonIgnore]
        public MetaType Type { get; set; }

        [JsonProperty("type")]
        private string strType => Type.ToCamelCaseString();
    }
}