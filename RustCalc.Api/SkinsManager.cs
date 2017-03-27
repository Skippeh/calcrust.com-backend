using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RustCalc.Common.Models;

namespace RustCalc.Api
{
    public static class SkinsManager
    {
        public static readonly Dictionary<Item, List<ItemSkin>> Skins = new Dictionary<Item, List<ItemSkin>>();
        public static string LastError;

        private const string requestUrl = "http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json";

        private static bool running = false;
        private static CancellationTokenSource cancellationSource;
        private static WebClient webClient;

        private static ExportData data;

        private static readonly int[] ignoreIds =
        {
            1, // The Accident Book
            10031, // Cloth
            14182, // Metal
            14183, // Wood
            14184, // Low Quality Bag
            14185, // High Quality Bag
            14187, // Box
            14189 // Weapon Barrel
        };

        public static void Initialize(ExportData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            SkinsManager.data = data;
            running = true;
            cancellationSource = new CancellationTokenSource();
            webClient = new WebClient();
            StartRefreshSkinsTask();
        }

        public static void Shutdown()
        {
            if (!running)
                return;

            running = false;
            cancellationSource.Cancel();
        }

        private static Task StartRefreshSkinsTask()
        {
            return Task.Run(async () =>
            {
                while (running)
                {
                    try
                    {
                        string json = await webClient.DownloadStringTaskAsync(requestUrl);
                        var skins = JsonConvert.DeserializeObject<ItemSkinsResponse>(json, new ItemSkinConverter()).Items;
                        var itemsDict = data.Items.Values.ToDictionary(item => item.Shortname, item => item);

                        foreach (var skin in skins)
                        {
                            if (ignoreIds.Contains(skin.ItemDefId))
                                continue;

                            if (!itemsDict.ContainsKey(skin.ItemShortname))
                            {
                                Console.Error.WriteLine($"Unknown item ({skin.ItemShortname}) for skin, ignoring: {skin.Name} ({skin.ItemDefId})");
                                continue;
                            }

                            AddOrUpdateSkin(itemsDict[skin.ItemShortname], skin);
                        }
                    }
                    catch (WebException ex)
                    {
                        Console.Error.WriteLine("Failed to download skins:\n" + ex);
                        LastError = "failed-request";
                    }
                    catch (JsonException ex)
                    {
                        Console.Error.WriteLine("Failed to deserialize skins:\n" + ex);
                        LastError = "failed-deserialization";
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed to parse skins:\n" + ex);
                        LastError = "failed-parse";
                    }

                    LastError = null;

                    await Task.Delay(1000 * 60 * 5, cancellationSource.Token);
                }
            });
        }

        private static void AddOrUpdateSkin(Item parent, ItemSkin skin)
        {
            List<ItemSkin> skins;

            if (Skins.ContainsKey(parent))
                skins = Skins[parent];
            else
            {
                skins = new List<ItemSkin>();
                Skins.Add(parent, skins);
            }

            var existingSkin = skins.FirstOrDefault(s => s.ItemDefId == skin.ItemDefId);

            if (existingSkin != null)
            {
                existingSkin.UpdateFrom(skin);
            }
            else
            {
                skins.Add(skin);
            }
        }

        private class ItemSkinsResponse
        {
            public int Appid;
            public ItemSkin[] Items;
        }
    }
}