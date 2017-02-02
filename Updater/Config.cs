using CommandLineParser.Arguments;

namespace Updater
{
    public class Config
    {
        [ValueArgument(typeof(string), 'a', "pushoverApiKey")]
        public string PushoverApiKey;

        [ValueArgument(typeof(string), 'u', "pushoverUserKey")]
        public string PushoverUserKey;
    }
}