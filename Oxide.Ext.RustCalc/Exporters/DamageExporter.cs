using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rust;
using RustCalc.Common;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Models.AttackInfoImplementations;
using RustCalc.Common.Models.DestructibleImplementations;
using RustCalc.Common.Serializing;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace RustCalc.Exporters
{
    [Exporter(typeof (ItemsExporter))]
    public class DamageExporter : IExporter
    {
        private List<GameObject> destroyablePrefabObjects;
        private Dictionary<GameObject, ItemDefinition> destroyableItemDefinitions;
        private List<ItemDefinition> ammoDefinitions;
        private Dictionary<ItemDefinition, BaseEntity> attackEntities;

        private ExportData exportData;

        public string ID => "Destructibles";

        public object ExportData(ExportData data)
        {
            exportData = data;

            var result = new Dictionary<string, Destructible>();
            var prefabs = new List<string>();

            foreach (var str in GameManifest.Current.pooledStrings)
            {
                if (!str.str.StartsWith("assets/")) continue;
                if (!str.str.StartsWith("assets/prefabs/building")) continue;
                if (Utility.EndsWithBlacklist.Any(prefab => str.str.EndsWith(prefab)))
                    continue;

                prefabs.Add(str.str);
            }

            destroyablePrefabObjects = prefabs.Select(name => GameManager.server.FindPrefab(name)).ToList();
            destroyableItemDefinitions = new Dictionary<GameObject, ItemDefinition>();
            ammoDefinitions = new List<ItemDefinition>();

            // Add deployables to prefabObjects and ammo to ammoDefinitions.
            foreach (var item in ItemManager.itemList)
            {
                if (Utility.ItemExcludeList.Contains(item.shortname))
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

            attackEntities = new Dictionary<ItemDefinition, BaseEntity>();

            foreach (ItemDefinition item in ItemManager.itemList)
            {
                if (Utility.ItemExcludeList.Contains(item.shortname))
                    continue;

                var entityMod = item.GetComponent<ItemModEntity>();

                if (entityMod != null)
                {
                    var entityPrefab = entityMod.entityPrefab.Get();

                    if (entityPrefab == null)
                    {
                        Trace.TraceWarning("ItemModEntity prefab null, ignoring damage info: " + item.shortname);
                        continue;
                    }

                    var thrownWeapon = entityPrefab.GetComponent<ThrownWeapon>();
                    var attackEntity = entityPrefab.GetComponent<AttackEntity>();

                    if (thrownWeapon != null)
                    {
                        var toThrow = thrownWeapon.prefabToThrow.Get();
                        var explosive = toThrow.GetComponent<TimedExplosive>();

                        if (explosive != null)
                        {
                            // Don't include harmless items
                            if (explosive.damageTypes.Sum(t => t.amount) <= 0)
                                continue;

                            attackEntities.Add(item, explosive);
                        }
                        else
                        {
                            Trace.TraceError("Unhandled prefabToThrow: " + toThrow.name);
                        }
                    }
                    else if (attackEntity != null)
                    {
                        // Don't include harmless items
                        if (attackEntity is Hammer || attackEntity is BaseMelee && ((BaseMelee) attackEntity).TotalDamage() <= 0)
                            continue;

                        if (attackEntity is BaseMelee || attackEntity is BaseProjectile)
                            attackEntities.Add(item, attackEntity);
                    }
                }
            }

            foreach (GameObject prefab in destroyablePrefabObjects)
            {
                if (prefab == null)
                {
                    Trace.TraceError("Prefab null");
                    continue;
                }

                var baseCombatEntity = prefab.GetComponent<BaseCombatEntity>();

                if (baseCombatEntity != null)
                {
                    string objectName = destroyableItemDefinitions.ContainsKey(prefab) ? destroyableItemDefinitions[prefab].shortname : prefab.name;
                    Destructible damageInfo = GetDamageInfo(baseCombatEntity);

                    result.Add(objectName, damageInfo);
                }
            }

            return result;
        }

        private Destructible GetDamageInfo(BaseCombatEntity entity)
        {
            Destructible result;

            var instance = GameObject.Instantiate(entity.gameObject);
            var baseCombatEntity = instance.GetComponent<BaseCombatEntity>();

            try
            {
                baseCombatEntity.Spawn();

                if (baseCombatEntity is BuildingBlock)
                {
                    var buildingBlock = (BuildingBlock) baseCombatEntity;
                    var destructible = new BuildingBlockDestructible();
                    result = destructible;

                    foreach (BuildingGrade.Enum grade in Enum.GetValues(typeof (BuildingGrade.Enum)))
                    {
                        if (grade == BuildingGrade.Enum.None || grade == BuildingGrade.Enum.Count)
                            continue;

                        buildingBlock.SetGrade(grade);
                        buildingBlock.SetHealthToMax();

                        destructible.Grades.Add((Common.Models.BuildingGrade) grade, GetDamagesForCombatEntity(baseCombatEntity));
                        destructible.GradesHealth.Add((Common.Models.BuildingGrade) grade, buildingBlock.MaxHealth());
                    }
                }
                else
                {
                    var destructible = new DeployableDestructible();
                    result = destructible;

                    destructible.Values = GetDamagesForCombatEntity(baseCombatEntity);
                    destructible.Health = baseCombatEntity.MaxHealth();
                }
            }
            finally
            {
                if (!baseCombatEntity.IsDestroyed)
                {
                    baseCombatEntity.DestroyShared();
                    baseCombatEntity.Kill();
                }
            }
            
            result.HasWeakspot = baseCombatEntity.GetField<DirectionProperties[], BaseCombatEntity>("propDirection").Any(protection => protection.bounds.size != Vector3.zero);
            return result;
        }

        private Dictionary<Common.Models.Item, AttackInfo> GetDamagesForCombatEntity(BaseCombatEntity baseCombatEntity)
        {
            var result = new Dictionary<Common.Models.Item, AttackInfo>();
            
            foreach (var kv in attackEntities)
            {
                ItemDefinition item = kv.Key;
                var modelItem = exportData.Items[item.itemid];
                BaseEntity attackEntity = kv.Value;
                AttackInfo attackInfo = null;

                if (attackEntity is AttackEntity)
                {
                    if (attackEntity is BaseMelee)
                    {
                        var meleeInfo = new MeleeAttackInfo();
                        attackInfo = meleeInfo;

                        meleeInfo.Values = GetHitValues(baseCombatEntity, 1, item, null, null, (BaseMelee) attackEntity);
                    }
                    else if (attackEntity is BaseProjectile)
                    {
                        var weaponInfo = new WeaponAttackInfo();
                        attackInfo = weaponInfo;

                        AmmoTypes ammoFlags = ((BaseProjectile)attackEntity).primaryMagazine.definition.ammoTypes;
                        ItemDefinition[] ammoTypes = GetAmmoDefinitions(ammoFlags);

                        foreach (var ammoType in ammoTypes)
                        {
                            weaponInfo.Ammunitions.Add(exportData.Items[ammoType.itemid], GetHitValues(baseCombatEntity, ((BaseProjectile)attackEntity).damageScale, item, ammoType.GetComponent<ItemModProjectile>(), null, null));
                        }
                    }
                }
                else if (attackEntity is TimedExplosive)
                {
                    var explosiveInfo = new ExplosiveAttackInfo();
                    attackInfo = explosiveInfo;

                    explosiveInfo.Values = GetHitValues(baseCombatEntity, 1, item, null, (TimedExplosive) attackEntity, null);
                }

                if (attackInfo == null)
                {
                    Trace.TraceError("Unexpected item: " + item.shortname);
                    continue;
                }

                result.Add(modelItem, attackInfo);
            }

            return result;
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

        /// <summary>NOTE: Only one of modProjectile, explosive, and baseMelee should be assigned!</summary>
        private HitValues GetHitValues(BaseCombatEntity entity, float damageScale, ItemDefinition itemDefinition, ItemModProjectile modProjectile, TimedExplosive explosive, BaseMelee baseMelee)
        {
            var result = new HitValues();

            var propDirections = entity.GetField<DirectionProperties[], BaseCombatEntity>("propDirection");
            List<DamageTypeEntry> damageTypes;
            bool usesPropProtection = false;

            if (modProjectile != null)
            {
                GameObject projectileObject = modProjectile.projectileObject.Get();
                Projectile projectile = projectileObject.GetComponent<Projectile>();
                TimedExplosive rocket = projectileObject.GetComponent<TimedExplosive>();

                if (projectile != null)
                {
                    damageTypes = projectile.damageTypes;
                    usesPropProtection = true;

                    if (projectile.conditionLoss > 0)
                    {
                        Trace.TraceWarning("Projectile.conditionLoss > 0: " + projectile.conditionLoss);
                    }
                }
                else
                    damageTypes = rocket.damageTypes;
            }
            else if (explosive != null)
            {
                damageTypes = explosive.damageTypes;
            }
            else if (baseMelee != null)
            {
                damageTypes = baseMelee.damageTypes;
                usesPropProtection = true;
            }
            else
            {
                throw new NotImplementedException();
            }

            var normalHit = new HitInfo();
            normalHit.damageTypes.Add(damageTypes);
            normalHit.damageTypes.ScaleAll(damageScale);

            var protectedHit = new HitInfo();
            protectedHit.damageTypes.Add(damageTypes);
            protectedHit.damageTypes.ScaleAll(damageScale);

            if (modProjectile != null && modProjectile.numProjectiles > 1)
            {
                normalHit.damageTypes.ScaleAll(modProjectile.numProjectiles);
                protectedHit.damageTypes.ScaleAll(modProjectile.numProjectiles);
            }

            if (entity.baseProtection != null)
            {
                entity.baseProtection.Scale(normalHit.damageTypes);
                entity.baseProtection.Scale(protectedHit.damageTypes);
            }

            if (usesPropProtection)
            {
                if (propDirections.Length > 1)
                {
                    Trace.TraceWarning(entity.name + ": propDirections.Length > 1, invalid strong hit scaling! " + propDirections.Length);
                }

                foreach (var propDirection in propDirections)
                {
                    propDirection.extraProtection.Scale(protectedHit.damageTypes);

                    if (propDirection.bounds.size == Vector3.zero) // Item doesn't have a weakspot
                    {
                        propDirection.extraProtection.Scale(normalHit.damageTypes);
                    }
                }
            }

            FillHitValues(result, normalHit, protectedHit, entity, itemDefinition, baseMelee, explosive, modProjectile);

            return result;
        }

        private void FillHitValues(HitValues result, HitInfo normalHit, HitInfo protectedHit, BaseCombatEntity entity, ItemDefinition itemDefinition, BaseMelee baseMelee, TimedExplosive explosive, ItemModProjectile modProjectile)
        {
            result.Weak.DPS = normalHit.damageTypes.Total();
            result.Strong.DPS = protectedHit.damageTypes.Total();

            result.Weak.TotalHits = result.Weak.DPS > 0 ? entity.health / result.Weak.DPS : -1;
            result.Strong.TotalHits = result.Strong.DPS > 0 ? entity.health / result.Strong.DPS : -1;

            float maxCondition = itemDefinition.condition.max;

            // Calculate condition loss
            if (baseMelee != null)
            {
                float conditionLoss = baseMelee.GetConditionLoss();
                float num = 0;

                foreach (var damageType in baseMelee.damageTypes)
                {
                    num += Mathf.Clamp(damageType.amount - protectedHit.damageTypes.Get(damageType.type), 0, damageType.amount);
                }

                conditionLoss = conditionLoss + num * 0.2f;

                float hitsFromOne = maxCondition / conditionLoss;
                float numStrongItems = result.Strong.TotalHits / hitsFromOne;
                float numWeakItems = result.Weak.TotalHits / hitsFromOne;

                result.Strong.TotalItems = result.Strong.TotalHits >= 0 ? numStrongItems : -1;
                result.Weak.TotalItems = result.Weak.TotalHits >= 0 ? numWeakItems : -1;
            }
            else if (explosive != null)
            {
                result.Strong.TotalHits = Mathf.Ceil(result.Strong.TotalHits);
                result.Weak.TotalHits = Mathf.Ceil(result.Weak.TotalHits);
            }
            else if (modProjectile != null)
            {
                var projectileObject = modProjectile.projectileObject.Get();
                var projectile = projectileObject.GetComponent<Projectile>();
                float conditionLoss;

                if (projectile != null)
                {
                    // Source: BaseProjectile.UpdateItemCondition
                    conditionLoss = (float) (0.5 * (1.0 / 3.0)); // As accurate as it gets. The actual code uses Random.Range(0, 2) != 0 to not apply 0.5 condition loss.
                }
                else // Explosives/Rockets/etc, shot from weapon
                {
                    conditionLoss = 4.5f; // The actual code applies Random.Range(4f, 5f) condition loss.
                }

                float hitsFromOne = maxCondition / conditionLoss;
                float numStrongItems = result.Strong.TotalHits / hitsFromOne;
                float numWeakItems = result.Weak.TotalHits / hitsFromOne;

                result.Strong.TotalItems = result.Strong.TotalHits >= 0 ? numStrongItems : -1;
                result.Weak.TotalItems = result.Weak.TotalHits >= 0 ? numWeakItems : -1;
            }
        }
    }
}