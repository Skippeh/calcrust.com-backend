using System;
using CommandLineParser.Exceptions;
using Discord;
using Discord.Modules;

namespace DiscordBot
{
    internal static class Program
    {
        public static DiscordClient Client { get; private set; }
        
        static void Main(string[] args)
        {
            var argsParser = new CommandLineParser.CommandLineParser();
            var settings = new Settings();

            argsParser.ShowUsageOnEmptyCommandline = true;
            argsParser.ExtractArgumentAttributes(settings);

            try
            {
                argsParser.ParseCommandLine(args);
            }
            catch (CommandLineException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return;
            }
            
            var builder = new DiscordConfigBuilder();

            // Todo: Better logging
            builder.LogLevel = LogSeverity.Info;
            builder.LogHandler += (sender, eventArgs) =>
            {
                Console.WriteLine("[DISCORD] " + eventArgs.Message);
            };
            
            Client = new DiscordClient(builder.Build());
            Client.AddService<ModuleService>();

            var calcModule = new RustCalcModule();
            Client.AddModule(calcModule);

            Client.ExecuteAndWait(async () =>
            {
                await Client.Connect(settings.Token, TokenType.Bot);
            });
        }
    }
}