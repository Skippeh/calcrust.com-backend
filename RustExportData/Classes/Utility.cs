using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Rust;

namespace Oxide.Classes
{
    class Utility
    {
        public const string API_URL = "https://api.calcrust.com/upload";
        public const string UPLOAD_PASSWORD = "REPLACE_ME";

        public static string DamageTypeToString(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Bite:
                    return "bites";
                case DamageType.Bleeding:
                    return "bleeding";
                case DamageType.Blunt:
                    return "blunt hits";
                case DamageType.Bullet:
                    return "bullets";
                case DamageType.Cold:
                    return "cold";
                case DamageType.ColdExposure:
                    return "cold exposure";
                case DamageType.Decay:
                    return "decaying";
                case DamageType.Drowned:
                    return "drowning";
                case DamageType.ElectricShock:
                    return "electric shock";
                case DamageType.Explosion:
                    return "explosions";
                case DamageType.Fall:
                    return "fall damage";
                case DamageType.Generic:
                    return "generic damage";
                case DamageType.Hunger:
                    return "hunger";
                case DamageType.Poison:
                    return "poison";
                case DamageType.Radiation:
                    return "radiation";
                case DamageType.RadiationExposure:
                    return "radiation exposure";
                case DamageType.Slash:
                    return "slash hits";
                case DamageType.Stab:
                    return "stab hits";
                case DamageType.Suicide:
                    return "suicides";
                case DamageType.Heat:
                    return "heat";
                case DamageType.Thirst:
                    return "thirst";
            }

            throw new NotImplementedException();
        }

        public static Dictionary<DamageType, float> MergeProtectionAmounts(ProtectionProperties[] protections)
        {
            var protectionAmounts = new Dictionary<DamageType, float>();

            foreach (var protection in protections)
            {
                for (int i = 0; i < protection.amounts.Length; ++i)
                {
                    var damageType = (DamageType)i;

                    if (protection.amounts[i] <= 0)
                        continue;

                    if (protectionAmounts.ContainsKey(damageType))
                        protectionAmounts[damageType] += protection.amounts[i];
                    else
                        protectionAmounts.Add(damageType, protection.amounts[i]);
                }
            }

            return protectionAmounts;
        }

        public static string EncodeDataUri(string str)
        {
            var builder = new StringBuilder();
            int maxLength = 32766;

            if (str.Length < maxLength)
                return str;

            int loops = (int) Math.Ceiling(str.Length / (double) maxLength);

            for (int i = 0; i < loops; ++i)
            {
                if (i >= loops - 1)
                {
                    builder.Append(Uri.EscapeDataString(str.Substring(maxLength * i)));
                }
                else
                {
                    builder.Append(Uri.EscapeDataString(str.Substring(maxLength * i, maxLength)));
                }
            }

            return builder.ToString();
        }
    }
}