using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Modules;

namespace DiscordBot.Modules
{
    public class RecipesModule : CalcRustModule
    {
        public override void Install(ModuleManager manager)
        {
            base.Install(manager);

            manager.CreateCommands("recipes", Config);
        }

        private void Config(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("requirements")
                .Alias("reqs")
                .Description("Prints the requirements for the specified item.")
                .Parameter("item")
                .Do(async args =>
                {
                    await args.Channel.SendMessage($"Request requirements for item '{args.GetArg("item")}'.");
                });
        }
    }
}