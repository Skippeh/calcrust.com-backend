namespace Updater.Pushover
{
    public enum MessagePriority
    {
        /// <summary>No notification or alert</summary>
        NoNotification = -2,

        /// <summary>Quiet notification (normal notification)</summary>
        QuietNotification = -1,

        /// <summary>High priority notification which bypasses quiet hours.</summary>
        HighPriority = 1,

        /// <summary>Same as high priority, but also requires the user to confirm.</summary>
        RequireConfirmation = 2
    }
}