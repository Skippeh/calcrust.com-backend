using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebAPI.Models.Destructibles
{
    public abstract class Destructible
    {
        public enum DestructibleType
        {
            BuildingBlock,
            Deployable
        }

        [JsonIgnore]   public DestructibleType Type { get; private set; }
        [JsonProperty] private string type => Type.ToCamelCaseString();

        protected Destructible(DestructibleType type)
        {
            Type = type;
        }
    }

    public sealed class DeployableDestructible : Destructible
    {
        public Dictionary<Item, AttackInfo> Values;

        public DeployableDestructible() : base(DestructibleType.Deployable) { }
    }

    public sealed class BuildingBlockDestructible : Destructible
    {
        public Dictionary<string, Dictionary<Item, AttackInfo>> Grades = new Dictionary<string, Dictionary<Item, AttackInfo>>();

        public BuildingBlockDestructible() : base(DestructibleType.BuildingBlock) { }
    }
}