using System;
using System.Threading.Tasks;
using CommandLineParser.Exceptions;
using SteamKit2;
using Updater.Pushover;
using Updater.Steam;

namespace Updater
{
    internal static class Program
    {
        public static SteamSession Session { get; private set; }
        public static Config Config { get; } = new Config();
        
        public static void Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser();
            parser.ExtractArgumentAttributes(Config);

            try
            {
                parser.ParseCommandLine(args);
            }
            catch (CommandLineArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                parser.ShowUsage();
                return;
            }

            Session = new SteamSession();
            Task.WaitAll(GetBranchInfo());

            Session.Dispose();
        }

        private static async Task GetBranchInfo()
        {
            bool connected = await Session.ConnectAsync();

            if (!connected)
            {
                Console.WriteLine("Failed to connect to Steam.");
                return;
            }

            var account = await Session.LoginAsync();

            if (account.Result != EResult.OK)
            {
                Console.WriteLine("Failed to login to Steam.");
                return;
            }

            var productInfo = await Session.GetProductInfo(258550);

            var publicBranch = productInfo.Apps[258550].KeyValues["depots"]["branches"]["public"];
            uint buildId = publicBranch["buildid"].AsUnsignedInteger();
            DateTime timeUpdated = DateTimeOffset.FromUnixTimeSeconds(publicBranch["timeupdated"].AsLong()).UtcDateTime;

            Console.WriteLine("Build ID: " + buildId + ", time updated: " + timeUpdated + " UTC");
        } 
    }
}