﻿using CommandLineParser.Arguments;

namespace DiscordBot
{
    public class Settings
    {
        [ValueArgument(typeof (string), 't', "token", Optional = false, Description = "The discord bot token used for authentication.")]
        public string Token { get; private set; } = "";

        [ValueArgument(typeof (string), 'a', "apiurl", Optional = true, Description = "The rust api to fetch data from.", FullDescription = "Defaults to 'https://api.calcrust.com/'. Note that the url needs to end with a slash.")]
        public string ApiUrl { get; private set; } = "https://api.calcrust.com/";
    }
}