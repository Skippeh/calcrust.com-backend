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

            callbacks.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                apps = client.GetHandler<SteamApps>();
                user = client.GetHandler<SteamUser>();
            });
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