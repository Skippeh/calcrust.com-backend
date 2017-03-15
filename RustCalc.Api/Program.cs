using System;
using System.Collections.Generic;
using System.IO;
using Nancy.Hosting.Self;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;

namespace RustCalc.Api
{
    public static class Program
    {
        private const short serverPort = 7545;

        public static Dictionary<GameBranch, ExportData> Data { get; private set; }

        static Program()
        {
            Data = new Dictionary<GameBranch, ExportData>();
        }

        static void Main(string[] args)
        {
            foreach (GameBranch branch in Enum.GetValues(typeof (GameBranch)))
            {
                Exception loadException = LoadBranch(branch);

                if (loadException != null)
                {
                    Console.Error.WriteLine($"Failed to load branch {branch}:");

                    if (!(loadException is FileNotFoundException) && !(loadException is DirectoryNotFoundException))
                        Console.Error.WriteLine(loadException.ToString());
                    else
                    {
                        Console.Error.WriteLine(loadException.Message);
                    }
                }
            }

            using (var host = new NancyHost(new ApiBootstrapper(), new HostConfiguration() { UrlReservations = new UrlReservations { CreateAutomatically = true } }, new Uri($"http://localhost:{serverPort}")))
            {
                host.Start();

                Console.WriteLine($"Starting api server on port {serverPort}...");

                Console.WriteLine("Press CTRL+Q to stop the server.");
                ConsoleKeyInfo consoleKeyInfo;
                while ((consoleKeyInfo = Console.ReadKey(true)).Key != ConsoleKey.Q || consoleKeyInfo.Modifiers != ConsoleModifiers.Control)
                    continue;

                Console.WriteLine("Stopping api server...");
                host.Stop();
            }
        }

        private static Exception LoadBranch(GameBranch branch)
        {
            try
            {
                using (var fs = File.OpenRead($"data/{branch}/rustcalc-export.bin"))
                {
                    var reader = new BinaryReader(fs);
                    var data = ExportManager.DeserializeData(reader);
                    Data.Add(branch, data);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}