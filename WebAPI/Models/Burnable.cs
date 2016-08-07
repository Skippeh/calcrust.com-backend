using Newtonsoft.Json;

namespace WebAPI.Models
{
    public class Burnable
    {
        public float FuelAmount { get; set; }
        public int ByproductAmount { get; set; }
        public float ByproductChance { get; set; }

        [JsonIgnore]
        public Item ByproductItem { get; set; }

        [JsonProperty("byproductItem")]
        private string strByproductItem => ByproductItem?.Shortname;
    }
}