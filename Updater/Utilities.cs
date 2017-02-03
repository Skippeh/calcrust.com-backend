using System;

namespace Updater
{
    public static class Utilities
    {
        public static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            dateTimeOffset = dateTimeOffset.AddSeconds(seconds);
            return dateTimeOffset;
        }

        public static long ToUnixFromSeconds(this DateTimeOffset dateTimeOffset)
        {
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var unixTimeStampInTicks = (dateTimeOffset.ToUniversalTime() - unixStart).Ticks;
            return unixTimeStampInTicks / TimeSpan.TicksPerSecond;
        }
    }
}