using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace Updater
{
    public class SteamSession : IDisposable
    {
        private readonly SteamClient client;
        private readonly CallbackManager callbacks;
        private SteamApps apps;
        private SteamUser user;
        
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

            callbacks.Subscribe<SteamKit2.SteamClient.ConnectedCallback>(callback =>
            {
                apps = client.GetHandler<SteamApps>();
                user = client.GetHandler<SteamUser>();
            });

            /*callbacks.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                running = true;

                updateThread = new Thread(RunCallbacks);
                updateThread.Start();

                apps = client.GetHandler<SteamApps>();
                user = client.GetHandler<SteamUser>();

                user.LogOnAnonymous(new SteamUser.AnonymousLogOnDetails { ClientLanguage = "en" });
            });

            callbacks.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                running = false;
                Console.WriteLine("Disconnected from steam.");
            });

            callbacks.Subscribe<SteamUser.LoggedOffCallback>(callback =>
            {
                Console.WriteLine("Logged off: " + callback.Result);
            });

            callbacks.Subscribe<SteamUser.LoggedOnCallback>(callback =>
            {
                callbacks.Subscribe<SteamApps.PICSProductInfoCallback>(apps.PICSGetProductInfo(258550, null, false), productInfos =>
                {
                    var publicBranch = productInfos.Apps[258550].KeyValues["depots"]["branches"]["public"];
                    uint buildId = publicBranch["buildid"].AsUnsignedInteger();
                    DateTime timeUpdated = DateTimeOffset.FromUnixTimeSeconds(publicBranch["timeupdated"].AsLong()).UtcDateTime;

                    Console.WriteLine("Build ID: " + buildId + ", time updated: " + timeUpdated + " UTC");
                });
            });*/
        }

        public void Dispose()
        {
            client.Disconnect();
        }

        public async Task<bool> ConnectAsync()
        {
            var task = new SteamTask<SteamClient.ConnectedCallback>(callbacks);
            client.Connect();
            return (await task.WaitForResult()).Result == EResult.OK;
        }

        /// <summary>Logs into steam anonymously.</summary>
        public async Task<SteamUser.LoggedOnCallback> LoginAsync()
        {
            var task = new SteamTask<SteamUser.LoggedOnCallback>(callbacks);
            user.LogOnAnonymous();
            return await task.WaitForResult();
        }

        public async Task<SteamApps.PICSProductInfoCallback> GetProductInfo(uint appId)
        {
            var task = new SteamTask<SteamApps.PICSProductInfoCallback>(callbacks, apps.PICSGetProductInfo(appId, null, false));
            return await task.WaitForResult();
        }
    }
}