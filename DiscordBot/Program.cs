using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLineParser.Exceptions;
using Discord;
using Discord.Commands;
using Discord.Modules;
using DiscordBot.Modules;
using DiscordBot.Rust;
using JsonNet.PrivateSettersContractResolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nito.AsyncEx;

namespace DiscordBot
{
    internal static class Program
    {
        public static DiscordClient Client { get; private set; }
        public static RustApi Api { get; private set; }

        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            ContractResolver = new PrivateSetterCamelCasePropertyNamesContractResolver()
        };
        
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

            Api = new RustApi(settings.ApiUrl);

            // Testing
            Action apiTest = async () =>
            {
                var search = await Api.SearchRecipe("rifle");
            };

            Task.Run(apiTest).Wait();
            
            var builder = new DiscordConfigBuilder();

            // Todo: Better logging
            builder.LogLevel = LogSeverity.Info;
            builder.LogHandler += (sender, eventArgs) =>
            {
                Console.WriteLine("[DISCORD] " + eventArgs.Message);
            };
            
            Client = new DiscordClient(builder.Build());
            Client.AddService<ModuleService>();
            Client.UsingCommands(conf =>
            {
                conf.HelpMode = HelpMode.Public;
                conf.AllowMentionPrefix = true;
            });

            Client.AddModule<RecipesModule>();
            Client.AddModule<DestructiblesModule>();

            Client.ExecuteAndWait(async () =>
            {
                await Client.Connect(settings.Token, TokenType.Bot);
            });

            // Prevent exiting the program.
            while (true) { Console.ReadKey(true); }
        }
    }
}