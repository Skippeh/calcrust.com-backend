using System;

namespace DiscordBot.Utility
{
    internal static class FormatUtility
    {
        public static string FriendlyTime(ulong totalSeconds)
        {
            int hours = (int)Math.Floor(totalSeconds / 3600f);
            int minutes = (int)Math.Floor(totalSeconds / 60f % 60);
            int seconds = (int) (totalSeconds % 60);

            if (hours <= 0 && minutes <= 0)
                return seconds + "s";
            if (hours <= 0)
                return minutes + "m" + seconds + "s";

            return hours + "h" + minutes + "m" + seconds + "s";
        }
    }
}