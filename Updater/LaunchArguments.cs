using CommandLineParser.Arguments;

namespace Updater
{
    public class LaunchArguments
    {
        [ValueArgument(typeof(bool), 'h', "help")]
        public bool ShowHelp;

        [ValueArgument(typeof(string), 'a', "pushoverApiKey", Description = "Api key for Pushover. If specified along with user key, notifications will be posted on certain events.")]
        public string PushoverApiKey;

        [ValueArgument(typeof(string), 'u', "pushoverUserKey")]
        public string PushoverUserKey;

        [ValueArgument(typeof(string), 'b', "branch", DefaultValue = "public", Description = "The game branch to use, for example staging or prerelease.")]
        public string Branch;

        [ValueArgument(typeof(uint), 'i', "interval", Description = "Seconds between checking for updates.", DefaultValue = 120u)]
        public uint CheckInterval;
    }
}