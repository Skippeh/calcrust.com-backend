using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebAPI.Models.Destructibles
{
    public abstract class AttackInfo
    {
        public enum AttackType
        {
            Invalid,
            Weapon,
            Melee,
            Explosive
        }

        [JsonIgnore]
        public AttackType Type { get; private set; }
        [JsonProperty]
        private string type => Type.ToCamelCaseString();

        protected AttackInfo(AttackType type)
        {
            Type = type;
        }
    }

    public abstract class SingleAttackInfo : AttackInfo
    {
        public HitValues Values;

        protected SingleAttackInfo(AttackType type) : base(type) { }
    }

    #region Implementations
    public class WeaponAttackInfo : AttackInfo
    {
        // Convert ammunition key from item to item shortname.
        class AmmunitionSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, ((Dictionary<Item, HitValues>) value).ToDictionary(kv => kv.Key.Shortname, kv => kv.Value));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof (Dictionary<Item, HitValues>);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        [JsonProperty("ammunitions")]
        [JsonConverter(typeof(AmmunitionSerializer))]
        public Dictionary<Item, HitValues> Ammunitions = new Dictionary<Item, HitValues>();

        public WeaponAttackInfo() : base(AttackType.Weapon) { }
    }

    public class MeleeAttackInfo : SingleAttackInfo
    {
        public MeleeAttackInfo() : base(AttackType.Melee) { }
    }

    public class ExplosiveAttackInfo : SingleAttackInfo
    {
        public ExplosiveAttackInfo() : base(AttackType.Explosive) { }
    }
    #endregion
}