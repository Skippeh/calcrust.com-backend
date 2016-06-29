using System.Collections.Generic;

namespace WebAPI.Models
{
    public class DamageInfo
    {
        public class WeaponInfo
        {
            public float DPS { get; set; }
            public float TotalHits { get; set; }
        }

        public Dictionary<string, WeaponInfo> Damages { get; set; } 
    }
}