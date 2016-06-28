using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebAPI.Models
{
    public class Cookable
    {
        [JsonIgnore]
        public Recipe.Item Output { get; set; }

        public int TTC { get; set; }

        [JsonIgnore]
        public List<Item> UsableOvens { get; set; } = new List<Item>();

        public override string ToString()
        {
            return $"[Cookable] {Output.Result.Name} ({Output.Count}x)";
        }
    }
}