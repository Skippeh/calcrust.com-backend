using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using RustExportData;
using System.Linq;
using System.Reflection;
using Oxide.Classes;
using Oxide.Classes.Destructible;
using Oxide.Core;
using Rust;
using UnityEngine;
using Component = UnityEngine.Component;
using Debug = UnityEngine.Debug;
using Time = UnityEngine.Time;
using Utility = Oxide.Classes.Utility;

namespace Oxide.Plugins
{
    public class RustExportData : RustPlugin
    {
        // debug
        [ChatCommand("look")]
        void ChatCmd_Look(BasePlayer player)
        {
            var lookObject = PhysicsUtility.GetLookTarget(player);

            if (lookObject == null)
            {
                player.ChatMessage("Nothing found.");
                return;
            }

            string layerName = LayerMask.LayerToName(lookObject.layer);
            player.ChatMessage("[" + lookObject.tag + "] " + layerName + ": " + lookObject.name + " (" + String.Join(", ", (lookObject.transform.GetComponents<Component>().Select(comp => comp.GetType().Name)).ToArray()) + ")");

            if (lookObject.transform.parent != null)
            {
                player.ChatMessage("Parent:");
                var parent = lookObject.transform.parent.gameObject;
                string parentLayerName = LayerMask.LayerToName(parent.layer);
                player.ChatMessage("[" + parent.tag + "] " + layerName + ": " + parent.name + " (" + String.Join(", ", (parent.transform.GetComponents<Component>().Select(comp => comp.GetType().Name)).ToArray()) + ")");
            }
        }

        private static readonly string[] excludeList =
        {
            "ammo.rocket.smoke", // WIP Smoke Rocket
            "generator.wind.scrap", // Wind Turbine
        };

        private static readonly Dictionary<string, float> ovenTemperatures = new Dictionary<string, float>();

        private List<GameObject> destroyablePrefabObjects;
        private Dictionary<GameObject, ItemDefinition> destroyableItemDefinitions;
        private List<ItemDefinition> ammoDefinitions; 
        
        void OnServerInitialized()
        {
            TOD_Sky.Instance.Cycle.Hour = 12;
            
            if (Config["UploadUrl"] == null)
                Debug.LogError("[RustExportData] Config UploadUrl not defined.");

            if (Config["UploadPassword"] == null)
                Debug.LogError("[RustExportData] Config UploadPassword not defined.");
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["UploadPassword"] = "CHANGEME";
            Config["UploadUrl"] = "https://CHANGEME/upload";
            SaveConfig();
        }
        
        [ConsoleCommand("calcrust.upload")]
        void ConsoleCmd_Upload(ConsoleSystem.Arg arg)
        {
            if (arg.FromClient)
                return;
            
            string data = JsonConvert.SerializeObject(ParseData());
            data = Utility.EncodeDataUri(data);
            webrequest.EnqueuePost((string) Config["UploadUrl"], "data=" + data, (statusCode, result) =>
            {
                Debug.Log("Response: " + new {statusCode, result});
            }, this, new Dictionary<string, string> {{"pw", (string) Config["UploadPassword"]}});
        }

        [ConsoleCommand("calcrust.export")]
        void ConsoleCmd_Export(ConsoleSystem.Arg arg)
        {
            if (arg.FromClient)
                return;

            var data = ParseData();
            Interface.Oxide.DataFileSystem.WriteObject("RustExportData", data);
            Debug.Log("Exported to [server_identity]/oxide/data/RustExportData.json.");
        }

        [ConsoleCommand("calcrust.uploadurl")]
        void ConsoleCmd_UploadUrl(ConsoleSystem.Arg arg)
        {
            if (arg.FromClient)
                return;

            string url = arg.GetString(0, null);
            if (url == null)
            {
                Debug.Log(Config["UploadUrl"]);
            }
            else
            {
                try
                {
                    var uri = new Uri(url);
                    Debug.Log(uri);
                    Config["UploadUrl"] = url;
                }
                catch (UriFormatException ex)
                {
                    Debug.Log("Invalid url.");
                }
            }
        }

        [ConsoleCommand("calcrust.uploadpass")]
        void ConsoleCmd_UploadPassword(ConsoleSystem.Arg arg)
        {
            if (arg.FromClient)
                return;

            string password = arg.GetString(0, null);
            if (password == null)
            {
                Debug.Log(Config["UploadPassword"]);
            }
            else
            {
                Config["UploadPassword"] = password;
            }
        }

        private RustData ParseData()
        {
            var startTime = Time.realtimeSinceStartup;
            var data = new RustData();

            // Parse damage data
            {
                var prefabs = new List<string>();

                string[] endsWithBlacklist =
                {
                    ".twig.prefab",
                    ".wood.prefab",
                    ".stone.prefab",
                    ".metal.prefab",
                    ".toptier.prefab",
                    ".item.prefab",
                    ".close-end.prefab",
                    "open-end.prefab",
                    "close-start.prefab",
                    "open-start.prefab",
                    "close-end.asset",
                    "open-end.asset",
                    "close-start.asset",
                    "open-start.asset",
                    "impact.prefab",
                    "knock.prefab",
                    "ladder_prop.prefab",
                };

                foreach (var str in GameManifest.Get().pooledStrings)
                {
                    if (!str.str.StartsWith("assets/")) continue;
                    if (!str.str.StartsWith("assets/prefabs/building")) continue;
                    if (endsWithBlacklist.Any(prefab => str.str.EndsWith(prefab)))
                        continue;
                    
                    prefabs.Add(str.str);
                }
                
                destroyablePrefabObjects = prefabs.Select(name => GameManager.server.FindPrefab(name)).ToList();
                destroyableItemDefinitions = new Dictionary<GameObject, ItemDefinition>();
                ammoDefinitions = new List<ItemDefinition>();

                // Add deployables to prefabObjects and ammo to ammoDefinitions.
                foreach (var item in ItemManager.itemList)
                {
                    if (excludeList.Contains(item.shortname))
                        continue;

                    if (item.GetComponent<ItemModProjectile>() != null)
                    {
                        ammoDefinitions.Add(item);
                    }

                    if (item.GetComponent<ItemModDeployable>() != null)
                    {
                        var gameObject = item.GetComponent<ItemModDeployable>().entityPrefab.Get();
                        var combatEntity = gameObject.GetComponent<BaseCombatEntity>();

                        if (combatEntity == null)
                            continue;

                        if (destroyablePrefabObjects.Contains(gameObject))
                            continue;

                        destroyablePrefabObjects.Add(gameObject);
                        destroyableItemDefinitions.Add(gameObject, item);
                    }
                }

                foreach (var prefab in destroyablePrefabObjects)
                {
                    if (prefab == null)
                    {
                        Debug.LogError("Prefab null");
                        continue;
                    }

                    var baseCombatEntity = prefab.GetComponent<BaseCombatEntity>();

                    if (baseCombatEntity != null)
                    {
                        string objectName = destroyableItemDefinitions.ContainsKey(prefab) ? destroyableItemDefinitions[prefab].shortname : prefab.name;
                        Destructible damageInfo = GetDamageInfo(baseCombatEntity);

                        data.DamageInfo.Add(objectName, damageInfo);
                    }
                }
            }

            Debug.Log("Damage data parsed");

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
                        ovenTemperatures.Add(item.shortname, GetProperty<float, BaseOven>(oven, "cookingTemperature"));
                        //Debug.Log(item.shortname + ": " + oven.temperature + " - " + ovenTemperatures[item.shortname]);
                    }
                }
            }

            Debug.Log("Got oven information...");

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

            Debug.Log("Cooking and smelting data parsed");

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
                var burnable = item.GetComponent<ItemModBurnable>();

                if (consumable != null)
                {
                    newItem.Meta.Add(MetaType.Consumable.ToCamelCaseString(), new MetaConsumable(item)
                    {
                        Effects = consumable.effects,
                        ItemsGivenOnConsume = consume?.product
                    });
                }

                if (deployable != null)
                {
                    var deployPrefab = deployable.entityPrefab.Get();
                    var oven = deployPrefab.GetComponent<BaseOven>();
                    var bed = deployPrefab.GetComponent<SleepingBag>();

                    if (oven != null)
                    {
                        newItem.Meta.Add(MetaType.Oven.ToCamelCaseString(), new MetaOven(item)
                        {
                            Oven = oven,
                            FuelType = oven.fuelType,
                            Slots = oven.inventorySlots,
                            AllowByproductCreation = oven.allowByproductCreation,
                            Temperature = GetProperty<float, BaseOven>(oven, "cookingTemperature")
                        });
                    }

                    if (bed != null)
                    {
                        newItem.Meta.Add(MetaType.Bed.ToCamelCaseString(), new MetaBed(item)
                        {
                            Bed = bed
                        });
                    }
                }

                if (wearable != null)
                {
                    if (wearable.HasProtections())
                    {
                        var protections = wearable.protectionProperties;

                        newItem.Meta.Add(MetaType.Wearable.ToCamelCaseString(), new MetaWearable(item)
                        {
                            Protections = protections
                        });
                    }
                }

                if (cookable != null)
                {
                    newItem.Meta.Add(MetaType.Cookable.ToCamelCaseString(), new MetaCookable(item)
                    {
                        Cookable = cookable
                    });
                }

                if (entity != null)
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
                            newItem.Meta.Add(MetaType.Weapon.ToCamelCaseString(), new MetaWeapon(item)
                            {
                                TimedExplosive = explosive
                            });
                        }
                    }
                    else if (baseProjectile != null)
                    {
                        var primaryAmmo = baseProjectile.primaryMagazine.ammoType;
                        var projectileMod = primaryAmmo.GetComponent<ItemModProjectile>();

                        var projectilePrefab = projectileMod.projectileObject.Get();
                        var projectile = projectilePrefab.GetComponent<Projectile>();

                        newItem.Meta.Add(MetaType.Weapon.ToCamelCaseString(), new MetaWeapon(item)
                        {
                            ProjectileMod = projectileMod,
                            BaseProjectile = baseProjectile,
                            Projectile = projectile
                        });
                    }
                }

                if (burnable != null)
                {
                    newItem.Meta.Add(MetaType.Burnable.ToCamelCaseString(), new MetaBurnable(item)
                    {
                        ByproductItem = burnable.byproductItem,
                        ByproductAmount = burnable.byproductAmount,
                        ByproductChance = (float) (1d - burnable.byproductChance),
                        FuelAmount = burnable.fuelAmount
                    });
                }

                data.Items.Add(item.shortname, newItem);
            }

            Debug.Log("Items parsed");

            foreach (ItemBlueprint blueprint in ItemManager.bpList)
            {
                if (excludeList.Contains(blueprint.targetItem.shortname))
                    continue;

                var newRecipe = new ExportRecipe();

                newRecipe.TTC = (int) blueprint.time;
                newRecipe.Parent = blueprint.targetItem.Parent?.shortname;
                
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
                
                data.Recipes.Add(blueprint.targetItem.shortname, newRecipe);
            }
            
            Debug.Log("Blueprints parsed");

            // Add all meta descriptions to item descriptions.
            foreach (var item in data.Items.Values)
            {
                item.Descriptions.AddRange(item.Meta.Values.SelectMany(meta => meta.Descriptions));
            }

            var endTime = Time.realtimeSinceStartup;
            var totalTime = endTime - startTime;
            data.Meta.Time = totalTime;
            
            return data;
        }

        private Destructible GetDamageInfo(BaseCombatEntity entity)
        {
            var instance = GameObject.Instantiate(entity.gameObject);
            var baseCombatEntity = instance.GetComponent<BaseCombatEntity>();
            baseCombatEntity.Spawn();

            Destructible result;

            try
            {
                var attackEntities = new Dictionary<ItemDefinition, BaseEntity>();

                foreach (ItemDefinition item in ItemManager.itemList)
                {
                    if (excludeList.Contains(item.shortname))
                        continue;

                    var entityMod = item.GetComponent<ItemModEntity>();

                    if (entityMod != null)
                    {
                        var entityPrefab = entityMod.entityPrefab.Get();
                        var thrownWeapon = entityPrefab.GetComponent<ThrownWeapon>();
                        var attackEntity = entityPrefab.GetComponent<AttackEntity>();

                        if (!(attackEntity is BaseProjectile) && !(attackEntity is BaseMelee))
                            continue;

                        if (thrownWeapon != null)
                        {
                            var toThrow = thrownWeapon.prefabToThrow.Get();
                            var explosive = toThrow.GetComponent<TimedExplosive>();

                            if (explosive != null)
                            {
                                attackEntities.Add(item, explosive);
                            }
                        }
                        else if (attackEntity != null)
                        {
                            attackEntities.Add(item, attackEntity);
                        }
                    }
                }

                if (baseCombatEntity is BuildingBlock)
                {
                    var destructible = (BuildingBlockDestructible) (result = new BuildingBlockDestructible());
                    var buildingBlock = (BuildingBlock) baseCombatEntity;

                    foreach (BuildingGrade.Enum grade in Enum.GetValues(typeof (BuildingGrade.Enum)))
                    {
                        if (grade == BuildingGrade.Enum.None || grade == BuildingGrade.Enum.Count)
                            continue;

                        var attackInfos = new Dictionary<string, AttackInfo>();

                        foreach (var keyval in attackEntities)
                        {
                            ItemDefinition item = keyval.Key;
                            BaseEntity attackEntity = keyval.Value;
                            AttackInfo attackInfo = null;
                            buildingBlock.SetGrade(grade);
                            buildingBlock.SetHealthToMax();

                            if (attackEntity is AttackEntity)
                            {
                                if (attackEntity is BaseMelee)
                                {
                                    attackInfo = new MeleeAttackInfo();
                                }
                                else if (attackEntity is BaseProjectile)
                                {
                                    var weaponInfo = (WeaponAttackInfo) (attackInfo = new WeaponAttackInfo());
                                    AmmoTypes ammoFlags = ((BaseProjectile) attackEntity).primaryMagazine.definition.ammoTypes;
                                    ItemDefinition[] ammoTypes = GetAmmoDefinitions(ammoFlags);

                                    foreach (var ammoType in ammoTypes)
                                    {
                                        weaponInfo.Ammunitions.Add(ammoType.shortname, GetHitValues(buildingBlock, ((BaseProjectile)attackEntity).damageScale, ammoType.GetComponent<ItemModProjectile>(), null));
                                    }
                                }
                            }
                            else if (attackEntity is TimedExplosive)
                            {
                                attackInfo = new ExplosiveAttackInfo();
                            }

                            if (attackInfo == null)
                            {
                                Debug.LogError("Unexpected item: " + item.name);
                                continue;
                            }

                            attackInfos.Add(item.shortname, attackInfo);
                        }

                        destructible.Grades.Add(grade.ToCamelCaseString(), attackInfos);
                    }
                }
                else // BaseCombatEntity
                {
                    var destructible = (DeployableDestructible) (result = new DeployableDestructible());

                    // It's probably possible to use the same code for calculating BaseCombatEntity and separate BuildingGrades.
                    // Use method normally with BaseCombatEntity.
                    //
                    // Apply new BuildingGrades and reset hp etc for each grade and use method on each grade and add to result grades.
                }
            }
            finally
            {
                if (!baseCombatEntity.isDestroyed)
                {
                    baseCombatEntity.DestroyShared();
                    baseCombatEntity.Kill();
                }
            }

            return result;
        }

        /// <summary>NOTE: Either modProjectile OR explosive should be assigned, not both!</summary>
        private HitValues GetHitValues(BaseCombatEntity entity, float damageScale, ItemModProjectile modProjectile, TimedExplosive explosive)
        {
            var hitValues = new HitValues();
            var propDirections = GetField<DirectionProperties[], BaseCombatEntity>(entity, "propDirection");
            List<DamageTypeEntry> damageTypes;

            if (modProjectile != null)
            {
                GameObject projectileObject = modProjectile.projectileObject.Get();
                Projectile projectile  = projectileObject.GetComponent<Projectile>();
                TimedExplosive rocket = projectileObject.GetComponent<TimedExplosive>();
                
                if (projectile != null)
                    damageTypes = projectile.damageTypes;
                else
                    damageTypes = rocket.damageTypes;
            }
            else
            {
                damageTypes = explosive.damageTypes;
            }
            
            var weakHit = new HitInfo();
            weakHit.damageTypes.Add(damageTypes);
            weakHit.damageTypes.ScaleAll(damageScale);
            entity.ScaleDamage(weakHit);

            var strongHit = new HitInfo();
            strongHit.damageTypes.Add(damageTypes);
            strongHit.damageTypes.ScaleAll(damageScale);
            entity.ScaleDamage(strongHit);

            foreach (var propDirection in propDirections)
            {
                propDirection.extraProtection.Scale(strongHit.damageTypes);
            }

            hitValues.WeakDPS = weakHit.damageTypes.Total();
            hitValues.StrongDPS = strongHit.damageTypes.Total();
            hitValues.TotalWeakHits = entity.health / hitValues.WeakDPS;
            hitValues.TotalStrongHits = entity.health / hitValues.StrongDPS;

            return hitValues;
        }

        private ItemDefinition[] GetAmmoDefinitions(AmmoTypes ammoTypes)
        {
            List<ItemDefinition> result = new List<ItemDefinition>();

            foreach (var ammoDefinition in ammoDefinitions)
            {
                var projectile = ammoDefinition.GetComponent<ItemModProjectile>();

                if ((projectile.ammoType & ammoTypes) > 0)
                {
                    result.Add(ammoDefinition);
                }
            }

            return result.ToArray();
        }

        /*private Dictionary<string, DamageInfo> GetDamageInfos(GameObject prefab, string itemName)
        {
            if (itemName == null)
                itemName = prefab.name;

            var instance = (GameObject)GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            var baseEntity = instance.GetComponent<BaseEntity>();
            baseEntity.Spawn();

            var result = new Dictionary<string, DamageInfo>();

            try
            {
                foreach (var item in ItemManager.itemList)
                {
                    if (excludeList.Contains(item.shortname))
                        continue;

                    var projectileMod = item.GetComponent<ItemModProjectile>();
                    var entityMod = item.GetComponent<ItemModEntity>();
                    List<DamageTypeEntry> damageTypes = null;
                    Projectile projectile = null;
                    float damageScale = 1f;

                    if (projectileMod != null)
                    {
                        var projectileObject = projectileMod.projectileObject.Get();

                        if (projectileObject == null)
                            continue;

                        var explosive = projectileObject.GetComponent<TimedExplosive>();
                        projectile = projectileObject.GetComponent<Projectile>();

                        if (explosive != null)
                        {
                            damageTypes = new List<DamageTypeEntry>(explosive.damageTypes);
                        }
                        else if (projectile != null && (/*item.category == ItemCategory.Ammunition ||*//* item.category == ItemCategory.Tool))
                        {
                            damageTypes = projectile.damageTypes;
                        }
                    }
                    else if (entityMod != null)
                    {
                        var entityPrefab = entityMod.entityPrefab.Get();
                        var attackEntity = entityPrefab.GetComponent<AttackEntity>();
                        var thrownWeapon = attackEntity as ThrownWeapon;

                        if (thrownWeapon != null)
                        {
                            var toThrow = thrownWeapon.prefabToThrow.Get();
                            var timedExplosive = toThrow.GetComponent<TimedExplosive>();

                            if (timedExplosive != null)
                            {
                                damageTypes = new List<DamageTypeEntry>(timedExplosive.damageTypes);
                            }
                        }
                    }

                    if (damageTypes == null)
                        continue;

                    DamageInfo damageInfo = new DamageInfo();
                    result.Add(item.shortname, damageInfo);

                    if (baseEntity is BuildingBlock)
                    {
                        var buildingBlock = (BuildingBlock) baseEntity;
                        var weaponInfos = new DamageInfo.BuildingBlockInfo();

                        foreach (BuildingGrade.Enum grade in Enum.GetValues(typeof (BuildingGrade.Enum)))
                        {
                            if (grade == BuildingGrade.Enum.None || grade == BuildingGrade.Enum.Count)
                                continue;

                            buildingBlock.SetGrade(grade);
                            buildingBlock.SetHealthToMax();

                            var strongHitInfo = new HitInfo();
                            strongHitInfo.damageTypes.Add(damageTypes);
                            strongHitInfo.damageTypes.ScaleAll(damageScale);
                            buildingBlock.ScaleDamage(strongHitInfo);
                            DirectionProperties[] propDirections = GetField<DirectionProperties[], BaseCombatEntity>(buildingBlock, "propDirection");

                            foreach (var propDirection in propDirections)
                            {
                                propDirection.extraProtection.Scale(strongHitInfo.damageTypes);
                            }

                            var weakHitInfo = new HitInfo();
                            weakHitInfo.damageTypes.Add(damageTypes);
                            weakHitInfo.damageTypes.ScaleAll(damageScale);
                            buildingBlock.ScaleDamage(weakHitInfo);

                            float totalStrongDamage = strongHitInfo.damageTypes.Total();
                            float strongHits = totalStrongDamage > 0 ? (buildingBlock.MaxHealth() / totalStrongDamage) : -1;

                            float totalWeakDamage = weakHitInfo.damageTypes.Total();
                            float weakHits = totalWeakDamage > 0 ? (buildingBlock.MaxHealth() / totalWeakDamage) : -1;

                            var attackInfo = new DamageInfo.SingleWeaponContainer();
                            attackInfo.Values.Add(grade.ToCamelCaseString(), new DamageInfo.SingleWeaponContainer
                            {
                                
                            });

                            weaponInfos.StrongValues.Add(grade.ToCamelCaseString(), new DamageInfo.AttackInfo
                            {
                                DPS = totalStrongDamage,
                                TotalHits = strongHits
                            });

                            weaponInfos.WeakValues.Add(grade.ToCamelCaseString(), new DamageInfo.AttackInfo
                            {
                                DPS = totalWeakDamage,
                                TotalHits = weakHits
                            });
                        }

                        damageInfo.Damages.Add(itemName, weaponInfos);
                    }
                    else if (baseEntity is BaseCombatEntity)
                    {
                        var combatEntity = (BaseCombatEntity) baseEntity;

                        var strongHitInfo = new HitInfo();
                        strongHitInfo.damageTypes.Add(damageTypes);
                        combatEntity.ScaleDamage(strongHitInfo);

                        float totalDamage = strongHitInfo.damageTypes.Total();
                        float hits = totalDamage > 0 ? (combatEntity.MaxHealth() / totalDamage) : -1;

                        var weaponInfos = new DamageInfo.ItemInfo(false);

                        weaponInfos.StrongValues.Add("default", new DamageInfo.AttackInfo
                        {
                            DPS = totalDamage,
                            TotalHits = hits
                        });

                        // Deployables don't have a strong/weak sides.
                        weaponInfos.WeakValues = new Dictionary<string, DamageInfo.AttackInfo>(weaponInfos.StrongValues);

                        damageInfo.Damages.Add(itemName, weaponInfos);
                    }
                }
            }
            finally
            {
                if (!baseEntity.isDestroyed)
                {
                    if (baseEntity is BaseCombatEntity)
                    {
                        ((BaseCombatEntity)baseEntity).DestroyShared();
                    }

                    baseEntity.Kill();
                }
            }

            return result;
        }*/

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            //Debug.Log("OnPlayerAttack: " + info.Weapon.LookupPrefabName());
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            //if (entity.name.Contains("/animals/"))
            //    return;
            //
            //var total = info.damageTypes.Total();
            //
            //if (info.HitEntity != null)
            //{
            //    var buildingBlock = info.HitEntity.GetComponent<global::BuildingBlock>();
            //
            //    if (buildingBlock != null)
            //    {
            //
            //    }
            //}
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

        public static T GetField<T, TClassType>(object obj, string memberName)
        {
            if (obj == null)
                return default(T);

            var targetType = typeof(TClassType);
            var type = obj.GetType();

            while (type != targetType)
            {
                if (!type.IsSubclassOf(targetType) || type.BaseType == null)
                    throw new ArgumentException(obj.GetType().FullName + " does not equal or inherit type " + targetType.FullName + ".");

                type = type.BaseType;
            }

            var fieldInfo = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (fieldInfo == null)
                return default(T);

            return (T) fieldInfo.GetValue(obj);
        }

        public static T GetProperty<T, TClassType>(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var targetType = typeof (TClassType);
            var type = obj.GetType();
            
            while (type != targetType)
            {
                if (!type.IsSubclassOf(targetType) || type.BaseType == null)
                    throw new ArgumentException(obj.GetType().FullName + " does not equal or inherit type " + targetType.FullName + ".");

                type = type.BaseType;
            }

            var propInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (propInfo == null)
                throw new NullReferenceException("No property found with name " + propertyName + " in type " + type.FullName + ".");

            return (T)propInfo.GetValue(obj, new object[0]);
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
        public Dictionary<string, Destructible> DamageInfo = new Dictionary<string, Destructible>();

        [JsonProperty("cookables")]
        public Dictionary<string, ExportCookable> CookableInfo = new Dictionary<string, ExportCookable>();

        [JsonProperty("meta")]
        public Meta Meta;

        public RustData()
        {
            Meta = new Meta(DateTime.UtcNow, Protocol.printable);
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

    internal class Meta
    {
        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate;

        [JsonProperty("time")]
        public float Time;

        [JsonProperty("version")]
        public string Version;

        public Meta(DateTime lastUpdate, string version)
        {
            LastUpdate = lastUpdate;
            Version = version;
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

        [JsonProperty("level")]
        public int Level;

        [JsonProperty("price")]
        public int Price;

        [JsonProperty("parent")]
        public string Parent { get; set; }
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

        [JsonProperty("numMeta")]
        public int NumMeta => Meta.Count;

        [JsonProperty("meta")]
        public Dictionary<string, ItemMeta> Meta { get; set; } = new Dictionary<string, ItemMeta>();

        [JsonProperty("descriptions")]
        public List<string> Descriptions { get; set; } = new List<string>();
    }

    internal enum MetaType
    {
        None,
        Consumable,
        Oven,
        Wearable,
        Bed,
        Cookable,
        Weapon,
        Burnable
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal abstract class ItemMeta
    {
        [JsonIgnore]
        public abstract string[] Descriptions { get; }
        
        public ItemDefinition Item { get; private set; }

        public MetaType Type { get; private set; }

        [JsonProperty("type")]
        private string strType => Type.ToCamelCaseString();

        protected ItemMeta(ItemDefinition itemDef, MetaType type)
        {
            if (itemDef == null)
                throw new ArgumentNullException(nameof(itemDef));

            Item = itemDef;
            Type = type;
        }
    }

    internal class MetaConsumable : ItemMeta
    {
        public ItemAmountRandom[] ItemsGivenOnConsume;
        public List<ItemModConsumable.ConsumableEffect> Effects;

        [JsonIgnore]
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

        public MetaConsumable(ItemDefinition itemDef) : base(itemDef, MetaType.Consumable)
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

                if (Oven.fuelType != null)
                {
                    float seconds = 0;
                    float fuelAmount = Oven.fuelType.GetComponent<ItemModBurnable>().fuelAmount;
                    float temperature = Oxide.Plugins.RustExportData.GetProperty<float, BaseOven>(Oven, "cookingTemperature");
                    seconds = 1f / ((temperature / 200f) / fuelAmount);
                    
                    descs.Add("Uses 1 " + Oven.fuelType.displayName.english + " every " + seconds + " second" + (seconds != 1d ? "s" : "") + ".");
                }

                return descs.ToArray();
            }
        }

        public ItemDefinition FuelType;

        [JsonProperty("slots")]
        public int Slots;

        [JsonProperty("allowByproductCreation")]
        public bool AllowByproductCreation { get; set; }

        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("fuelType")]
        private string strFuelType => FuelType?.shortname;

        public MetaOven(ItemDefinition itemDef) : base(itemDef, MetaType.Oven)
        {
        }
    }

    internal class MetaWearable : ItemMeta
    {
        public ProtectionProperties Protections;

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

        public MetaWearable(ItemDefinition itemDef) : base(itemDef, MetaType.Wearable)
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

                descs.Add(Bed.secondsBetweenReuses + " second cooldown between respawns. Triggers cooldown on all beds within " + ConVar.Server.respawnresetrange + "m.");

                return descs.ToArray();
            }
        }

        public MetaBed(ItemDefinition itemDef) : base(itemDef, MetaType.Bed)
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

        public MetaCookable(ItemDefinition itemDef) : base(itemDef, MetaType.Cookable)
        {
        }
    }

    internal class MetaWeapon : ItemMeta
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
                
                if (TimedExplosive != null)
                {
                    var hitInfo = new HitInfo();
                    hitInfo.damageTypes = new DamageTypeList();

                    TimedExplosive.damageTypes.ForEach(t => hitInfo.damageTypes.Add(t.type, t.amount));
                    damageTypes = hitInfo.damageTypes;
                }
                else if (Projectile != null && ProjectileMod != null)
                {
                    // Scale damage with projectile mod
                    var hitInfo = new HitInfo();
                    Projectile.CalculateDamage(hitInfo, Projectile.Modifier.Default, BaseProjectile.damageScale);
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

                        //if (amount > 0)
                        //    descs.Add("Inflicts " + Math.Round(amount) + " " + damageType.ToString().ToLower() + " base damage per " + projectileType);
                    }
                }

                return descs.ToArray();
            }
        }

        public MetaWeapon(ItemDefinition itemDef) : base(itemDef, MetaType.Weapon)
        {
        }
    }

    internal class MetaBurnable : ItemMeta
    {
        [JsonProperty("fuelAmount")]
        public float FuelAmount;

        [JsonProperty("byproductAmount")]
        public int ByproductAmount;

        [JsonProperty("byproductChance")]
        public float ByproductChance;

        public ItemDefinition ByproductItem;

        [JsonProperty("byproductItem")]
        private string strByproductItem => ByproductItem?.shortname;

        public MetaBurnable(ItemDefinition itemDef) : base(itemDef, MetaType.Burnable)
        {
        }

        public override string[] Descriptions => new string[0];
    }
}