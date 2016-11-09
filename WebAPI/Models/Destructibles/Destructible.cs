using System;
using System.Collections.Generic;
using System.Linq;
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
        public DestructibleValues Values;

        public DeployableDestructible() : base(DestructibleType.Deployable) { }
    }

    public sealed class BuildingBlockDestructible : Destructible
    {
        public Dictionary<string, DestructibleValues> Grades = new Dictionary<string, DestructibleValues>();

        public BuildingBlockDestructible() : base(DestructibleType.BuildingBlock) { }
    }

    [JsonConverter(typeof(DestructibleValuesConverter))]
    public class DestructibleValues : Dictionary<Item, AttackInfo>
    {
        
    }

    public class DestructibleValuesConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((DestructibleValues) value).ToDictionary(kv => kv.Key.Shortname, kv => kv.Value));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (DestructibleValues);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }
    }
}