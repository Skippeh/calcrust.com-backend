using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Classes.Destructibles
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
        [JsonProperty("values")]
        public Dictionary<string, AttackInfo> Values = new Dictionary<string, AttackInfo>();

        public DeployableDestructible() : base(DestructibleType.Deployable) { }
    }

    public sealed class BuildingBlockDestructible : Destructible
    {
        [JsonProperty("grades")]
        public Dictionary<string, Dictionary<string, AttackInfo>> Grades = new Dictionary<string, Dictionary<string, AttackInfo>>();

        public BuildingBlockDestructible() : base(DestructibleType.BuildingBlock) { }
    }
}