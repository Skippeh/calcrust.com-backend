using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;
using UnityEngine;

namespace RustCalc.Exporters
{
    [Exporter(typeof (ItemsExporter))]
    public class DamageExporter : IExporter
    {
        private List<GameObject> destroyablePrefabObjects;
        private Dictionary<GameObject, ItemDefinition> destroyableItemDefinitions;
        private List<ItemDefinition> ammoDefinitions;

        public string ID => "destructibles";

        public IBinarySerializable ExportData(ExportData data)
        {
            var result = new SerializableDictionary<string, Destructible>(true);
            var prefabs = new List<string>();

            foreach (var str in GameManifest.Get().pooledStrings)
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


            return null;
        }
    }
}