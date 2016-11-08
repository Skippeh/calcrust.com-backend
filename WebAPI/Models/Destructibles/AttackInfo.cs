using System.Collections.Generic;
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
        [JsonProperty("ammunitions")]
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