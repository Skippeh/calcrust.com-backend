using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Rust.Models
{
    public sealed class Item : IEquatable<Item>
    {
        [JsonProperty("shortname")] public string Id;
        public string Name;
        public string Description;
        public int MaxStack;
        public string Category;
        public JObject Meta; // Todo: Parse meta
        public string[] Descriptions;
        
        public bool Equals(Item other)
        {
            return other != null && Id == other.Id;
        }
    }
}