using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebAPI.Models
{
    public class Cookable
    {
        public class Oven
        {
            public Item Item;
            public float FuelConsumed;
        }

        [JsonIgnore]
        public Recipe.Item Output { get; set; }

        public int TTC { get; set; }

        [JsonIgnore]
        public List<Oven> UsableOvens { get; set; } = new List<Oven>();

        public override string ToString()
        {
            return $"[Cookable] {Output.Result.Name} ({Output.Count}x)";
        }
    }
}