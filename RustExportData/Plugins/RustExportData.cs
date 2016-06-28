using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RustExportData;
using System.Linq;
using System.Reflection;
using ConVar;
using Oxide.Core.Libraries;
using Rust;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Time = UnityEngine.Time;
using Utility = Oxide.Classes.Utility;

namespace Oxide.Plugins
{
    public class RustExportData : RustPlugin
    {
        private static readonly string[] excludeList =
        {
            "ammo.rocket.smoke", // WIP Smoke Rocket
            "generator.wind.scrap", // Wind Turbine
            "salt.water", // Salt water
            "water", // Water
        };

        private static readonly Dictionary<string, float> ovenTemperatures = new Dictionary<string, float>();


        void OnServerInitialized()
        {
            TOD_Sky.Instance.Cycle.Hour = 12;
            var data = ParseData();
            
            LoadConfig();
            Config.Clear();
            Config["data_json"] = data;
            SaveConfig();
            Debug.Log("Exported items");
        }

        [ConsoleCommand("upload")]
        void ConsoleCmd_Upload(ConsoleSystem.Arg arg)
        {
            if (arg.FromClient)
                return;

            string data = JsonConvert.SerializeObject(ParseData());
            data = Utility.EncodeDataUri(data);
            webrequest.EnqueuePost(Utility.API_URL, "data=" + data, (statusCode, result) =>
            {
                Debug.Log(new {statusCode, result});
            }, this, new Dictionary<string, string> {{"pw", Utility.UPLOAD_PASSWORD}});
        }

        private RustData ParseData()
        {
            var startTime = Time.realtimeSinceStartup;

            var data = new RustData();

            var buildingsToTest =
                GameManager.server.FindPrefabNames("assets/prefabs/building core/")
                    .Where(s => !s.EndsWith(".twig.prefab") && !s.EndsWith(".wood.prefab") && !s.EndsWith(".stone.prefab") && !s.EndsWith(".metal.prefab") && !s.EndsWith(".toptier.prefab"))
                    .ToArray();
            
            //// Fix file paths because they don't match the actual prefab paths for some reason.
            buildingsToTest = buildingsToTest.Select(prefabPath =>
            {
                string filenameWithExtension = prefabPath.Substring(prefabPath.LastIndexOf("/") + 1);
                string fileName = filenameWithExtension.Substring(0, filenameWithExtension.LastIndexOf("."));
                string path = prefabPath.Substring(0, prefabPath.LastIndexOf("/"));
            
                return path + "/" + fileName + "/" + filenameWithExtension;
            }).ToArray();

            // Add building damage data
            foreach (var prefabPath in buildingsToTest)
            {
                var prefab = GameManager.server.FindPrefab(prefabPath);
            
                if (prefab == null)
                    continue;
            
                var instance = GameObject.Instantiate(prefab);
                var buildingBlock = instance.GetComponent<global::BuildingBlock>();
            
                if (buildingBlock != null)
                {
                    buildingBlock.Spawn(true);
                    var definition = buildingBlock.blockDefinition;
            
                    //Debug.Log(definition.info.name.english + ":");
            
                    var damageInfo = new DamageInfo
                    {
                        Type = "buildingBlock"
                    };
            
                    data.DamageInfo.Add(definition.info.name.english, damageInfo);
            
                    foreach (var grade in definition.grades)
                    {
                        buildingBlock.SetGrade(grade.gradeBase.type);
            
                        float maxHealth = grade.maxHealth;
                        string keyName = grade.gradeBase.type.ToString();
                        keyName = keyName.Substring(0, 1).ToLower() + keyName.Substring(1);
            
                        var scalesInfo = GetScalesInfo(maxHealth, buildingBlock);
                        damageInfo.Scales.Add(keyName, scalesInfo);
                    }
            
                    buildingBlock.Kill();
                }
                else
                {
                    Debug.LogError("buildingsToTest contains non BuildingBlock prefab!!");
                }
            }

            // Add combat entity damage data
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                if (item.GetComponent<ItemModDeployable>() != null)
                {
                    var deployableMod = item.GetComponent<ItemModDeployable>();
                    var deployablePrefab = deployableMod.entityPrefab.Get();
                    var baseCombatEntity = deployablePrefab.GetComponent<BaseCombatEntity>();
            
                    if (baseCombatEntity != null)
                    {
                        var damageInfo = new DamageInfo
                        {
                            Type = "deployable"
                        };
            
                        data.DamageInfo.Add(item.shortname, damageInfo);
            
                        float maxHealth = baseCombatEntity.startHealth;
            
                        var scalesInfo = GetScalesInfo(maxHealth, baseCombatEntity);
                        damageInfo.Scales.Add("default", scalesInfo);
                    }
                }
            }

            ovenTemperatures.Clear();

            // Find ovens and populate oven info
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                var deployableMod = item.GetComponent<ItemModDeployable>();

                if (deployableMod != null)
                {
                    var deployObject = deployableMod.entityPrefab.Get();
                    var oven = deployObject.GetComponent<BaseOven>();

                    if (oven != null)
                    {
                        ovenTemperatures.Add(item.shortname, GetProperty<float>(oven, "cookingTemperature"));
                        //Debug.Log(item.shortname + ": " + oven.temperature + " - " + ovenTemperatures[item.shortname]);
                    }
                }
            }

            // Add cooking and smelting data
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                var cookableMod = item.GetComponent<ItemModCookable>();

                if (cookableMod != null)
                {
                    var exportCookable = new ExportCookable();
                    data.CookableInfo.Add(item.shortname, exportCookable);

                    exportCookable.Output = new ExportRecipeItem
                    {
                        Count = cookableMod.amountOfBecome,
                        ItemId = cookableMod.becomeOnCooked.shortname
                    };

                    exportCookable.TTC = cookableMod.cookTime;
                    // burnable.fuel -= (float)(0.5 * ((double)this.cookingTemperature / 200.0));

                    foreach (var keyval in ovenTemperatures)
                    {
                        string shortname = keyval.Key;
                        float ovenTemp = keyval.Value;

                        if (ovenTemp >= cookableMod.lowTemp && ovenTemp <= cookableMod.highTemp)
                        {
                            var ovenDef = ItemManager.FindItemDefinition(shortname).GetComponent<ItemModDeployable>().entityPrefab.Get().GetComponent<BaseOven>();
                            var ovenFuel = ovenDef.fuelType.GetComponent<ItemModBurnable>();
                            float fuelConsumed = (float) (cookableMod.cookTime * (ovenTemp / 200.0) / ovenFuel.fuelAmount);
                            
                            exportCookable.UsableOvens.Add(new ExportCookable.Oven
                            {
                                Shortname = shortname,
                                FuelConsumed = fuelConsumed,
                                Fuel = ovenDef.fuelType.shortname
                            });
                        }
                    }
                }
            }

            foreach (ItemDefinition item in ItemManager.itemList)
            {
                if (excludeList.Contains(item.shortname))
                    continue;

                var newItem = new ExportItem();
                newItem.Name = item.displayName.english;
                newItem.Description = item.displayDescription.english;
                newItem.MaxStack = item.stackable;
                newItem.Category = item.category.ToString();
                newItem.Category = newItem.Category.Substring(0, 1).ToLower() + newItem.Category.Substring(1);

                // Set meta depending on item type
                var consumable = item.GetComponent<ItemModConsumable>();
                var consume = item.GetComponent<ItemModConsume>();
                var deployable = item.GetComponent<ItemModDeployable>();
                var wearable = item.GetComponent<ItemModWearable>();
                var cookable = item.GetComponent<ItemModCookable>();
                var entity = item.GetComponent<ItemModEntity>();

                if (consumable != null)
                {
                    newItem.Meta = new MetaConsumable(item)
                    {
                        Effects = consumable.effects,
                        ItemsGivenOnConsume = consume?.product
                    };
                }
                else if (deployable != null)
                {
                    var deployPrefab = deployable.entityPrefab.Get();
                    var oven = deployPrefab.GetComponent<BaseOven>();
                    var bed = deployPrefab.GetComponent<SleepingBag>();

                    if (oven != null)
                    {
                        newItem.Meta = new MetaOven(item)
                        {
                            Oven = oven
                        };
                    }
                    else if (bed != null)
                    {
                        newItem.Meta = new MetaBed(item)
                        {
                            Bed = bed
                        };
                    }
                }
                else if (wearable != null)
                {
                    if (wearable.HasProtections())
                    {
                        var protections = GetField<GameManifest.PrefabProperties>(wearable, "prefabProperties").protections;

                        newItem.Meta = new MetaWearable(item)
                        {
                            Protections = protections
                        };
                    }
                }
                else if (cookable != null)
                {
                    newItem.Meta = new MetaCookable(item)
                    {
                        Cookable = cookable
                    };
                }
                else if (entity != null)
                {
                    var prefab = entity.entityPrefab.Get();

                    var thrownWeapon = prefab.GetComponent<ThrownWeapon>();
                    var baseProjectile = prefab.GetComponent<global::BaseProjectile>();
                    
                    if (thrownWeapon != null)
                    {
                        var throwable = thrownWeapon.prefabToThrow.Get();
                        var explosive = throwable.GetComponent<TimedExplosive>();

                        if (explosive != null)
                        {
                            newItem.Meta = new MetaWeaponDamage(item)
                            {
                                TimedExplosive = explosive
                            };
                        }
                    }
                    else if (baseProjectile != null)
                    {
                        var primaryAmmo = baseProjectile.primaryMagazine.ammoType;
                        var projectileMod = primaryAmmo.GetComponent<ItemModProjectile>();

                        var projectilePrefab = projectileMod.projectileObject.Get();
                        var projectile = projectilePrefab.GetComponent<Projectile>();

                        newItem.Meta = new MetaWeaponDamage(item)
                        {
                            ProjectileMod = projectileMod,
                            BaseProjectile = baseProjectile,
                            Projectile = projectile
                        };
                    }
                }

                data.Items.Add(item.shortname, newItem);
            }

            foreach (ItemBlueprint blueprint in ItemManager.bpList)
            {
                if (excludeList.Contains(blueprint.targetItem.shortname))
                    continue;

                var newRecipe = new ExportRecipe();

                newRecipe.TTC = (int) blueprint.time;

                if (blueprint.defaultBlueprint)
                    newRecipe.Rarity = "default";
                else
                {
                    switch (blueprint.rarity)
                    {
                        case Rarity.None:
                            newRecipe.Rarity = "default";
                            break;
                        case Rarity.Common:
                            newRecipe.Rarity = "fragments";
                            break;
                        case Rarity.Uncommon:
                            newRecipe.Rarity = "page";
                            break;
                        case Rarity.Rare:
                            newRecipe.Rarity = "book";
                            break;
                        case Rarity.VeryRare:
                            newRecipe.Rarity = "library";
                            break;
                    }
                }

                foreach (ItemAmount ingredient in blueprint.ingredients)
                {
                    newRecipe.Input.Add(new ExportRecipeItem
                    {
                        Count = (int) ingredient.amount,
                        ItemId = ingredient.itemDef.shortname
                    });
                }

                newRecipe.Output = new ExportRecipeItem
                {
                    Count = blueprint.amountToCreate,
                    ItemId = blueprint.targetItem.shortname
                };

                newRecipe.Researchable = blueprint.isResearchable;

                data.Recipes.Add(blueprint.targetItem.shortname, newRecipe);
            }

            var endTime = Time.realtimeSinceStartup;
            var totalTime = endTime - startTime;
            data.Meta.Time = totalTime;
            
            return data;
        }

        private ScalesInfo GetScalesInfo(float maxHealth, BaseCombatEntity combatEntity)
        {
            var scalesInfo = new ScalesInfo();
            scalesInfo.Health = maxHealth;
            
            foreach (var item in ItemManager.itemList)
            {
                var hitInfo = new HitInfo();
                
                var entityMod = item.GetComponent<ItemModEntity>();
                var projectileMod = item.GetComponent<ItemModProjectile>();

                if (projectileMod != null)
                {
                    var projectileObject = projectileMod.projectileObject.Get();
                    var projectile = projectileObject.GetComponent<Projectile>();
                    var timedExplosive = projectileObject.GetComponent<TimedExplosive>();

                    if (timedExplosive != null)
                    {
                        hitInfo.damageTypes.Add(timedExplosive.damageTypes);
                    }
                    else if (projectile != null)
                    {
                        hitInfo.damageTypes.Add(projectile.damageTypes);
                    }
                }
                else if (entityMod != null)
                {
                    var entityPrefab = entityMod.entityPrefab.Get();
                    var attackEntity = entityPrefab.GetComponent<AttackEntity>();

                    if (attackEntity == null)
                    {
                        continue;
                    }

                    var asThrownWeapon = attackEntity as ThrownWeapon;
                    var asMelee = attackEntity as BaseMelee;

                    if (asThrownWeapon != null)
                    {
                        var thrownObject = asThrownWeapon.prefabToThrow.Get();
                        var timedExplosive = thrownObject.GetComponent<TimedExplosive>();

                        if (timedExplosive != null)
                        {
                            hitInfo.damageTypes.Add(timedExplosive.damageTypes);
                        }
                        else
                        {
                            Debug.LogError("Unhandled ThrownWeapon type!! (" + item.shortname + ")");
                            continue;
                        }
                    }
                    else if (asMelee != null)
                    {
                        if (asMelee is Hammer)
                            continue;

                        hitInfo.damageTypes.Add(asMelee.damageTypes);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                var weakHitInfo = new HitInfo();
                var strongHitInfo = new HitInfo();

                Array.Copy(hitInfo.damageTypes.types, weakHitInfo.damageTypes.types, hitInfo.damageTypes.types.Length);
                Array.Copy(hitInfo.damageTypes.types, strongHitInfo.damageTypes.types, hitInfo.damageTypes.types.Length);
                
                var propDirection = GetField<DirectionProperties[]>(combatEntity, "propDirection");

                if (propDirection != null)
                {
                    foreach (var properties in propDirection)
                    {
                        properties.extraProtection.Scale(strongHitInfo.damageTypes);
                    }
                }

                combatEntity.ScaleDamage(weakHitInfo);
                combatEntity.ScaleDamage(strongHitInfo);

                float weakDamage = weakHitInfo.damageTypes.Total();
                float strongDamage = strongHitInfo.damageTypes.Total();

                scalesInfo.Damages.Add(item.shortname, new StrongWeakDamageInfo
                {
                    WeakSide = new WeaponDamageInfo.Values
                    {
                        DamagePerHit = weakDamage,
                        TotalHits = weakDamage > 0 ? (float) (maxHealth / (double) weakDamage) : -1
                    },
                    StrongSide = new WeaponDamageInfo.Values
                    {
                        DamagePerHit = strongDamage,
                        TotalHits = strongDamage > 0 ? (float) (maxHealth / (double) strongDamage) : -1
                    }
                });
            }

            return scalesInfo;
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            //Debug.Log("OnPlayerAttack: " + info.Weapon.LookupPrefabName());
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.name.Contains("/animals/"))
                return;

            //Debug.Log(entity.name);

            WriteDamageTypes(info.damageTypes);

            var total = info.damageTypes.Total();
            //Debug.Log(total);

            if (info.HitEntity != null)
            {
                var buildingBlock = info.HitEntity.GetComponent<global::BuildingBlock>();

                if (buildingBlock != null)
                {

                }
            }
        }

        void OnWeaponFired(global::BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            //Debug.Log(projectile.name);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            string entityString = entity.ToString();

            if (entityString.Contains("/autospawn/") || entityString.Contains("/building core/"))
                return;

            //Debug.Log(entityString);
        }

        private void WriteComponents(MonoBehaviour behaviour) { WriteComponents(behaviour?.gameObject); }
        private void WriteComponents(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.Log("null []");
                return;
            }

            Debug.Log(gameObject.name + " [" + String.Join(", ", gameObject.GetComponents<MonoBehaviour>().Select(c => c.GetType().Name).ToArray()) + "]");
        }

        private void WriteDamageTypes(DamageTypeList damageInfo)
        {
            Dictionary<DamageType, float> damages = new Dictionary<DamageType, float>();
            for (int i = 0; i < (int)DamageType.LAST; ++i)
            {
                damages.Add((DamageType) i, damageInfo.types[i]);
            }

            Debug.Log(String.Join("\n", damages.Select(d => d.Key.ToString() + ": " + d.Value).ToArray()));
        }

        private void WriteDamageTypes(ProtectionProperties properties)
        {
            var damages = new Dictionary<DamageType, float>();

            for (int i = 0; i < (int) DamageType.LAST; ++i)
            {
                damages.Add((DamageType) i, properties.amounts[i]);
            }

            Debug.Log(String.Join("\n", damages.Select(d => d.Key.ToString() + ": " + d.Value).ToArray()));
        }

        private T GetField<T>(object obj, string memberName)
        {
            if (obj == null)
                return default(T);

            var type = obj.GetType();

            var fieldInfo = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (fieldInfo == null)
                return default(T);

            return (T) fieldInfo.GetValue(obj);
        }
        
        private T GetProperty<T>(object obj, string propertyName)
        {
            if (obj == null)
                return default(T);

            var type = obj.GetType();

            var propInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (propInfo == null)
                return default(T);

            return (T) propInfo.GetValue(obj, new object[0]);
        }
    }
}

namespace RustExportData
{
    internal class RustData
    {
        [JsonProperty("items")]
        public Dictionary<string, ExportItem> Items = new Dictionary<string, ExportItem>();

        [JsonProperty("recipes")]
        public Dictionary<string, ExportRecipe> Recipes = new Dictionary<string, ExportRecipe>();
        
        [JsonProperty("damageInfo")]
        public Dictionary<string, DamageInfo> DamageInfo = new Dictionary<string, DamageInfo>();

        [JsonProperty("cookables")]
        public Dictionary<string, ExportCookable> CookableInfo = new Dictionary<string, ExportCookable>();

        [JsonProperty("meta")]
        public Meta Meta;

        public RustData()
        {
            Meta = new Meta(DateTime.UtcNow);
        }
    }

    internal class ExportCookable
    {
        public class Oven
        {
            [JsonProperty("shortname")]
            public string Shortname;

            [JsonProperty("fuelConsumed")]
            public float FuelConsumed;

            [JsonProperty("fuel")]
            public string Fuel { get; set; } // The fuel type item's shortname.
        }

        [JsonProperty("usableOvens")]
        public List<Oven> UsableOvens = new List<Oven>();

        [JsonProperty("ttc")]
        public float TTC;

        [JsonProperty("output")]
        public ExportRecipeItem Output;
    }

    internal class DamageInfo
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("scales")]
        public Dictionary<string, ScalesInfo> Scales = new Dictionary<string, ScalesInfo>();
    }

    internal class ScalesInfo
    {
        [JsonProperty("health")]
        public float Health;
        
        [JsonProperty("damages")]
        public Dictionary<string, WeaponDamageInfo> Damages = new Dictionary<string, WeaponDamageInfo>();
    }

    internal abstract class WeaponDamageInfo
    {
        public class Values
        {
            [JsonProperty("totalHits")]
            public float TotalHits;

            [JsonProperty("damagePerHit")]
            public float DamagePerHit;
        }
    }

    internal class StrongWeakDamageInfo : WeaponDamageInfo
    {
        [JsonProperty("strongSide")]
        public Values StrongSide;

        [JsonProperty("weakSide")]
        public Values WeakSide;
    }

    internal class CombatEntityDamageInfo : WeaponDamageInfo
    {
        [JsonProperty("totalHits")]
        public float TotalHits;

        [JsonProperty("damagePerHit")]
        public float DamagePerHit;
    }

    internal class Meta
    {
        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate;

        [JsonProperty("time")]
        public float Time;

        public Meta(DateTime lastUpdate)
        {
            LastUpdate = lastUpdate;
        }
    }

    internal class ExportRecipe
    {
        [JsonProperty("input")]
        public List<ExportRecipeItem> Input = new List<ExportRecipeItem>();

        [JsonProperty("output")]
        public ExportRecipeItem Output;

        [JsonProperty("ttc")]
        public int TTC;

        [JsonProperty("rarity")]
        public string Rarity;

        [JsonProperty("researchable")]
        public bool Researchable;
    }

    internal class ExportRecipeItem
    {
        [JsonProperty("item")]
        public string ItemId;

        [JsonProperty("count")]
        public int Count;
    }

    internal class ExportItem
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("maxStack")]
        public int MaxStack;

        [JsonProperty("category")]
        public string Category;

        [JsonProperty("meta")]
        public ItemMeta Meta { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal abstract class ItemMeta
    {
        [JsonProperty("descriptions")]
        public abstract string[] Descriptions { get; }

        public ItemDefinition Item { get; private set; }

        protected ItemMeta(ItemDefinition itemDef)
        {
            if (itemDef == null)
                throw new ArgumentNullException(nameof(itemDef));

            Item = itemDef;
        }
    }

    internal class MetaConsumable : ItemMeta
    {
        public ItemAmountRandom[] ItemsGivenOnConsume;
        public List<ItemModConsumable.ConsumableEffect> Effects;

        public override string[] Descriptions
        {
            get
            {
                var descs = new List<string>();

                if (ItemsGivenOnConsume != null)
                {
                    foreach (var randomAmount in ItemsGivenOnConsume)
                    {
                        descs.Add("50% chance to give " + randomAmount.itemDef.displayName.english + ".");
                    }
                }

                foreach (var effect in Effects)
                {
                    var desc = "";
                    var type = effect.type;

                    if (effect.amount > 0)
                        desc += "Adds ";
                    else
                        desc += "Removes ";

                    desc += Math.Abs(effect.amount).ToString("0") + " ";
                    desc += (type == MetabolismAttribute.Type.HealthOverTime ? MetabolismAttribute.Type.Health : type).ToString().ToLower();

                    if (effect.type == MetabolismAttribute.Type.HealthOverTime)
                        desc += " over " + effect.time.ToString("0") + " seconds";

                    descs.Add(desc);
                }

                return descs.ToArray();
            }
        }

        public MetaConsumable(ItemDefinition itemDef) : base(itemDef)
        {
        }
    }

    internal class MetaOven : ItemMeta
    {
        public BaseOven Oven;

        public override string[] Descriptions
        {
            get
            {
                var descs = new List<string>();

                foreach (var item in Oven.startupContents)
                {
                    descs.Add("Spawns with " + item.amount + " " + item.itemDef.displayName.english);
                }

                descs.Add("Can be used for " + Oven.temperature.ToString().ToLower());

                return descs.ToArray();
            }
        }

        public MetaOven(ItemDefinition itemDef) : base(itemDef)
        {
        }
    }

    internal class MetaWearable : ItemMeta
    {
        public ProtectionProperties[] Protections;

        public override string[] Descriptions
        {
            get
            {
                var descs = new List<string>();

                var protectionAmounts = Utility.MergeProtectionAmounts(Protections);

                foreach (var keyval in protectionAmounts)
                    descs.Add(keyval.Value * 2f * 10f + " protection from " + Utility.DamageTypeToString(keyval.Key));

                return descs.ToArray();
            }
        }

        public MetaWearable(ItemDefinition itemDef) : base(itemDef)
        {
        }
    }

    internal class MetaBed : ItemMeta
    {
        public SleepingBag Bed { get; set; }

        public override string[] Descriptions
        {
            get
            {
                var descs = new List<string>();

                descs.Add(Bed.secondsBetweenReuses + " second cooldown between respawns. Triggers cooldown on all beds within " + Server.respawnresetrange + "m.");

                return descs.ToArray();
            }
        }

        public MetaBed(ItemDefinition itemDef) : base(itemDef)
        {
        }
    }

    internal class MetaCookable : ItemMeta
    {
        public ItemModCookable Cookable;

        public override string[] Descriptions
        {
            get
            {
                var descs = new List<string>();

                descs.Add("Can be cooked for " + Cookable.cookTime + " seconds to get x" + Cookable.amountOfBecome + " " + Cookable.becomeOnCooked.displayName.english);

                return descs.ToArray();
            }
        }

        public MetaCookable(ItemDefinition itemDef) : base(itemDef)
        {
        }
    }

    internal class MetaWeaponDamage : ItemMeta
    {
        public ItemModProjectile ProjectileMod;
        public global::BaseProjectile BaseProjectile;
        public Projectile Projectile;
        
        public TimedExplosive TimedExplosive;

        public override string[] Descriptions
        {
            get
            {
                var descs = new List<string>();

                DamageTypeList damageTypes = null;
                string projectileType = "explosion";
                
                if (TimedExplosive != null)
                {
                    var hitInfo = new HitInfo();
                    hitInfo.damageTypes = new DamageTypeList();

                    TimedExplosive.damageTypes.ForEach(t => hitInfo.damageTypes.Add(t.type, t.amount));
                    damageTypes = hitInfo.damageTypes;
                }
                else if (Projectile != null && ProjectileMod != null)
                {
                    switch (ProjectileMod.ammoType)
                    {
                        case AmmoTypes.BOW_ARROW:
                            projectileType = "arrow";
                            break;
                        case AmmoTypes.HANDMADE_SHELL:
                        case AmmoTypes.SHOTGUN_12GUAGE:
                            projectileType = "shell";
                            break;
                        case AmmoTypes.PISTOL_9MM:
                        case AmmoTypes.RIFLE_556MM:
                            projectileType = "bullet";
                            break;
                        case AmmoTypes.ROCKET:
                            projectileType = "rocket";
                            break;
                        default:
                            throw new NotImplementedException();
                            break;
                    }

                    // Scale damage with projectile mod
                    var hitInfo = new HitInfo();
                    Projectile.CalculateDamage(hitInfo, ProjectileMod, Projectile.fullDamageVelocity * BaseProjectile.projectileVelocityScale, BaseProjectile.damageScale);
                    damageTypes = hitInfo.damageTypes;

                    try
                    {
                        //Debug.Log(Item.shortname + ": " + BaseProjectile.projectileVelocityScale + ", " + BaseProjectile.damageScale);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                        throw ex;
                    }

                    if (ProjectileMod.numProjectiles > 1)
                        descs.Add("Fires " + ProjectileMod.numProjectiles + " projectiles per shot");

                    if (Projectile.breakProbability > 0)
                        descs.Add(Math.Round(Projectile.breakProbability * 100f) + "% to break ammo on impact");
                }

                if (damageTypes != null)
                {
                    for (int i = 0; i < damageTypes.types.Length; i++)
                    {
                        var damageType = (DamageType) i;
                        var amount = damageTypes.types[i];

                        if (amount > 0)
                            ;//descs.Add("Inflicts " + Math.Round(amount) + " " + damageType.ToString().ToLower() + " base damage per " + projectileType);
                    }
                }

                return descs.ToArray();
            }
        }

        public MetaWeaponDamage(ItemDefinition itemDef) : base(itemDef)
        {
        }
    }
}