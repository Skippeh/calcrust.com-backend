using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebAPI.Models.MetaModels
{
    public sealed class Burnable : ItemMeta
    {
        public float FuelAmount { get; set; }
        public int ByproductAmount { get; set; }
        public float ByproductChance { get; set; }

        [JsonIgnore]
        public Item ByproductItem { get; set; }

        [JsonProperty("byproductItem")]
        private string strByproductItem => ByproductItem?.Shortname;

        public Burnable(IEnumerable<string> descriptions) : base(MetaType.Burnable)
        {
            Descriptions = descriptions.ToList();
        }
    }
}