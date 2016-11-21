using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace DiscordBot.Modules
{
    public class CalcRustModule : IModule
    {
        protected DiscordClient Client { get; private set; }
        protected CommandService Commands { get; private set; }

        public virtual void Install(ModuleManager manager)
        {
            Client = manager.Client;
            Commands = Client.GetService<CommandService>();
        }
    }
}