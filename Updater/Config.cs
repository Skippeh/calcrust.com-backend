using CommandLineParser.Arguments;

namespace Updater
{
    public class Config
    {
        [ValueArgument(typeof(string), 'a', "pushoverApiKey", Optional = false)]
        public string PushoverApiKey;

        [ValueArgument(typeof(string), 'u', "pushoverUserKey", Optional = false)]
        public string PushoverUserKey;
    }
}