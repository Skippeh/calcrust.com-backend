using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2;
using Updater.Client;
using Updater.Extensions;
using Updater.Oxide;

namespace Updater.Steam
{
    public class AppPoller : IDisposable
    {
        public uint AppId;
        public string Branch;
        
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TimeSpan checkDelay = TimeSpan.FromSeconds(Program.LaunchArguments.CheckInterval);

        private static Dictionary<uint, Dictionary<string, uint>> currentVersions = new Dictionary<uint, Dictionary<string, uint>>();

        private Task pollingTask;
        private bool isServer => AppId == 258550;
        private bool isClient => AppId == 252490;

        private SteamSession session;

        public static bool LoadCurrentVersions(bool exitOnParseFailure)
        {
            if (!File.Exists("versions.json"))
            {
                SaveCurrentVersions();
                return false;
            }

            try
            {
                currentVersions = JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<string, uint>>>(File.ReadAllText("versions.json"));
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

        private static void SetVersion(uint appId, string branch, uint buildId)
        {
            if (!currentVersions.ContainsKey(appId))
            {
                currentVersions[appId] = new Dictionary<string, uint>();
            }

            currentVersions[appId][branch] = buildId;
        }

        public AppPoller(uint appId, string branch, string username = null, string password = null)
        {
            AppId = appId;
            Branch = branch;

            session = new SteamSession(username, password);

            pollingTask = Task.Run(PollUpdates, cancellation.Token);
        }

        public void Dispose()
        {
            cancellation.Cancel();
            session.Dispose();
        }

        private async Task PollUpdates()
        {
            while (true)
            {
                try
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    
                    UpdateInfo updateInfo;

                    if (!session.LoggedIn)
                    {
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

                    if (updateInfo.BuildID > GetInstalledVersion(AppId, Branch))
                    {
                        try
                        {
                            Task notificationTask = null;

                            if (Program.Pushbullet != null)
                                notificationTask = Program.Pushbullet.SendNotificationAsync("Rust Calculator",
                                    $"Downloading update...\nAppId/Branch: {AppId}/{Branch} ({(isClient ? "client" : "server")})\n" +
                                    $"Build id: {updateInfo.BuildID}\nReleased: {updateInfo.TimeUpdated} UTC");

                            Console.WriteLine("Downloading update. Build ID: " + updateInfo.BuildID + ", released: " + updateInfo.TimeUpdated);

                            // Delete managed dll's to avoid patching the same files multiple times.
                            try
                            {
                                if (Directory.Exists($"depots/{AppId}-{Branch}/RustDedicated_Data/Managed"))
                                {
                                    Directory.Delete($"depots/{AppId}-{Branch}/RustDedicated_Data/Managed", true);
                                }
                            }
                            catch (IOException ex)
                            {
                                Console.Error.WriteLine(ex);
                                await Retry();
                                continue;
                            }

                            if (await DownloadUpdates())
                            {
                                Console.WriteLine("Successfully downloaded update.");

                                if (isServer)
                                {
                                    Console.WriteLine("Patching server...");
                                    if (!await PatcherUtility.PatchServer($"./depots/{AppId}-{Branch}/"))
                                    {
                                        Console.Error.WriteLine("Failed to patch server.");
                                        await Retry();
                                        continue;
                                    }

                                    Console.WriteLine("Successfully patched server.");

                                    Console.WriteLine("Running server and uploading data...");
                                    if (!await ServerUtility.RunServerUpdateApi($"./depots/{AppId}-{Branch}/", Branch))
                                    {
                                        Console.Error.WriteLine("Failed to run server and update api.");

                                        await Retry();
                                        continue;
                                    }
                                }
                                else if (isClient)
                                {
                                    // Todo: Create smaller sized images next to original item images appended with _small.png.

                                    if (!await ClientUtility.UploadImages($"./depots/{AppId}-{Branch}/"))
                                    {
                                        Console.WriteLine("Failed to upload client images.");
                                        await Retry();
                                        continue;
                                    }
                                }

                                SetVersion(AppId, Branch, updateInfo.BuildID);
                                SaveCurrentVersions();
                                Console.WriteLine("Update finished successfully.");

                                if (Program.Pushbullet != null)
                                    await Program.Pushbullet.SendNotificationAsync("Rust Calculator", (isClient ? "Client " : "Server ") + "update finished successfully. 👌");
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
                
                await Task.Delay(checkDelay, cancellation.Token);
            }
        }

        private async Task Retry()
        {
            if (session.LoggedIn)
                Console.WriteLine("Retrying in 10 seconds...");

            await Task.Delay(TimeSpan.FromSeconds(10), cancellation.Token);
        }

        private uint GetInstalledVersion(uint appId, string branch)
        {
            if (currentVersions.ContainsKey(appId) && currentVersions[appId].ContainsKey(branch))
            {
                return currentVersions[appId][branch];
            }

            SetVersion(appId, branch, 0);
            return 0;
        }

        private async Task<bool> DownloadUpdates()
        {
            if (session.Username != null)
                await session.Logoff();

            bool success = await DepotUtility.DownloadAppAsync(AppId, Branch, isClient ? "./Data/client-filelist.txt" : null, session.Username, session.Password);

            if (session.Username != null)
                await session.ConnectAndLoginAsync();

            return success;
        }

        private async Task<UpdateInfo> GetUpdateInfo()
        {
            var productInfo = await session.GetProductInfo(AppId);

            var branch = productInfo.Apps[AppId].KeyValues["depots"]["branches"][Program.LaunchArguments.Branch];
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