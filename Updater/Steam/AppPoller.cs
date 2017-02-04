using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2;

namespace Updater.Steam
{
    public class AppPoller : IDisposable
    {
        public uint AppId;
        public string Branch;
        
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TimeSpan checkDelay = TimeSpan.FromSeconds(Program.LaunchArguments.CheckInterval);

        private static Dictionary<uint, uint> currentVersions = new Dictionary<uint, uint>();

        private Task pollingTask; 

        public static bool LoadCurrentVersions(bool exitOnParseFailure)
        {
            if (!File.Exists("versions.json"))
            {
                SaveCurrentVersions();
                return false;
            }

            try
            {
                currentVersions = JsonConvert.DeserializeObject<Dictionary<uint, uint>>(File.ReadAllText("versions.json"));
                return true;
            }
            catch (JsonSerializationException)
            {
                Console.Error.WriteLine("Failed to load versions.json!");

                if (exitOnParseFailure)
                    Environment.Exit(1);
            }

            return false;
        }

        public static bool SaveCurrentVersions()
        {
            try
            {
                File.WriteAllText("versions.json", JsonConvert.SerializeObject(currentVersions));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public AppPoller(uint appId, string branch)
        {
            AppId = appId;
            Branch = branch;

            pollingTask = Task.Run(PollUpdates, cancellation.Token);
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }

        private async Task PollUpdates()
        {
            while (true)
            {
                try
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    
                    UpdateInfo updateInfo;

                    if (!await Program.Session.ConnectAsync())
                    {
                        Console.Error.WriteLine("Failed to connect to Steam.");
                        await Retry();
                        continue;
                    }

                    var account = await Program.Session.Login();

                    if (account.Result != EResult.OK)
                    {
                        Console.Error.WriteLine("Failed to log in to Steam: " + account.Result);
                        await Retry();
                        continue;
                    }
                    
                    try
                    {
                        updateInfo = await GetUpdateInfo();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to check for updates: " + ex.Message);
                        await Retry();
                        continue;
                    }

                    // Check and throw if cancelled before starting update process.
                    cancellation.Token.ThrowIfCancellationRequested();

                    if (updateInfo.BuildID > GetInstalledVersion(AppId))
                    {
                        try
                        {
                            Task notificationTask = null;

                            if (Program.Pushbullet != null)
                                notificationTask = Program.Pushbullet.SendNotificationAsync("Rust Calculator", $"Downloading update...\nAppId/Branch: {AppId}/{Branch}\nBuild id: {updateInfo.BuildID}\nReleased: {updateInfo.TimeUpdated} UTC");

                            Console.WriteLine("Downloading update. Build ID: " + updateInfo.BuildID + ", released: " + updateInfo.TimeUpdated);

                            if (await DownloadUpdates())
                            {
                                currentVersions[AppId] = updateInfo.BuildID;
                                SaveCurrentVersions();
                                Console.WriteLine("Successfully downloaded update.");
                            }
                            else
                            {
                                Console.Error.WriteLine("Failed to download updates.");
                                await Retry();
                                continue;
                            }

                            if (notificationTask != null)
                                await notificationTask; // Just incase it's not done (highly unlikely though).
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to download updates: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                await Program.Session.Logoff();
                await Task.Delay(checkDelay, cancellation.Token);
            }
        }

        private async Task Retry()
        {
            Console.WriteLine("Retrying in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10), cancellation.Token);
        }

        private uint GetInstalledVersion(uint appId)
        {
            if (currentVersions.ContainsKey(appId))
            {
                return currentVersions[appId];
            }

            currentVersions[appId] = 0;
            return 0;
        }

        private async Task<bool> DownloadUpdates()
        {
            return await DepotUtility.DownloadAppAsync(AppId, Branch);
        }

        private async Task<UpdateInfo> GetUpdateInfo()
        {
            var productInfo = await Program.Session.GetProductInfo(258550);

            var branch = productInfo.Apps[258550].KeyValues["depots"]["branches"][Program.LaunchArguments.Branch]; // Todo: Verify that branch exists.
            uint buildId = branch["buildid"].AsUnsignedInteger();
            DateTime timeUpdated = Utilities.FromUnixTimeSeconds(branch["timeupdated"].AsLong()).UtcDateTime;
            
            return new UpdateInfo
            {
                BuildID = buildId,
                TimeUpdated = timeUpdated
            };
        }
    }
}