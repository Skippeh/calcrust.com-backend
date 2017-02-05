using System;
using System.Net;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace Updater.Steam
{
    public class SteamSession : IDisposable
    {
        public bool LoggedIn { get; private set; }
        private SteamUser.LoggedOnCallback currentLogin;

        private readonly SteamClient client;
        private readonly CallbackManager callbacks;
        private SteamApps apps;
        private SteamUser user;

        private bool stayConnected;
        private bool reconnecting;

        public SteamSession()
        {
            client = new SteamClient();
            callbacks = new CallbackManager(client);

            DebugLog.Enabled = false;
            DebugLog.AddListener((s, s1) =>
            {
                Console.WriteLine(s + "\t" + s1);
            });

            Task.Run(async () =>
            {
                TimeSpan delay = TimeSpan.FromSeconds(1);

                while (true)
                {
                    callbacks.RunWaitAllCallbacks(TimeSpan.Zero);
                    await Task.Delay(delay);
                }
            });

            callbacks.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                apps = client.GetHandler<SteamApps>();
                user = client.GetHandler<SteamUser>();
            });

            callbacks.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                LoggedIn = false;

                if (stayConnected && !reconnecting)
                {
                    reconnecting = true;

                    Task.WaitAll(Task.Run(async () =>
                    {
                        Console.Error.WriteLine("Lost connection to Steam, reconnecting...");

                        // Try send notification
                        if (Program.Pushbullet != null)
                            await Program.Pushbullet.SendNotificationAsync("Rust Calculator", "Lost connection to Steam!");

                        int numFailures = 0;
                        int notificationDelay = 60 * 30; // Try send notification every 30 minutes.
                        DateTime downStart = DateTime.UtcNow;
                        DateTime nextNotification = DateTime.UtcNow + TimeSpan.FromSeconds(notificationDelay);

                        while (true)
                        {
                            try
                            {
                                TimeSpan downTime = DateTime.UtcNow - downStart;

                                if (await ConnectAndLoginAsync())
                                {
                                    reconnecting = false;
                                    Console.WriteLine("Reconnected.");

                                    if (Program.Pushbullet != null)
                                        await Program.Pushbullet.SendNotificationAsync("Rust Calculator", "Reconnected to Steam after " + downTime.ToString(@"hh\:mm\:ss") + ".");

                                    break;
                                }
                                else
                                {
                                    ++numFailures;

                                    if (numFailures % 10 == 0)
                                    {
                                        Console.Error.WriteLine("Failed to connect 10 times...");
                                    }

                                    if (DateTime.UtcNow > nextNotification)
                                    {
                                        nextNotification = DateTime.UtcNow + TimeSpan.FromSeconds(notificationDelay);

                                        if (Program.Pushbullet != null)
                                            await Program.Pushbullet.SendNotificationAsync("Rust Calculator", "Still offline, downtime: " + downTime.ToString(@"hh\:mm\:ss" + "."));
                                    }

                                    await Task.Delay(1000); // Wait a second before trying to reconnect.
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                    }));
                }
            });
            
            Task.WaitAll(ConnectAndLoginAsync());
        }

        public void Dispose()
        {
            if (LoggedIn || (client != null && client.IsConnected))
                Task.WaitAll(Logoff());
        }
        
        public Task<bool> ConnectAsync()
        {
            return Task.Run<bool>(() =>
            {
                if (client.IsConnected)
                    return true;

                stayConnected = true;
                var steamTask1 = new SteamTask<SteamClient.ConnectedCallback>(callbacks);
                var steamTask2 = new SteamTask<SteamClient.DisconnectedCallback>(callbacks);
                client.Connect();

                var task1 = steamTask1.WaitForResult();
                var task2 = steamTask2.WaitForResult();
                Task.WaitAny(task1, task2);

                if (task1.IsCompleted) // Connected
                {
                    steamTask2.Cancel();
                    return true;
                }
                else if (task2.IsCompleted) // Failed to connect
                {
                    steamTask1.Cancel();
                    return false;
                }

                throw new NotImplementedException();
            });
        }

        /// <summary>Logs into steam anonymously.</summary>
        public async Task<SteamUser.LoggedOnCallback> Login()
        {
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected to Steam.");

            if (LoggedIn)
                return currentLogin;
            
            var task = new SteamTask<SteamUser.LoggedOnCallback>(callbacks);
            user.LogOnAnonymous();
            
            var result = await task.WaitForResult();
            LoggedIn = result.Result == EResult.OK;

            if (LoggedIn)
                currentLogin = result;

            return result;
        }

        public async Task<SteamApps.PICSProductInfoCallback> GetProductInfo(uint appId)
        {
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected to Steam.");

            var task = new SteamTask<SteamApps.PICSProductInfoCallback>(callbacks, apps.PICSGetProductInfo(appId, null, false));
            return await task.WaitForResult();
        }

        public async Task<SteamClient.DisconnectedCallback> Logoff()
        {
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected to Steam.");

            stayConnected = false;
            var task = new SteamTask<SteamClient.DisconnectedCallback>(callbacks);
            user.LogOff();
            var callback = await task.WaitForResult();

            LoggedIn = false;
            currentLogin = null;

            return callback;
        }
        
        /// <summary>Connects and logs in to Steam, but only if we're not connected or not logged in. Safe to call multiple times.</summary>
        public async Task<bool> ConnectAndLoginAsync()
        {
            bool connected = client.IsConnected;

            if (!client.IsConnected)
                connected = await ConnectAsync();

            if (!connected)
                return false;

            bool loggedIn = LoggedIn;

            if (!loggedIn)
                loggedIn = (await Login()).Result == EResult.OK;

            LoggedIn = loggedIn;

            if (!loggedIn)
                return false;

            return true;
        }
    }
}