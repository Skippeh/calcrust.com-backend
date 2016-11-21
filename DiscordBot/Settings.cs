using CommandLineParser.Arguments;

namespace DiscordBot
{
    public class Settings
    {
        [ValueArgument(typeof (string), 't', "token", Optional = false, Description = "The discord bot token used for authentication.")]
        public string Token { get; private set; } = "";
    }
}