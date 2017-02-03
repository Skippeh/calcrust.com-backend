using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLineParser.Exceptions;
using PushbulletSharp;
using SteamKit2;
using Updater.Steam;

namespace Updater
{
    internal static class Program
    {
        public static SteamSession Session { get; private set; }
        public static List<AppPoller> AppPollers = new List<AppPoller>();  
        public static LaunchArguments LaunchArguments { get; } = new LaunchArguments();
        public static bool RunningUnix { get; private set; }
        public static PushbulletClient Pushbullet { get; private set; }

        public static void Main(string[] args)
        {
            RunningUnix = System.Environment.OSVersion.Platform == PlatformID.Unix;

            if (RunningUnix)
            {
                Console.WriteLine("Unix system detected");
            }
            else if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Console.WriteLine("Running on unsupported OS. Some things may not work properly.");
            }

            var parser = new CommandLineParser.CommandLineParser();
            parser.ExtractArgumentAttributes(LaunchArguments);

            try
            {
                parser.ParseCommandLine(args);

                if (LaunchArguments.ShowHelp)
                {
                    parser.ShowUsage();
                    return;
                }
            }
            catch (CommandLineArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                parser.ShowUsage();
                return;
            }

            if (!LaunchArguments.InstallPath.EndsWith("/") &&
                !LaunchArguments.InstallPath.EndsWith("\\") &&
                !LaunchArguments.InstallPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                LaunchArguments.InstallPath += Path.DirectorySeparatorChar;
            }

            if (!File.Exists("ThirdParty/DepotDownloader/DepotDownloader.exe"))
            {
                Console.Error.WriteLine("DepotDownloader.exe not found.");
                return;
            }

            InitializePushbullet();
            
            Session = new SteamSession();

            AppPoller.LoadCurrentVersions(true);
            AppPollers.Add(new AppPoller(258550, LaunchArguments.Branch));

            Console.WriteLine("Press CTRL+Q to quit.");
            
            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    break;
                }
            }

            Session.Dispose();
            AppPoller.SaveCurrentVersions();

            foreach (var poller in AppPollers)
            {
                poller.Dispose();
            }
        }

        private static void InitializePushbullet()
        {
            string apiKey = LaunchArguments.PushbulletToken;
            string password = LaunchArguments.PushbulletPassword;
            TimeZoneInfo timeZone = TimeZoneInfo.Utc;

            if (apiKey == null)
            {
                Console.WriteLine("Pushbullet token not specified, notifications disabled.");
                return;
            }

            Console.WriteLine("Pushbullet token specified, notifications will be sent.");

            if (password != null)
                Pushbullet = new PushbulletClient(apiKey, password, timeZone);
            else
                Pushbullet = new PushbulletClient(apiKey, timeZone);
        }
    }
}