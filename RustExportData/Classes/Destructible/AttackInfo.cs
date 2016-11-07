using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Classes.Destructible
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
        [JsonProperty("values")]
        public HitValues Values;

        protected SingleAttackInfo(AttackType type) : base(type) { }
    }

    #region Implementations
    public class WeaponAttackInfo : AttackInfo
    {
        [JsonProperty("ammunitions")]
        public Dictionary<string, HitValues> Ammunitions = new Dictionary<string, HitValues>();

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