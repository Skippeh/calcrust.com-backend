using System.Collections.Generic;

namespace DiscordBot.Rust.Models
{
    public class Destructible
    {
        public enum DestructibleType
        {
            BuildingBlock,
            Deployable
        }

        public string Id;
        public string Name;
        public DestructibleType Type;
        public bool HasProtection;
    }
}