using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebAPI.Models;
using WebAPI.Models.Destructibles;

namespace WebAPI
{
    internal static class DataManager
    {
        public static RustData Data { get; private set; }

        private static string filePath;

        private static FileSystemWatcher watcher;
        private static DateTime lastWriteTime;

        private static Dictionary<string, int> authTries = new Dictionary<string, int>();

        public static void Start(string _filePath)
        {
            filePath = _filePath;

            if (watcher != null)
                throw new InvalidOperationException("Already started.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified data file can't be found.", filePath);
            
            string directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            string fileName = Path.GetFileName(filePath);
            watcher = new FileSystemWatcher(directory, fileName);
            Console.WriteLine($"Watching directory {directory} for {fileName}");

            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;

            LoadFile();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                var writeTime = File.GetLastWriteTime(filePath);

                if (writeTime - lastWriteTime <= new TimeSpan(0, 0, 0, 0, 50)) // Hack: avoid event being triggered twice caused by bug by checking if time since last save is > 50ms.
                    return;

                lastWriteTime = writeTime;

                Thread.Sleep(100); // File in use for some reason (by the watcher i assume).

                Console.Write($"Reloading...");
                Data?.Dispose();
                Data = null;
                LoadFile();
                Console.WriteLine(" done.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public static void Stop()
        {
            watcher?.Dispose();
            watcher = null;
        }

        private static void LoadFile()
        {
            string contents = File.ReadAllText(filePath);
            Data = ParseData(contents);

            if (!File.Exists("auths.json"))
            {
                SaveAuthTries();
            }
            else
            {
                authTries = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("auths.json"));
            }
        }

        private static RustData ParseData(string contents)
        {
            var data = new RustData();
            JObject jData = JObject.Parse(contents);
            var jItems = jData["items"];

            data.Items = jItems.ToObject<Dictionary<string, Item>>();

            // Set shortname
            foreach (var keyval in data.Items)
            {
                keyval.Value.Shortname = keyval.Key;
            }

            data.Meta = jData["meta"].ToObject<Meta>();
            data.Recipes = new Dictionary<string, Recipe>();
            data.Cookables = new Dictionary<string, Cookable>();
            data.DamageInfo = new Dictionary<string, Destructible>();

            var jRecipes = jData["recipes"].Value<JObject>();
            var jCookables = jData["cookables"].Value<JObject>();
            var jDestructibles = jData["damageInfo"].Value<JObject>();

            foreach (var keyval in jRecipes)
            {
                dynamic jRecipe = keyval.Value;
                string shortname = keyval.Key;
                var recipe = keyval.Value.ToObject<Recipe>();

                recipe.Output = ParseRecipeItem(jRecipe.output, data.Items);
                recipe.Input = ((JArray) jRecipe.input).Select(r => ParseRecipeItem(r, data.Items)).ToList();

                if (jRecipe.parent != null)
                    recipe.Parent = data.Items[(string) jRecipe.parent];

                data.Recipes.Add(shortname, recipe);
            }

            foreach (var keyval in jCookables)
            {
                dynamic jCookable = keyval.Value;
                string shortname = keyval.Key;
                var cookable = keyval.Value.ToObject<Cookable>();

                cookable.Output = ParseRecipeItem(jCookable.output, data.Items);
                cookable.UsableOvens = ((JArray) jCookable.usableOvens).Select(jItem => new Cookable.Oven
                {
                    Item = data.Items[jItem["shortname"].Value<string>()],
                    FuelConsumed = jItem["fuelConsumed"].Value<float>()
                }).ToList();

                data.Cookables.Add(shortname, cookable);
            }
            
            foreach (var keyval in jDestructibles)
            {
                JObject jDestructible = keyval.Value.Value<JObject>();
                string shortname = keyval.Key;
                string buildingType = jDestructible["type"].Value<string>();
                Destructible destructible;
                
                switch (buildingType)
                {
                    case "buildingBlock":
                    {
                        var buildingDestructible = (BuildingBlockDestructible) (destructible = new BuildingBlockDestructible());

                        foreach (var gradeKeyval in jDestructible["grades"].Value<JObject>())
                        {
                            string grade = gradeKeyval.Key;
                            JObject jAttackInfos = gradeKeyval.Value.Value<JObject>();
                            var attackInfos = ParseAttackInfos(jAttackInfos, data.Items);

                            buildingDestructible.Grades.Add(grade, attackInfos);
                        }

                        break;
                    }
                    case "deployable":
                    {
                        destructible = new DeployableDestructible();
                        ((DeployableDestructible) destructible).Values = ParseAttackInfos(jDestructible["values"].Value<JObject>(), data.Items);
                        break;
                    }
                    default:
                    {
                        Console.Error.WriteLine("Unhandled building type: " + buildingType);
                        continue;
                    }
                }

                destructible.HasProtection = jDestructible["hasProtection"].Value<bool>();

                data.DamageInfo.Add(shortname, destructible);
            }

            // Parse item meta data
            foreach (Item item in data.Items.Values)
            {
                item.Meta = new Dictionary<MetaType, ItemMeta>();
                JObject jMetas = (JObject) jItems[item.Shortname]["meta"];

                foreach (JProperty jMetaProperty in jMetas.Values<JProperty>())
                {
                    var jMeta = (JObject) jMetaProperty.Value;
                    MetaType metaType = jMeta["type"].ToObject<MetaType>();
                    switch (metaType)
                    {
                        case MetaType.Oven:
                        {
                            item.Meta.Add(MetaType.Oven, new Models.MetaModels.Oven()
                            {
                                AllowByproductCreation = jMeta["allowByproductCreation"].Value<bool>(),
                                Slots = jMeta["slots"].Value<int>(),
                                FuelType = jMeta["fuelType"].Type != JTokenType.Null ? data.Items[jMeta["fuelType"].Value<string>()] : null,
                                Temperature = jMeta["temperature"].Value<float>()
                            });
                            break;
                        }
                        case MetaType.Burnable:
                        {
                            item.Meta.Add(MetaType.Burnable, new Models.MetaModels.Burnable()
                            {
                                ByproductAmount = jMeta["byproductAmount"].Value<int>(),
                                ByproductChance = jMeta["byproductChance"].Value<float>(),
                                ByproductItem = jMeta["byproductItem"].Type != JTokenType.Null ? data.Items[jMeta["byproductItem"].Value<string>()] : null,
                                FuelAmount = jMeta["fuelAmount"].Value<float>()
                            });
                            break;
                        }
                        case MetaType.Weapon:
                        {
                            item.Meta.Add(MetaType.Weapon, new Models.MetaModels.Weapon()
                            {
                                FireDelay = jMeta["fireDelay"].Value<float>(),
                                ReloadTime = jMeta["reloadTime"].Value<float>(),
                                MagazineSize = jMeta["magazineSize"].Value<int>()
                            });
                            break;
                        }
                        default:
                        {
                            item.Meta.Add(metaType, new ItemMeta(metaType));
                            continue;
                        }
                    }
                }
            }
            
            return data;
        }

        private static DestructibleValues ParseAttackInfos(JObject jAttackInfos, Dictionary<string, Item> items)
        {
            var attackInfos = new DestructibleValues();

            foreach (var keyval in jAttackInfos)
            {
                Item item = items[keyval.Key];
                JObject jAttackInfo = keyval.Value.Value<JObject>();
                string attackInfoType = jAttackInfo["type"].Value<string>();
                AttackInfo attackInfo;

                switch (attackInfoType)
                {
                    case "weapon":
                    {
                        WeaponAttackInfo weaponAttackInfo = (WeaponAttackInfo) (attackInfo = new WeaponAttackInfo());
                        var jAmmunitions = jAttackInfo["ammunitions"].Value<JObject>();

                        foreach (var jAmmunitionKeyval in jAmmunitions)
                        {
                            Item ammunitionItem = items[jAmmunitionKeyval.Key];
                            HitValues hitValues = ParseHitValues(jAmmunitionKeyval.Value.Value<JObject>());

                            weaponAttackInfo.Ammunitions.Add(ammunitionItem, hitValues);
                        }
                        
                        break;
                    }
                    case "melee":
                    case "explosive":
                    {
                        SingleAttackInfo singleAttackInfo;

                        if (attackInfoType == "melee")
                            singleAttackInfo = (SingleAttackInfo) (attackInfo = new MeleeAttackInfo());
                        else
                            singleAttackInfo = (SingleAttackInfo) (attackInfo = new ExplosiveAttackInfo());

                        singleAttackInfo.Values = ParseHitValues(jAttackInfo["values"].Value<JObject>());

                        break;
                    }
                    default:
                    {
                        Console.Error.WriteLine("Unhandled AttackInfo: " + attackInfoType);
                        continue;
                    }
                }

                attackInfos.Add(item, attackInfo);
            }

            return attackInfos;
        }

        private static HitValues ParseHitValues(JObject jHitValues)
        {
            return new HitValues
            {
                WeakDPS = jHitValues["weakDps"].Value<float>(),
                StrongDPS = jHitValues["strongDps"].Value<float>(),
                TotalWeakHits = jHitValues["totalWeakHits"].Value<float>(),
                TotalStrongHits = jHitValues["totalStrongHits"].Value<float>(),
                TotalStrongItems = jHitValues["totalStrongItems"].Value<float>(),
                TotalWeakItems = jHitValues["totalWeakItems"].Value<float>()
            };
        }

        private static Recipe.Item ParseRecipeItem(JToken jItem, Dictionary<string, Item> items)
        {
            return new Recipe.Item(jItem["count"].Value<double>(), items[jItem["item"].Value<string>()]);
        }

        public static void ChangeData(string json)
        {
            var newData = ParseData(json);
            Data.Dispose();
            Data = newData;
        }

        public static bool IsBanned(string ip)
        {
            return authTries.ContainsKey(ip) && authTries[ip] >= 5;
        }

        public static void IncrementTries(string ip)
        {
            if (!authTries.ContainsKey(ip))
                authTries.Add(ip, 0);

            ++authTries[ip];
            SaveAuthTries();
        }

        public static void ResetTries(string ip)
        {
            if (authTries.ContainsKey(ip))
                authTries.Remove(ip);

            SaveAuthTries();
        }

        private static readonly object _saveLock = new object();
        private static void SaveAuthTries()
        {
            lock (_saveLock)
            {
                using (var file = File.CreateText("auths.json"))
                {
                    file.Write(JsonConvert.SerializeObject(authTries, Formatting.Indented));
                }
            }
        }

        public static void Save(string json)
        {
            using (var file = File.CreateText(filePath))
            {
                file.Write(json);
            }
        }
    }
}