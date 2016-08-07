using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebAPI.Models.MetaModels
{
    public sealed class Oven : ItemMeta
    {
        [JsonIgnore]
        public Item FuelType { get; set; }

        public int Slots { get; set; }
        public bool AllowByproductCreation { get; set; }

        [JsonProperty("fuelType")]
        private string strFuelType => FuelType?.Shortname;

        public Oven(IEnumerable<string> descriptions) : base(MetaType.Oven)
        {
            Descriptions = descriptions.ToList();
        }
    }
}