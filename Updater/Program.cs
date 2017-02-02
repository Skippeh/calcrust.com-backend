﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLineParser.Exceptions;
using SteamKit2;
using Updater.Steam;

namespace Updater
{
    internal static class Program
    {
        public static SteamSession Session { get; private set; }
        public static List<AppPoller> AppPollers = new List<AppPoller>();  
        public static LaunchArguments LaunchArguments { get; } = new LaunchArguments();
        
        public static void Main(string[] args)
        {
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
    }
}