using CommandLineParser.Arguments;

namespace Updater
{
    public class LaunchArguments
    {
        [ValueArgument(typeof(bool), 'h', "help")]
        public bool ShowHelp;

        [ValueArgument(typeof(string), 't', "pushbulletToken", Description = "Api key for Pushbullet. If specified notifications will be posted on certain events.")]
        public string PushbulletToken;

        [ValueArgument(typeof(string), 'p', "pushbulletPassword", Description = "Pushbullet encryption password.")]
        public string PushbulletPassword;

        [ValueArgument(typeof(string), 'b', "branch", DefaultValue = "public", Description = "The game branch to use, for example staging or prerelease.")]
        public string Branch;

        [ValueArgument(typeof(uint), 'i', "interval", Description = "Seconds between checking for updates.", DefaultValue = 120u)]
        public uint CheckInterval;

        [ValueArgument(typeof(string), 's', "steamcmd", Description = "The filepath to SteamCMD root folder.", Optional = false)]
        public string SteamCMDPath;
    }
}