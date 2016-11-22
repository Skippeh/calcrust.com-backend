using System;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using DiscordBot.Rust;

namespace DiscordBot.Modules
{
    public class CalcRustModule : IModule
    {
        public static RustApi Api => Program.Api;

        protected DiscordClient Client { get; private set; }

        public virtual void Install(ModuleManager manager)
        {
            Client = manager.Client;
        }
    }
}