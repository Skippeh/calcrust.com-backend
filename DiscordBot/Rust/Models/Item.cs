using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Rust.Models
{
    public sealed class Item
    {
        [JsonProperty("shortname")] public string Id;
        public string Name;
        public string Description;
        public int MaxStack;
        public string Category;
        public JObject Meta; // Todo: Parse meta
        public string[] Descriptions;
    }
}